using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class ServiceRegistrationsAndRosterE2ETests(PostgresWebAppFactory factory)
    : IClassFixture<PostgresWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    
    private static string NewValidCpf()
    {
        var rnd = Random.Shared.Next(0, 999_999_999);
        var base9 = rnd.ToString("000000000");

        int sum1 = 0;
        for (int i = 0; i < 9; i++) sum1 += (base9[i] - '0') * (10 - i);
        int d1 = 11 - (sum1 % 11); if (d1 >= 10) d1 = 0;

        int sum2 = 0;
        for (int i = 0; i < 9; i++) sum2 += (base9[i] - '0') * (11 - i);
        sum2 += d1 * 2;
        int d2 = 11 - (sum2 % 11); if (d2 >= 10) d2 = 0;

        return base9 + d1.ToString() + d2.ToString();
    }

    private static object NewRetreatOpen(string name)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            name = new { value = $"{name}-{Guid.NewGuid():N}".Substring(0, 30) },
            edition = "ED-SRV",
            theme = "Tema",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots = 10,
            femaleSlots = 10,
            registrationStart = today.AddDays(-1).ToString("yyyy-MM-dd"),
            registrationEnd = today.AddDays(10).ToString("yyyy-MM-dd"),
            feeFazer = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewRetreatClosed(string name)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            name = new { value = $"{name}-{Guid.NewGuid():N}".Substring(0, 30) },
            edition = "ED-CLOSED",
            theme = "Tema",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots = 10,
            femaleSlots = 10,
            registrationStart = today.AddDays(-10).ToString("yyyy-MM-dd"),
            registrationEnd = today.AddDays(-5).ToString("yyyy-MM-dd"),
            feeFazer = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewCreateSpace(string name, bool isActive, int min, int max, string? desc = null)
        => new { name, description = desc, minPeople = min, maxPeople = max, isActive };

    private static object NewServiceReg(Guid retreatId, string name, string cpf, string email, int gender,
        string city = "SP", string region = "Oeste", Guid? pref = null)
        => new
        {
            retreatId,
            name = new { value = name },
            cpf = new { value = cpf },
            email = new { value = email },
            phone = "11999999999",
            birthDate = "1990-01-01",
            gender,
            city,
            region,
            preferredSpaceId = pref
        };

    private static object ToggleLock(bool locked) => new { @lock = locked };
    private sealed class CreatedRetreat { public Guid RetreatId { get; set; } }
    private sealed class CreateSpaceResp { public Guid SpaceId { get; set; } public int Version { get; set; } }
    private sealed class CreateServiceRegistrationResp { public Guid ServiceRegistrationId { get; set; } }

    private sealed class ListSpacesResp { public int Version { get; set; } public List<SpaceListItem> Items { get; set; } = new(); }
    private sealed class SpaceListItem
    {
        public Guid SpaceId { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public int MinPeople { get; set; }
        public int MaxPeople { get; set; }
        public int Allocated { get; set; }
    }

    private sealed class DetailResp
    {
        public int Version { get; set; }
        public SpaceView Space { get; set; } = null!;
        public int TotalMembers { get; set; }
        public List<MemberItem> Members { get; set; } = new();
    }
    private sealed class SpaceView
    {
        public Guid SpaceId { get; set; }
        public string Name { get; set; } = "";
        public bool HasCoordinator { get; set; }
        public bool HasVice { get; set; }
        public int MinPeople { get; set; }
        public int MaxPeople { get; set; }
    }
    private sealed class MemberItem
    {
        public Guid RegistrationId { get; set; }
        public string Role { get; set; } = "";
    }

    private sealed class UpdateRosterResp
    {
        public int Version { get; set; }
        public List<RosterMsg> Errors { get; set; } = new();
        public List<RosterMsg> Warnings { get; set; } = new();
    }
    private sealed class RosterMsg
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
    }
    

    [Fact]
    public async Task Registrations_validations_and_roster_editing_end_to_end()
    {
        // --- Retiro principal (aberto) e spaces ---
        var retOpen = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatOpen("Retiro REG-ROSTER"));
        retOpen.StatusCode.Should().Be(HttpStatusCode.Created, await retOpen.Content.ReadAsStringAsync());
        var retreatId = (await retOpen.Content.ReadFromJsonAsync<CreatedRetreat>(Json))!.RetreatId;

        var sA = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces",
            NewCreateSpace("Espaço A", isActive: true, min: 2, max: 5, "Principal"));
        sA.StatusCode.Should().Be(HttpStatusCode.OK, await sA.Content.ReadAsStringAsync());
        var spaceA = (await sA.Content.ReadFromJsonAsync<CreateSpaceResp>(Json))!.SpaceId;

        var sB = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces",
            NewCreateSpace("Espaço B", isActive: true, min: 1, max: 3, "Auxiliar"));
        sB.StatusCode.Should().Be(HttpStatusCode.OK);
        var spaceB = (await sB.Content.ReadFromJsonAsync<CreateSpaceResp>(Json))!.SpaceId;

        // --- Retiro fechado ---
        var retClosed = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatClosed("Retiro JAN-FECH"));
        retClosed.StatusCode.Should().Be(HttpStatusCode.Created);
        var retreatClosedId = (await retClosed.Content.ReadFromJsonAsync<CreatedRetreat>(Json))!.RetreatId;

        // --- Outro retiro + espaço extra ---
        var retOther = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatOpen("Retiro OUTRO"));
        retOther.StatusCode.Should().Be(HttpStatusCode.Created);
        var retreatOtherId = (await retOther.Content.ReadFromJsonAsync<CreatedRetreat>(Json))!.RetreatId;

        var sOther = await _client.PostAsJsonAsync($"/api/retreats/{retreatOtherId}/service/spaces",
            NewCreateSpace("Espaço OUT", isActive: true, min: 0, max: 10));
        sOther.StatusCode.Should().Be(HttpStatusCode.OK);
        var spaceOther = (await sOther.Content.ReadFromJsonAsync<CreateSpaceResp>(Json))!.SpaceId;

        // --- POST /service/registrations: sucesso ---
        var cpfCoord = NewValidCpf();
        var ok1 = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Coord QA", cpfCoord, $"coord{Guid.NewGuid():N}@t.com", 0, pref: spaceA));
        ok1.StatusCode.Should().Be(HttpStatusCode.Created);
        var regCoord = (await ok1.Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json))!.ServiceRegistrationId;

        // --- Duplicado por CPF (mesmo retiro) => 400 ---
        var dupCpf = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Dup CPF", cpfCoord, $"dup{Guid.NewGuid():N}@t.com", 0, pref: spaceA));
        dupCpf.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // --- e-mail duplicado permitido ---
        var sameEmail = $"vice{Guid.NewGuid():N}@t.com";
        var ok2 = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Vice QA", NewValidCpf(), sameEmail, 0, pref: spaceA));
        ok2.StatusCode.Should().Be(HttpStatusCode.Created);
        var regVice = (await ok2.Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json))!.ServiceRegistrationId;

        // --- preferredSpaceId de OUTRO retiro => 400 ---
        var wrongPref = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Wrong Pref", NewValidCpf(), $"wp{Guid.NewGuid():N}@t.com", 1, pref: spaceOther));
        wrongPref.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // --- Janela Fechada => 400 ---
        var closedReg = await _client.PostAsJsonAsync($"/api/retreats/{retreatClosedId}/service/registrations",
            NewServiceReg(retreatClosedId, "Jan Fech", NewValidCpf(), $"jf{Guid.NewGuid():N}@t.com", 1, pref: null));
        closedReg.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // --- GET by id 200 e 404 (outro retiro) ---
        var byIdOk = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/{regCoord}");
        byIdOk.StatusCode.Should().Be(HttpStatusCode.OK);

        var byIdWrongRetreat = await _client.GetAsync($"/api/retreats/{retreatOtherId}/service/registrations/{regCoord}");
        byIdWrongRetreat.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // --- Unassigned e confirmed ---
        var unassigned = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/roster/unassigned");
        unassigned.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmed = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/confirmed");
        confirmed.StatusCode.Should().Be(HttpStatusCode.OK);

        // --- Mais membros para roster ---
        var r3 = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "M1 QA", NewValidCpf(), $"m1{Guid.NewGuid():N}@t.com", 0, city: "SP", pref: spaceA));
        r3.StatusCode.Should().Be(HttpStatusCode.Created);
        var regM1 = (await r3.Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json))!.ServiceRegistrationId;

        var r4 = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "M2 QA", NewValidCpf(), $"m2{Guid.NewGuid():N}@t.com", 1, city: "RJ", pref: spaceA));
        r4.StatusCode.Should().Be(HttpStatusCode.Created);
        var regM2 = (await r4.Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json))!.ServiceRegistrationId;

        var r5 = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Unassigned QA", NewValidCpf(), $"u{Guid.NewGuid():N}@t.com", 1, city: "SP", pref: spaceB));
        r5.StatusCode.Should().Be(HttpStatusCode.Created);
        var regUnassigned = (await r5.Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json))!.ServiceRegistrationId;

        // --- Versão corrente (para roster) ---
        var listBefore = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        listBefore.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = (await listBefore.Content.ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;

        // --- PUT roster (feliz) ---
        var rosterOkBody = new
        {
            retreatId,
            version,
            spaces = new[]
            {
                new
                {
                    spaceId = spaceA,
                    name = (string?)null,
                    members = new[]
                    {
                        new { registrationId = regCoord, role = 1, position = 0 },
                        new { registrationId = regVice,  role = 2, position = 1 },
                        new { registrationId = regM1,   role = 0, position = 2 },
                        new { registrationId = regM2,   role = 0, position = 3 }
                    }
                }
            },
            ignoreWarnings = true
        };

        var putRosterOk = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", rosterOkBody);
        putRosterOk.StatusCode.Should().Be(HttpStatusCode.OK, await putRosterOk.Content.ReadAsStringAsync());
        var putRosterOkDto = await putRosterOk.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        putRosterOkDto!.Errors.Should().BeEmpty();
        putRosterOkDto.Warnings.Should().BeEmpty();

        // confere detail A
        var detailA = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}");
        detailA.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailADto = await detailA.Content.ReadFromJsonAsync<DetailResp>(Json);
        detailADto!.Space.HasCoordinator.Should().BeTrue();
        detailADto.Space.HasVice.Should().BeTrue();

        // --- Versão desatualizada (gera erro na resposta) ---
        var bump = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces/{spaceB}/lock", ToggleLock(true));
        bump.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var putStale = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", rosterOkBody);
        putStale.StatusCode.Should().Be(HttpStatusCode.OK);
        var putStaleDto = await putStale.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        putStaleDto!.Errors.Should().NotBeEmpty("deve acusar versão desatualizada e não aplicar");

        // --- SPACE_LOCKED ---
        var lockA = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}/lock", ToggleLock(true));
        lockA.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listForLocked = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        var vLocked = (await listForLocked.Content.ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;

        var tryEditLocked = new
        {
            retreatId,
            version = vLocked,
            spaces = new[]
            {
                new
                {
                    spaceId = spaceA,
                    name = (string?)null,
                    members = new[]
                    {
                        new { registrationId = regCoord, role = 1, position = 0 },
                        new { registrationId = regVice,  role = 2, position = 1 }
                    }
                }
            },
            ignoreWarnings = true
        };
        var lockedPut = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", tryEditLocked);
        lockedPut.StatusCode.Should().Be(HttpStatusCode.OK);
        var lockedDto = await lockedPut.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        lockedDto!.Errors.Should().NotBeEmpty("deve acusar SPACE_LOCKED");

        var unlockA = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}/lock", ToggleLock(false));
        unlockA.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // --- UNKNOWNs / WRONG_RETREAT / duplicidades ---
        var listV = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        var vNow = (await listV.Content.ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;

        var fakeSpace = Guid.NewGuid();
        var fakeReg = Guid.NewGuid();

        var errorsBody = new
        {
            retreatId,
            version = vNow,
            spaces = new object[]
            {
                new { spaceId = fakeSpace, name = (string?)null, members = Array.Empty<object>() },
                new { spaceId = spaceOther, name = (string?)null, members = Array.Empty<object>() },

                new {
                    spaceId = spaceA,
                    name = (string?)null,
                    members = new[] {
                        new { registrationId = regM1, role = 0, position = 0 }
                    }
                },
                new {
                    spaceId = spaceB,
                    name = (string?)null,
                    members = new[] {
                        new { registrationId = regM1, role = 0, position = 0 }
                    }
                },

                new {
                    spaceId = spaceA,
                    name = (string?)null,
                    members = new[] {
                        new { registrationId = fakeReg, role = 0, position = 9 }
                    }
                },

                new {
                    spaceId = spaceA,
                    name = (string?)null,
                    members = new[] {
                        new { registrationId = regCoord, role = 1, position = 0 },
                        new { registrationId = regVice,  role = 1, position = 1 },
                    }
                }
            },
            ignoreWarnings = true
        };

        var putErrors = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", errorsBody);
        putErrors.StatusCode.Should().Be(HttpStatusCode.OK, await putErrors.Content.ReadAsStringAsync());
        var putErrorsDto = await putErrors.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        putErrorsDto!.Errors.Should().NotBeEmpty();

        // --- Warnings (BelowMin, OverMax, MissingCoordinator/MissingVice) ---
        var vWarn = (await (await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces")).Content
            .ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;

        // cria extras p/ OverMax
        var ex1Resp = await (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Extra1", NewValidCpf(), $"ex1{Guid.NewGuid():N}@t.com", 0, pref: spaceB)))
            .Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json);
        var ex1 = ex1Resp!.ServiceRegistrationId;

        var ex2Resp = await (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Extra2", NewValidCpf(), $"ex2{Guid.NewGuid():N}@t.com", 0, pref: spaceB)))
            .Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json);
        var ex2 = ex2Resp!.ServiceRegistrationId;

        var ex3Resp = await (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations",
            NewServiceReg(retreatId, "Extra3", NewValidCpf(), $"ex3{Guid.NewGuid():N}@t.com", 1, pref: spaceB)))
            .Content.ReadFromJsonAsync<CreateServiceRegistrationResp>(Json);
        var ex3 = ex3Resp!.ServiceRegistrationId;

        var warnBodyNoApply = new
        {
            retreatId,
            version = vWarn,
            spaces = new[]
            {
                new
                {
                    spaceId = spaceB,
                    name = (string?)null,
                    members = new[]
                    {
                        new { registrationId = regUnassigned, role = 0, position = 0 },
                        new { registrationId = ex1,          role = 0, position = 1 },
                        new { registrationId = ex2,          role = 0, position = 2 },
                        new { registrationId = ex3,          role = 0, position = 3 }
                    }
                }
            },
            ignoreWarnings = false
        };
        var putWarnNoApply = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", warnBodyNoApply);
        var bodyWarnNoApply = await putWarnNoApply.Content.ReadAsStringAsync();
        putWarnNoApply.StatusCode.Should().Be(
            HttpStatusCode.BadRequest,
            $"esperado 400 quando warnings presentes com ignoreWarnings=false. Body: {bodyWarnNoApply}"
        );

        // versão não muda
        var vAfterNoApply = (await (await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces")).Content
            .ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;
        vAfterNoApply.Should().Be(vWarn);

        // 2) ignoreWarnings=true => 200 (aplica) OU 400 (VALIDATION_ERROR) a depender da política atual
        var warnBodyApply = new
        {
            retreatId,
            version = vWarn,
            spaces = new[]
            {
                new
                {
                    spaceId = spaceB,
                    name = (string?)null,
                    members = new[]
                    {
                        new { registrationId = regUnassigned, role = 0, position = 0 },
                        new { registrationId = ex1,          role = 0, position = 1 },
                        new { registrationId = ex2,          role = 0, position = 2 },
                        new { registrationId = ex3,          role = 0, position = 3 }
                    }
                }
            },
            ignoreWarnings = true
        };
        var putWarnApply = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", warnBodyApply);
        var bodyWarnApply = await putWarnApply.Content.ReadAsStringAsync();

        if (putWarnApply.StatusCode == HttpStatusCode.OK)
        {
            var vAfterApply = (await (await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces")).Content
                .ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;
            vAfterApply.Should().BeGreaterThan(vWarn);
        }
        else
        {
            putWarnApply.StatusCode.Should().Be(
                HttpStatusCode.BadRequest,
                $"esperado 200 OU 400 dependendo da política atual. Body: {bodyWarnApply}"
            );
            bodyWarnApply.Should().Contain("VALIDATION_ERROR");
            var vStill = (await (await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces")).Content
                .ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;
            vStill.Should().Be(vWarn);
        }

        // --- Alerts (200 nas três modalidades) ---
        var aAll = await _client.GetAsync($"/api/retreats/{retreatId}/service/alerts?mode=all");
        aAll.StatusCode.Should().Be(HttpStatusCode.OK);

        var aPref = await _client.GetAsync($"/api/retreats/{retreatId}/service/alerts?mode=preferences");
        aPref.StatusCode.Should().Be(HttpStatusCode.OK);

        var aRoster = await _client.GetAsync($"/api/retreats/{retreatId}/service/alerts?mode=roster");
        aRoster.StatusCode.Should().Be(HttpStatusCode.OK);

        // MOVE A → B  (e garante version bump e contagens)

        var unlockB = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces/{spaceB}/lock", ToggleLock(false));
        unlockB.StatusCode.Should().Be(HttpStatusCode.NoContent, await unlockB.Content.ReadAsStringAsync());

        // Recarrega a versão mais recente AGORA (após o unlock) para usar no payload do move
        var listForMove = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        listForMove.StatusCode.Should().Be(HttpStatusCode.OK);
        var listForMoveDto = await listForMove.Content.ReadFromJsonAsync<ListSpacesResp>(Json);
        var vPreMove = listForMoveDto!.Version;

        // helper local: map "Coordinator"/"Vice"/outros -> 1/2/0
        int ToRoleEnum(string r) =>
            r == "Coordinator" ? 1 :
            r == "Vice"        ? 2 : 0;

        // pega detalhes atuais de A e B
        var detailABefore = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}");
        detailABefore.StatusCode.Should().Be(HttpStatusCode.OK);
        var aBefore = await detailABefore.Content.ReadFromJsonAsync<DetailResp>(Json);

        var detailBBefore = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces/{spaceB}");
        detailBBefore.StatusCode.Should().Be(HttpStatusCode.OK);
        var bBefore = await detailBBefore.Content.ReadFromJsonAsync<DetailResp>(Json);

        var aPre = aBefore!.Members.Count;
        var bPre = bBefore!.Members.Count;

        // constrói nova lista de membros de A (remove regM1) e de B (adiciona regM1 como Member no fim)
        var aMembersNew = aBefore.Members
            .Where(m => m.RegistrationId != regM1)
            .Select((m, idx) => new {
                registrationId = m.RegistrationId,
                role = ToRoleEnum(m.Role),
                position = idx
            })
            .ToArray();

        var bMembersNew = bBefore.Members
            .Select((m, idx) => new {
                registrationId = m.RegistrationId,
                role = ToRoleEnum(m.Role),
                position = idx
            })
            .Concat(new[] {
                new { registrationId = regM1, role = 0, position = bPre }
            })
            .ToArray();

        var moveBody = new
        {
            retreatId,
            version = vPreMove,
            spaces = new[]
            {
                new { spaceId = spaceA, name = (string?)null, members = aMembersNew },
                new { spaceId = spaceB, name = (string?)null, members = bMembersNew }
            },
            ignoreWarnings = true
        };

        var putMove = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", moveBody);
        putMove.StatusCode.Should().Be(HttpStatusCode.OK, await putMove.Content.ReadAsStringAsync());
        var putMoveDto = await putMove.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        putMoveDto!.Errors.Should().BeEmpty();

        var listPostMove = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        var listPostMoveDto = await listPostMove.Content.ReadFromJsonAsync<ListSpacesResp>(Json);
        listPostMoveDto!.Version.Should().BeGreaterThan(vPreMove);

        var aPos = listPostMoveDto.Items.Single(i => i.SpaceId == spaceA).Allocated;
        var bPos = listPostMoveDto.Items.Single(i => i.SpaceId == spaceB).Allocated;
        aPos.Should().Be(aPre - 1, "A perdeu 1 membro");
        bPos.Should().Be(bPre + 1, "B ganhou 1 membro");

        // ============================================================
        // ROLES SWAP (coord ↔ vice) em A com partial update
        // ============================================================

        // pega estado atual de A
        var detailABeforeSwap = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}");
        detailABeforeSwap.StatusCode.Should().Be(HttpStatusCode.OK);
        var aSwapBefore = await detailABeforeSwap.Content.ReadFromJsonAsync<DetailResp>(Json);

        // identifica coord & vice atuais em A
        var currCoord = aSwapBefore!.Members.FirstOrDefault(m => m.Role == "Coordinator")?.RegistrationId;
        var currVice  = aSwapBefore.Members.FirstOrDefault(m => m.Role == "Vice")?.RegistrationId;
        currCoord.Should().NotBeNull("deve haver um coordenador em A");
        currVice.Should().NotBeNull("deve haver um vice em A");

        // monta o payload só para A, invertendo as funções
        int ToSwappedRole(MemberItem m)
        {
            if (m.RegistrationId == currCoord) return 2; // vira Vice
            if (m.RegistrationId == currVice)  return 1; // vira Coord
            return 0;
        }

        // versão atual para o swap
        var listForSwap = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        listForSwap.StatusCode.Should().Be(HttpStatusCode.OK);
        var vForSwap = (await listForSwap.Content.ReadFromJsonAsync<ListSpacesResp>(Json))!.Version;

        var aMembersSwapped = aSwapBefore.Members
            .Select((m, idx) => new
            {
                registrationId = m.RegistrationId,
                role = ToSwappedRole(m),
                position = idx
            })
            .ToArray();

        var swapBody = new
        {
            retreatId,
            version = vForSwap,
            spaces = new[]
            {
                new { spaceId = spaceA, name = (string?)null, members = aMembersSwapped }
            },
            ignoreWarnings = true
        };

        var putSwap = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/service/roster", swapBody);
        putSwap.StatusCode.Should().Be(HttpStatusCode.OK, await putSwap.Content.ReadAsStringAsync());
        var putSwapDto = await putSwap.Content.ReadFromJsonAsync<UpdateRosterResp>(Json);
        putSwapDto!.Errors.Should().BeEmpty("troca de roles não deve gerar DUPLICATE_LEADER");

        // version bump após o swap
        var listAfterSwap = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces");
        var listAfterSwapDto = await listAfterSwap.Content.ReadFromJsonAsync<ListSpacesResp>(Json);
        listAfterSwapDto!.Version.Should().BeGreaterThan(vForSwap);

        // confere que as funções foram trocadas
        var detailAAfterSwap = await _client.GetAsync($"/api/retreats/{retreatId}/service/spaces/{spaceA}");
        var aSwapAfter = await detailAAfterSwap.Content.ReadFromJsonAsync<DetailResp>(Json);

        var newCoord = aSwapAfter!.Members.FirstOrDefault(m => m.Role == "Coordinator")?.RegistrationId;
        var newVice  = aSwapAfter.Members.FirstOrDefault(m => m.Role == "Vice")?.RegistrationId;

        newCoord.Should().Be(currVice, "o vice anterior virou coordenador");
        newVice.Should().Be(currCoord, "o coordenador anterior virou vice");
    }
}
