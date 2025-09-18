using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RabbitMQ.Client;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class FamiliesEndToEndTests(RabbitOutboxWebAppFactory factory) : IClassFixture<RabbitOutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    private static object NewRetreatBodyOpenNow(string name = "Retiro FAM", int maleSlots = 20, int femaleSlots = 20)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var unique = $"{name}-{Guid.NewGuid():N}";
        return new
        {
            name = new { value = unique },
            edition = "ED-FAM",
            theme = "Tema",
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

    private const int GenderMale = 0;
    private const int GenderFemale = 1;
    private const int ParticipationGuest = 0;

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender, string city = "SP")
        => new
        {
            name  = new { value = name },
            cpf   = new { value = cpf },
            email = new { value = email },
            phone = "11999999999",
            birthDate = "1990-01-01",
            gender,
            city,
            participationCategory = ParticipationGuest,
            region = "Oeste",
            retreatId
        };

    private static object GenerateBody(int? capacity = null, bool replaceExisting = true, bool fillExistingFirst = false)
        => new { capacity, replaceExisting, fillExistingFirst };

    private static object LockRequest(bool locked) => new { @lock = locked };

    private async Task<Guid> CreateRegistrationAsync(object body)
    {
        var r = await _client.PostAsJsonAsync("/api/Registrations", body);
        var err = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.Created, $"registration create failed. Body: {err}");
        var dto = await r.Content.ReadFromJsonAsync<CreatedRegistrationDto>();
        dto.Should().NotBeNull();
        return dto!.RegistrationId;
    }
    
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    private async Task<IConnection> CreateRabbitConnectionAsync(RabbitOutboxWebAppFactory factory)
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port     = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        return await cf.CreateConnectionAsync("samtests-producer");
    }

    private async Task PublishPaymentConfirmedAsync(IEnumerable<Guid> registrationIds, decimal amount = 0m, string method = "pix")
    {
        using var conn = await CreateRabbitConnectionAsync(factory);
        using var ch   = await conn.CreateChannelAsync();

        const string exchange = "sam.topic";
        const string routing  = "payment.confirmed.v1";
        
        await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        foreach (var id in registrationIds)
        {
            var env = new
            {
                type = routing,
                data = new
                {
                    RegistrationId = id,
                    Amount = amount,
                    Method = method,
                    PaidAt = DateTimeOffset.UtcNow
                }
            };

            var json    = JsonSerializer.Serialize(env, _json);
            var payload = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(json));

            var props = new BasicProperties
            {
                DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent

            };

            await ch.BasicPublishAsync<BasicProperties>(
                exchange: exchange,
                routingKey: routing,
                mandatory: false,
                basicProperties: props,
                body: payload,
                cancellationToken: default
            );
        }
    }
    
    private async Task WaitUntilFamiliesAppearAsync(Guid retreatId, int expectedCount = 1, int capacity = 4, bool replaceExisting = true, bool fillExistingFirst = false, int timeoutMs = 10000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var gen = await _client.PostAsJsonAsync(
                $"/api/retreats/{retreatId}/families/generate",
                new { capacity, replaceExisting, fillExistingFirst }
            );
            gen.StatusCode.Should().Be(HttpStatusCode.OK, await gen.Content.ReadAsStringAsync());

            
            var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
            list.StatusCode.Should().Be(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());
            var listDto = await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();

            if (listDto is not null && listDto.Families.Count >= expectedCount)
                return;

            await Task.Delay(300);
        }

        throw new Xunit.Sdk.XunitException($"Famílias não apareceram após {timeoutMs}ms (esperado >= {expectedCount}).");
    }


    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class CreatedRegistrationDto { public Guid RegistrationId { get; set; } }
    
    private sealed class GenerateResponse
    {
        public int Version { get; set; }
        public List<GeneratedFamilyDto> Families { get; set; } = new();
    }
    private sealed class GeneratedFamilyDto
    {
        public Guid FamilyId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public int TotalMembers { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public int Remaining { get; set; }
        public List<GeneratedMemberDto> Members { get; set; } = new();
        public List<AlertDto> Alerts { get; set; } = new();
    }
    private sealed class GeneratedMemberDto
    {
        public Guid RegistrationId { get; set; }
        public string Name { get; set; } = "";
        public string Gender { get; set; } = "";
        public string? City { get; set; }
        public int Position { get; set; }
    }
    private sealed class AlertDto
    {
        public string Severity { get; set; } = "";
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
        public List<Guid> RegistrationIds { get; set; } = new();
    }

    private sealed class GetAllFamiliesResponse
    {
        public int Version { get; set; }
        public bool FamiliesLocked { get; set; }
        public List<FamilyReadDto> Families { get; set; } = new();
    }
    private sealed class GetByIdResponse
    {
        public int Version { get; set; }
        public FamilyReadDto? Family { get; set; }
    }
    private sealed class FamilyReadDto
    {
        public Guid FamilyId { get; set; }
        public string Name { get; set; } = "";
        public int Capacity { get; set; }
        public int TotalMembers { get; set; }
        public int MaleCount { get; set; } 
        public int FemaleCount { get; set; } 
        public int Remaining { get; set; }
        public bool IsLocked { get; set; }
        public List<MemberView> Members { get; set; } = new();
        public List<AlertDto> Alerts { get; set; } = new();
    }
    private sealed class MemberView
    {
        public Guid RegistrationId { get; set; }
        public string Name { get; set; } = "";
        public string Gender { get; set; } = "";
        public string City { get; set; } = "";
        public int Position { get; set; }
    }

    private sealed class LockFamiliesResponse { public int Version { get; set; } public bool Locked { get; set; } }
    

    [Fact]
    public async Task Families_happy_flow_generate_list_get_update_delete()
    {
       
        var postRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        var postRetErr = await postRet.Content.ReadAsStringAsync();
        postRet.StatusCode.Should().Be(HttpStatusCode.Created, $"retreat create failed. Body: {postRetErr}");
        var created = await postRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;
        
        var r1 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Joao Silva",   "52998224725", "m1@fam.com", GenderMale,   "Recife"));
        var r2 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Pedro Costa",  "15350946056", "m2@fam.com", GenderMale,   "SP"));
        var r3 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Ana Souza",    "93541134780", "f1@fam.com", GenderFemale, "SP"));
        var r4 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "Beatriz Lima", "28625587887", "f2@fam.com", GenderFemale, "RJ"));

        await PublishPaymentConfirmedAsync(new[] { r1, r2, r3, r4 });

        
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, capacity: 4, replaceExisting: true);

        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families?includeAlerts=true");
        var listErr = await list.Content.ReadAsStringAsync();
        list.StatusCode.Should().Be(HttpStatusCode.OK, $"list failed. Body: {listErr}");
        var listDto = await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
        listDto.Should().NotBeNull();
        listDto!.FamiliesLocked.Should().BeFalse();
        listDto.Families.Should().HaveCount(1);
        listDto.Families.First().Capacity.Should().Be(4);
        listDto.Families.First().TotalMembers.Should().Be(4);

        var famId = listDto.Families.First().FamilyId;
        var versionAfterGen = listDto.Version;
        
        var byId = await _client.GetAsync($"/api/retreats/{retreatId}/families/{famId}?includeAlerts=true");
        var byIdErr = await byId.Content.ReadAsStringAsync();
        byId.StatusCode.Should().Be(HttpStatusCode.OK, $"get by id failed. Body: {byIdErr}");
        var byIdDto = await byId.Content.ReadFromJsonAsync<GetByIdResponse>();
        byIdDto.Should().NotBeNull();
        byIdDto!.Family.Should().NotBeNull();
        byIdDto.Family!.IsLocked.Should().BeFalse();
        byIdDto.Family.Members.Should().HaveCount(4).And.OnlyHaveUniqueItems(m => m.Position);
        
        var moved = byIdDto.Family.Members
            .OrderBy(m => m.Position)
            .Select(m => new { registrationId = m.RegistrationId, position = (m.Position + 1) % 4 })
            .ToList();

        var putBody = new
        {
            version = versionAfterGen,
            families = new[] {
                new {
                    familyId = famId,
                    name = byIdDto.Family.Name,
                    capacity = 4,
                    members = moved
                }
            },
            ignoreWarnings = true
        };
        var put = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/families", putBody);
        var putErr = await put.Content.ReadAsStringAsync();
        put.StatusCode.Should().Be(HttpStatusCode.OK, $"put failed. Body: {putErr}");

        var afterPutList = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var afterPutDto = await afterPutList.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
        afterPutDto.Should().NotBeNull();
        afterPutDto!.Version.Should().Be(versionAfterGen + 1);
        
        var del = await _client.DeleteAsync($"/api/retreats/{retreatId}/families/{famId}");
        var delErr = await del.Content.ReadAsStringAsync();
        del.StatusCode.Should().Be(HttpStatusCode.NoContent, $"delete failed. Body: {delErr}");

        var byId404 = await _client.GetAsync($"/api/retreats/{retreatId}/families/{famId}");
        byId404.StatusCode.Should().Be(HttpStatusCode.OK);
        var byId404Dto = await byId404.Content.ReadFromJsonAsync<GetByIdResponse>();
        byId404Dto.Should().NotBeNull();
        byId404Dto!.Family.Should().BeNull();
    }

    [Fact]
    public async Task Global_and_single_locks_are_enforced()
    {
       
        var postRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow("Retiro Locks"));
        var postRetErr = await postRet.Content.ReadAsStringAsync();
        postRet.StatusCode.Should().Be(HttpStatusCode.Created, $"retreat create failed. Body: {postRetErr}");
        var retreatId = (await postRet.Content.ReadFromJsonAsync<CreatedRetreatDto>())!.RetreatId;
        
        var m1 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "M1 Teste", "52998224725", "m1@locks.com", GenderMale));
        var m2 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "M2 Teste", "15350946056", "m2@locks.com", GenderMale));
        var f1 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "F1 Teste", "93541134780", "f1@locks.com", GenderFemale));
        var f2 = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "F2 Teste", "28625587887", "f2@locks.com", GenderFemale));
        
        await PublishPaymentConfirmedAsync(new[] { m1, m2, f1, f2 });
        await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, capacity: 4, replaceExisting: true);
        
        var listAfterGen = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var listAfterGenBody = await listAfterGen.Content.ReadAsStringAsync();
        listAfterGen.StatusCode.Should().Be(HttpStatusCode.OK, $"list after generate failed. Body: {listAfterGenBody}");
        var listAfterGenDto = await listAfterGen.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
        listAfterGenDto.Should().NotBeNull();
        listAfterGenDto!.Families.Should().HaveCount(1);
        var famId = listAfterGenDto.Families.First().FamilyId;
        
        var lockFam = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/{famId}/lock", LockRequest(true));
        var lockFamErr = await lockFam.Content.ReadAsStringAsync();
        lockFam.StatusCode.Should().Be(HttpStatusCode.OK, $"family lock failed. Body: {lockFamErr}");
        
        var list = await _client.GetAsync($"/api/retreats/{retreatId}/families");
        var listDto = await list.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
        var version = listDto!.Version;

        var putBody = new
        {
            version,
            families = new[] {
                new {
                    familyId = famId,
                    name = listDto.Families[0].Name + " X",
                    capacity = 4,
                    members = listDto.Families[0].Members
                        .Select(m => new { registrationId = m.RegistrationId, position = m.Position })
                        .ToList()
                }
            },
            ignoreWarnings = true
        };
        var put = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/families", putBody);
        put.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        
        var lockGlobal = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/lock", LockRequest(true));
        var lockGlobalErr = await lockGlobal.Content.ReadAsStringAsync();
        lockGlobal.StatusCode.Should().Be(HttpStatusCode.OK, $"global lock failed. Body: {lockGlobalErr}");
        
        var genLocked = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/generate", GenerateBody(4, false));
        genLocked.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var putLocked = await _client.PutAsJsonAsync($"/api/retreats/{retreatId}/families", putBody);
        putLocked.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var delLocked = await _client.DeleteAsync($"/api/retreats/{retreatId}/families/{famId}");
        delLocked.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/lock", LockRequest(false));
        await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/families/{famId}/lock", LockRequest(false));

        var del = await _client.DeleteAsync($"/api/retreats/{retreatId}/families/{famId}");
        var delErr = await del.Content.ReadAsStringAsync();
        del.StatusCode.Should().Be(HttpStatusCode.NoContent, $"delete after unlock failed. Body: {delErr}");
    }

    [Fact]
public async Task Generate_fillExistingFirst_completes_unlocked_families_first()
{
    var postRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow("Retiro FillFirst"));
    var postRetErr = await postRet.Content.ReadAsStringAsync();
    postRet.StatusCode.Should().Be(HttpStatusCode.Created, $"retreat create failed. Body: {postRetErr}");
    var retreatId = (await postRet.Content.ReadFromJsonAsync<CreatedRetreatDto>())!.RetreatId;

    var a = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "M1 Teste", "52998224725", "a@a.com", GenderMale));
    var b = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "M2 Teste", "15350946056", "b@b.com", GenderMale));
    var c = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "F1 Teste", "93541134780", "c@c.com", GenderFemale));
    var d = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "F2 Teste", "28625587887", "d@d.com", GenderFemale));
    
    await PublishPaymentConfirmedAsync(new[] { a, b, c, d });
    await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, capacity: 4, replaceExisting: true);

    var listAfterGen1 = await _client.GetAsync($"/api/retreats/{retreatId}/families");
    listAfterGen1.StatusCode.Should().Be(HttpStatusCode.OK);
    var listAfterGen1Dto = await listAfterGen1.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
    listAfterGen1Dto.Should().NotBeNull();
    listAfterGen1Dto!.Families.Should().HaveCount(1);
    var famId = listAfterGen1Dto.Families.First().FamilyId;
    
    var e = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "M3 Extra", "11144477735", "e@e.com", GenderMale));
    var f = await CreateRegistrationAsync(NewRegistrationBody(retreatId, "F3 Extra", "76253361006", "f@f.com", GenderFemale));
    await PublishPaymentConfirmedAsync(new[] { e, f });

    
    await WaitUntilFamiliesAppearAsync(retreatId, expectedCount: 1, capacity: 4, replaceExisting: false, fillExistingFirst: true);

    
    var after = await _client.GetAsync($"/api/retreats/{retreatId}/families");
    after.StatusCode.Should().Be(HttpStatusCode.OK);
    var afterDto = await after.Content.ReadFromJsonAsync<GetAllFamiliesResponse>();
    afterDto.Should().NotBeNull();
    afterDto!.Families.Should().HaveCount(1);
    afterDto.Families.First(f => f.FamilyId == famId).TotalMembers.Should().Be(4);
}

}
