using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RabbitMQ.Client;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class FamilyGroupsE2ETests(RabbitOutboxWebAppFactory factory)
    : IClassFixture<RabbitOutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private const int GenderMale = 0;
    private const int GenderFemale = 1;
    private const int ParticipationGuest = 0;
    private static object NewRetreatBodyOpenNow(string name = "Retiro GRPS", int maleSlots = 20, int femaleSlots = 20)
    {
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var unique = $"{name} {Guid.NewGuid():N}".Substring(0, 30); 
        var edRand = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var edition = $"ED{edRand}"; 

        return new
        {
            name = new { value = unique },
            edition,
            theme = "Tema Base",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate   = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots,
            femaleSlots,
            registrationStart = today.AddDays(-1).ToString("yyyy-MM-dd"),
            registrationEnd   = today.AddDays(10).ToString("yyyy-MM-dd"),
            feeFazer  = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct  = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender) => new
    {
        name  = new { value = name },
        cpf   = new { value = cpf },
        email = new { value = email },

        phone = "11999999999",
        birthDate = "2000-01-01",
        gender,
        city = "SP",
        state = "SP",
        retreatId,

        maritalStatus = 1,
        pregnancy = 0,
        shirtSize = 3,
        weightKg = 80,
        heightCm = 180,
        profession = "Dev",
        streetAndNumber = "Rua A, 123",
        neighborhood = "Centro",

        whatsapp = "11988887777",
        neighborPhone = "1133334444",
        relativePhone = "11911112222",
        facebookUsername = "fulano.fb",
        instagramHandle  = "fulano.ig",

        fatherStatus = 1,
        fatherName = "Pai Teste",
        fatherPhone = "1133332222",
        motherStatus = 1,
        motherName = "Mae Teste",
        motherPhone = "11911113333",
        hadFamilyLossLast6Months = false,
        familyLossDetails = (string?)null,
        hasRelativeOrFriendSubmitted = false,
        submitterRelationship = 0,
        submitterNames = (string?)null,

        religion = "Católica",
        previousUncalledApplications = 0,
        rahaminVidaCompleted = 0,

        alcoholUse = 0,
        smoker = false,
        usesDrugs = false,
        drugUseFrequency = (int?)null,
        hasAllergies = false,
        allergiesDetails = (string?)null,
        hasMedicalRestriction = false,
        medicalRestrictionDetails = (string?)null,
        takesMedication = false,
        medicationsDetails = (string?)null,
        physicalLimitationDetails = (string?)null,
        recentSurgeryOrProcedureDetails = (string?)null,

        termsAccepted = true,
        termsVersion = "2025-10-01",
        marketingOptIn = true,
        clientIp = "127.0.0.1",
        userAgent = "IntegrationTest"
    };


    private static object LockRequest(bool locked) => new { @lock = locked };

    private static async Task AssertCreated(HttpResponseMessage r, string context)
    {
        if (r.StatusCode != HttpStatusCode.Created)
        {
            var body = await r.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected 201 Created ({context}) but got {(int)r.StatusCode}. Body:\n{body}");
        }
    }
    
    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class CreatedRegistrationDto { public Guid RegistrationId { get; set; } }

    private sealed class GetAllFamiliesResponse
    {
        public int Version { get; set; }
        public bool FamiliesLocked { get; set; }
        public List<FamilyReadDto> Families { get; set; } = new();
    }
    private sealed class FamilyReadDto
    {
        public Guid FamilyId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public int TotalMembers { get; set; }
    }

    private sealed class ListByStatusResponse { public List<FamilyGroupItem> Items { get; set; } = new(); }
    private sealed class FamilyGroupItem
    {
        public Guid FamilyId { get; set; }
        public string Name { get; set; } = "";
        public string GroupStatus { get; set; } = "";
        public string? GroupLink { get; set; }
        public string? GroupExternalId { get; set; }
        public string? GroupChannel { get; set; }
        public DateTimeOffset? GroupCreatedAt { get; set; }
        public DateTimeOffset? GroupLastNotifiedAt { get; set; }
        public int GroupVersion { get; set; }
    }
    
    private async Task<Guid> CreateRegistrationAsync(object body)
    {
        var r = await _client.PostAsJsonAsync("/api/Registrations", body);
        r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        var dto = await r.Content.ReadFromJsonAsync<CreatedRegistrationDto>(Json);
        dto.Should().NotBeNull();
        return dto!.RegistrationId;
    }

    private async Task<IConnection> CreateRabbitConnectionAsync()
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port     = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        return await cf.CreateConnectionAsync("samtests-e2e");
    }

    private async Task PublishPaymentConfirmedAsync(IEnumerable<Guid> regIds)
    {
        using var conn = await CreateRabbitConnectionAsync();
        using var ch   = await conn.CreateChannelAsync();

        const string exchange = "sam.topic";
        const string routing  = "payment.confirmed.v1";
        await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        foreach (var id in regIds)
        {
            var env = new { type = routing, data = new { RegistrationId = id, Amount = 0m, Method = "pix", PaidAt = DateTimeOffset.UtcNow } };
            var json    = JsonSerializer.Serialize(env, Json);
            var payload = Encoding.UTF8.GetBytes(json);
            var props = new BasicProperties { DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent };
            await ch.BasicPublishAsync(exchange, routing, false, props, payload);
        }
    }

    private async Task PublishFamilyGroupCreatedAsync(Guid retreatId, Guid familyId, string link, string channel = "whatsapp", string? externalId = "ext-1")
    {
        using var conn = await CreateRabbitConnectionAsync();
        using var ch   = await conn.CreateChannelAsync();

        const string exchange = "sam.topic";
        const string routing  = "family.group.created.v1";
        await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var env = new
        {
            type = routing,
            data = new
            {
                RetreatId = retreatId,
                FamilyId = familyId,
                Link = link,
                Channel = channel,
                ExternalId = externalId,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var json    = JsonSerializer.Serialize(env, Json);
        var payload = Encoding.UTF8.GetBytes(json);
        var props   = new BasicProperties { DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent };

        await ch.BasicPublishAsync(exchange, routing, false, props, payload);
    }

    private async Task WaitUntilFamiliesAppearAsync(Guid retreatId, int expectedCount, int membersPerFamily, bool replaceExisting, bool fillExistingFirst = false, int timeoutMs = 12_000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var gen = await _client.PostAsJsonAsync(
                $"/api/retreats/{retreatId}/families/generate",
                new { membersPerFamily, replaceExisting, fillExistingFirst }
            );
            gen.StatusCode.Should().Be(HttpStatusCode.OK, await gen.Content.ReadAsStringAsync());

            var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
            list.StatusCode.Should().Be(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());
            var listDto = await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>(Json);

            if (listDto is not null && listDto.Families.Count >= expectedCount)
                return;

            await Task.Delay(300);
        }

        throw new Xunit.Sdk.XunitException($"Famílias não apareceram após {timeoutMs}ms (esperado >= {expectedCount}).");
    }

    private async Task<FamilyGroupItem?> WaitForGroupStatusAsync(Guid retreatId, Guid familyId, string status, int timeoutSec = 60)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (DateTime.UtcNow < deadline)
        {
            var resp = await _client.GetAsync($"/admin/retreats/{retreatId}/groups?status={status}");
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(300);
                continue;
            }

            var dto = await resp.Content.ReadFromJsonAsync<ListByStatusResponse>(Json);
            var item = dto!.Items.SingleOrDefault(i => i.FamilyId == familyId);
            if (item is not null) return item;

            await Task.Delay(400);
        }
        return null;
    }
    

    [Fact]
    public async Task Bulk_core_transiciona_para_Creating_e_depois_de_created_v1_fica_Active_com_link()
    {
       
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        await AssertCreated(createRet, "retreat create");
        var retreatId = (await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>(Json))!.RetreatId;

        var r1 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Joao Silva",   "52998224725", "m1@f.com", GenderMale));
        var r2 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Pedro Costa",  "15350946056", "m2@f.com", GenderMale));
        var r3 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Ana Souza",    "93541134780", "f1@f.com", GenderFemale));
        var r4 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Beatriz Lima", "28625587887", "f2@f.com", GenderFemale));

        await PublishPaymentConfirmedAsync(new[] { r1, r2, r3, r4 });
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, membersPerFamily: 4, replaceExisting: true);

        
        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var famId = (await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>(Json))!.Families.First().FamilyId;
        
        (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/lock", LockRequest(true)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        
        (await _client.PostAsJsonAsync($"/admin/retreats/{retreatId}/groups",
            new { RetreatId = Guid.Empty, DryRun = false, ForceRecreate = false }))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);

        var creating = await WaitForGroupStatusAsync(retreatId, famId, "creating", timeoutSec: 60);
        creating.Should().NotBeNull("esperava Creating depois do bulk");
        var versionAfterCreating = creating!.GroupVersion;
        
        var fakeLink = $"https://chat/{Guid.NewGuid():N}";
        await PublishFamilyGroupCreatedAsync(retreatId, famId, fakeLink, "whatsapp", "ext-coretest");
        
        var active = await WaitForGroupStatusAsync(retreatId, famId, "active", timeoutSec: 60);
        active.Should().NotBeNull("esperava Active após created.v1");
        active!.GroupStatus.Should().Be("Active");
        active.GroupLink.Should().Be(fakeLink);
        active.GroupChannel.Should().Be("whatsapp");
        active.GroupCreatedAt.Should().NotBeNull();
        active.GroupVersion.Should().BeGreaterThanOrEqualTo(versionAfterCreating);
    }

    [Fact]
    public async Task Resend_retorna_202_e_nao_muda_estado_do_Core()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow("Retiro Reenvio"));
        await AssertCreated(createRet, "retreat create");
        var retreatId = (await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>(Json))!.RetreatId;

        var ids = new[]
        {
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "A A", "52998224725", "a@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "B B", "15350946056", "b@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "C C", "93541134780", "c@x.com", GenderFemale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "D D", "28625587887", "d@x.com", GenderFemale))
        };

        await PublishPaymentConfirmedAsync(ids);
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, membersPerFamily:4, replaceExisting: true);

        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var famId = (await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>(Json))!.Families.First().FamilyId;
        
        (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/lock", LockRequest(true)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/admin/retreats/{retreatId}/groups",
            new { RetreatId = Guid.Empty, DryRun = false, ForceRecreate = false }))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);

        var link = $"https://chat/{Guid.NewGuid():N}";
        await PublishFamilyGroupCreatedAsync(retreatId, famId, link, "whatsapp", "ext-resend");
        var active = await WaitForGroupStatusAsync(retreatId, famId, "active", timeoutSec: 60);
        active.Should().NotBeNull();
        var beforeVersion = active!.GroupVersion;
        
        var resend = await _client.PostAsync($"/admin/retreats/{retreatId}/groups/{famId}/resend", null);
        resend.StatusCode.Should().Be(HttpStatusCode.Accepted);
        
        var check = await WaitForGroupStatusAsync(retreatId, famId, "active", timeoutSec: 20);
        check.Should().NotBeNull();
        check!.GroupStatus.Should().Be("Active");
        check.GroupVersion.Should().Be(beforeVersion);
        check.GroupLink.Should().Be(link);
    }

    [Fact]
    public async Task Idempotencia_replay_de_created_v1_eh_ignorado()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow("Retiro Idemp"));
        await AssertCreated(createRet, "retreat create");
        var retreatId = (await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>(Json))!.RetreatId;

        var ids = new[]
        {
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "A A", "52998224725", "a@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "B B", "15350946056", "b@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "C C", "93541134780", "c@x.com", GenderFemale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "D D", "28625587887", "d@x.com", GenderFemale))
        };

        await PublishPaymentConfirmedAsync(ids);
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, membersPerFamily: 4, replaceExisting: true);

        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var famId = (await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>(Json))!.Families.First().FamilyId;

        (await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/lock", LockRequest(true)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/admin/retreats/{retreatId}/groups",
            new { RetreatId = Guid.Empty, DryRun = false, ForceRecreate = false }))
            .StatusCode.Should().Be(HttpStatusCode.Accepted);

        var link = $"https://chat/{Guid.NewGuid():N}";

        await PublishFamilyGroupCreatedAsync(retreatId, famId, link, "whatsapp", "ext-1");
        var active1 = await WaitForGroupStatusAsync(retreatId, famId, "active", timeoutSec: 60);
        active1.Should().NotBeNull();
        var beforeVersion = active1!.GroupVersion;
        
        await PublishFamilyGroupCreatedAsync(retreatId, famId, link, "whatsapp", "ext-1");

        var active2 = await WaitForGroupStatusAsync(retreatId, famId, "active", timeoutSec: 20);
        active2.Should().NotBeNull();
        active2!.GroupVersion.Should().Be(beforeVersion);
        active2.GroupLink.Should().Be(link);
    }

    [Fact]
    public async Task Regras_de_lock_sem_lock_retorna_400()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow("Retiro LocksGroups"));
        await AssertCreated(createRet, "retreat create");
        var retreatId = (await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>(Json))!.RetreatId;

        var ids = new[]
        {
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "A A", "52998224725", "a@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "B B", "15350946056", "b@x.com", GenderMale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "C C", "93541134780", "c@x.com", GenderFemale)),
            await CreateRegistrationAsync(NewRegistrationBody(retreatId, "D D", "28625587887", "d@x.com", GenderFemale))
        };

        await PublishPaymentConfirmedAsync(ids);
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, membersPerFamily: 4, replaceExisting: true);

        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var famId = (await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>(Json))!.Families.First().FamilyId;
        
        var bulk = await _client.PostAsJsonAsync($"/admin/retreats/{retreatId}/groups",
            new { RetreatId = Guid.Empty, DryRun = false, ForceRecreate = false });
        bulk.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var notify = await _client.PostAsJsonAsync($"/admin/retreats/{retreatId}/groups/{famId}/notify",
            new { RetreatId = Guid.Empty, FamilyId = Guid.Empty, ForceRecreate = false });
        notify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
