using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RabbitMQ.Client;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class ServiceRegistrationPaymentE2ETests(RabbitOutboxWebAppFactory factory)
    : IClassFixture<RabbitOutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private const int GenderMale = 0;
    private const int GenderFemale = 1;

    private static object NewRetreatBodyOpenNow(string name = "Retiro SERV")
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var unique = $"{name}-{Guid.NewGuid():N}".Substring(0, 30);
        return new
        {
            name = new { value = unique },
            edition = "ED-SRV",
            theme = "Tema",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate   = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots = 100,
            femaleSlots = 100,
            registrationStart = today.AddDays(-1).ToString("yyyy-MM-dd"),
            registrationEnd   = today.AddDays(10).ToString("yyyy-MM-dd"),
            feeFazer  = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct  = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewServiceSpaceBody(string name, int min = 0, int max = 10, bool active = true)
        => new { Name = name, Description = "e2e", MinPeople = min, MaxPeople = max, IsActive = active };

    private static object NewServiceRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender, Guid? preferredSpaceId)
        => new
        {
            retreatId,
            name = new { value = name },
            cpf = new { value = cpf },
            email = new { value = email },
            phone = "11999999999",
            birthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)).ToString("yyyy-MM-dd"),
            gender,
            city = "SP",
            region = "Oeste",
            preferredSpaceId
        };

    private async Task<Guid> CreateRetreatAsync()
    {
        var r = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        var err = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.Created, err);
        var dto = await r.Content.ReadFromJsonAsync<CreatedRetreatDto>(Json);
        dto.Should().NotBeNull();
        return dto!.RetreatId;
    }

    private async Task<(Guid spaceId, int version)> CreateSpaceAsync(Guid retreatId, string? name = null, int min = 0, int max = 10, bool active = true)
    {
        var body = NewServiceSpaceBody(name ?? $"SERV-{Guid.NewGuid():N}", min, max, active);
        var r = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces", body);
        var err = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.OK, $"space create failed. Body: {err}");
        var dto = await r.Content.ReadFromJsonAsync<CreateSpaceDto>(Json);
        dto.Should().NotBeNull();
        return (dto!.SpaceId, dto.Version);
    }

    private async Task<Guid> CreateServiceRegistrationAsync(Guid retreatId, string name, string cpf, string email, int gender, Guid? preferredSpaceId)
    {
        var body = NewServiceRegistrationBody(retreatId, name, cpf, email, gender, preferredSpaceId);
        var r = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/registrations", body);
        var err = await r.Content.ReadAsStringAsync();
        r.StatusCode.Should().Be(HttpStatusCode.Created, $"service registration failed. Body:\n{err}");
        var dto = await r.Content.ReadFromJsonAsync<CreateServiceRegDto>(Json);
        dto.Should().NotBeNull();
        return dto!.ServiceRegistrationId;
    }

    private async Task LockSpaceAsync(Guid retreatId, Guid spaceId, bool @lock)
    {
        var r = await _client.PostAsJsonAsync($"/api/retreats/{retreatId}/service/spaces/{spaceId}/lock", new { @lock });
        r.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task PublishPaymentConfirmedAsync(Guid registrationId, Guid? paymentId = null, decimal amount = 0m, string method = "pix")
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        using var conn = await cf.CreateConnectionAsync("samtests-service");
        using var ch = await conn.CreateChannelAsync();

        const string exchange = "sam.topic";
        const string routing = "payment.confirmed.v1";
        await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var env = new
        {
            type = routing,
            data = new
            {
                PaymentId = paymentId ?? Guid.NewGuid(),
                RegistrationId = registrationId,
                Amount = amount,
                Method = method,
                PaidAt = DateTimeOffset.UtcNow
            }
        };

        var json = JsonSerializer.Serialize(env, Json);
        var payload = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties { DeliveryMode = RabbitMQ.Client.DeliveryModes.Persistent };
        await ch.BasicPublishAsync(exchange, routing, false, props, payload);
    }

    private async Task WaitUntilRosterHasAsync(Guid retreatId, Guid spaceId, Guid registrationId, int timeoutMs = 30000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var resp = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/roster");
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var dto = await resp.Content.ReadFromJsonAsync<GetRosterDto>(Json);
                var space = dto!.Spaces.FirstOrDefault(s => s.SpaceId == spaceId);
                if (space != null && space.Members.Any(m => m.RegistrationId == registrationId))
                    return;
            }
            await Task.Delay(500);
        }
        throw new Xunit.Sdk.XunitException($"Registro {registrationId} não apareceu no roster do espaço {spaceId} em {timeoutMs}ms.");
    }

    private async Task WaitUntilRosterNotHasAsync(Guid retreatId, Guid spaceId, Guid registrationId, int timeoutMs = 10000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var resp = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/roster");
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                var dto = await resp.Content.ReadFromJsonAsync<GetRosterDto>(Json);
                var space = dto!.Spaces.FirstOrDefault(s => s.SpaceId == spaceId);
                var has = space != null && space.Members.Any(m => m.RegistrationId == registrationId);
                if (!has) return;
            }
            await Task.Delay(500);
        }
        throw new Xunit.Sdk.XunitException($"Registro {registrationId} ainda está no roster do espaço {spaceId} após {timeoutMs}ms.");
    }

    [Fact]
    public async Task Happy_flow_service_registration_payment_autoassign_preferred_space()
    {
        var retreatId = await CreateRetreatAsync();
        var (spaceId, _) = await CreateSpaceAsync(retreatId, $"SERV-{Guid.NewGuid():N}", 0, 10, true);

        var regId = await CreateServiceRegistrationAsync(
            retreatId,
            "Joao Servidor",
            "52998224725",
            $"serv-{Guid.NewGuid():N}@e2e.com",
            GenderMale,
            spaceId
        );

        await PublishPaymentConfirmedAsync(regId);
        await WaitUntilRosterHasAsync(retreatId, spaceId, regId, 30000);
    }

    [Fact]
    public async Task No_autoassign_when_space_locked()
    {
        var retreatId = await CreateRetreatAsync();
        var (spaceId, _) = await CreateSpaceAsync(retreatId, $"LOCK-{Guid.NewGuid():N}", 0, 10, true);

        await LockSpaceAsync(retreatId, spaceId, true);

        var regId = await CreateServiceRegistrationAsync(
            retreatId,
            "Maria Servidora",
            "93541134780",
            $"serv-{Guid.NewGuid():N}@e2e.com",
            GenderFemale,
            spaceId
        );

        await PublishPaymentConfirmedAsync(regId);
        await WaitUntilRosterNotHasAsync(retreatId, spaceId, regId, 10000);
    }

    [Fact]
    public async Task Idempotent_on_duplicate_payment_event_only_one_assignment()
    {
        var retreatId = await CreateRetreatAsync();
        var (spaceId, _) = await CreateSpaceAsync(retreatId, $"IDEMP-{Guid.NewGuid():N}", 0, 10, true);

        var regId = await CreateServiceRegistrationAsync(
            retreatId,
            "Pedro Idempotente",
            "15350946056",
            $"serv-{Guid.NewGuid():N}@e2e.com",
            GenderMale,
            spaceId
        );

        var paymentId = Guid.NewGuid();
        await PublishPaymentConfirmedAsync(regId, paymentId);
        await PublishPaymentConfirmedAsync(regId, paymentId);

        await WaitUntilRosterHasAsync(retreatId, spaceId, regId, 30000);

        var roster = await _client.GetAsync($"/api/retreats/{retreatId}/service/registrations/roster");
        roster.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await roster.Content.ReadFromJsonAsync<GetRosterDto>(Json);
        var members = dto!.Spaces.First(s => s.SpaceId == spaceId).Members.Where(m => m.RegistrationId == regId).ToList();
        members.Count.Should().Be(1);
    }

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class CreateSpaceDto { public Guid SpaceId { get; set; } public int Version { get; set; } }
    private sealed class CreateServiceRegDto { public Guid ServiceRegistrationId { get; set; } }
    private sealed class GetRosterDto { public int Version { get; set; } public List<RosterSpace> Spaces { get; set; } = new(); }
    private sealed class RosterSpace { public Guid SpaceId { get; set; } public string Name { get; set; } = ""; public List<RosterMember> Members { get; set; } = new(); }
    private sealed class RosterMember { public Guid RegistrationId { get; set; } public string Name { get; set; } = ""; public int Position { get; set; } public string? City { get; set; } }
}
