using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RabbitMQ.Client;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class AdminNotificationsRabbitE2ETests(RabbitOutboxWebAppFactory factory)
    : IClassFixture<RabbitOutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private const int GenderMale = 0;
    private const int GenderFemale = 1;
    private const int ParticipationGuest = 0;

    private static object NewRetreatBodyOpenNow(string name = "Retiro RABBIT", int maleSlots = 2, int femaleSlots = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new {
            name = new { value = name },
            edition = "ED-RAB",
            theme = "Tema",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate   = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots,
            femaleSlots,
            registrationStart = today.AddDays(-1).ToString("yyyy-MM-dd"),
            registrationEnd   = today.AddDays(7).ToString("yyyy-MM-dd"),
            feeFazer  = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct  = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender) => new {
        name  = new { value = name },
        cpf   = new { value = cpf },
        email = new { value = email },
        phone = "11999999999",
        birthDate = "2000-01-01",
        gender,
        city = "SP",
        participationCategory = ParticipationGuest,
        region = "Oeste",
        retreatId
    };

    [Fact]
    public async Task NotifySelected_publica_no_Rabbit_um_evento_por_selecionado()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        createRet.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        var regs = new[] {
            NewRegistrationBody(retreatId, "M1 Teste", "52998224725", "m1@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M2 Teste", "15350946056", "m2@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M3 Teste", "11144477735", "m3@t.com", GenderMale),
            NewRegistrationBody(retreatId, "F1 Teste", "93541134780", "f1@t.com", GenderFemale),
            NewRegistrationBody(retreatId, "F2 Teste", "28625587887", "f2@t.com", GenderFemale)
        };
        foreach (var payload in regs)
        {
            var r = await _client.PostAsJsonAsync("/api/Registrations", payload);
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }

        var commit = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/commit", null);
        commit.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await commit.Content.ReadFromJsonAsync<PreviewDto>();
        var expected = preview!.Male.Count + preview.Female.Count; // 3

        using var conn = await CreateRabbitConnectionAsync(factory);
        using var ch   = await conn.CreateChannelAsync();

        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);

        // 🔧 Bind com coringa para capturar QUALQUER routing key publicada no exchange
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "#");

        var notify = await _client.PostAsync($"/admin/notifications/retreats/{retreatId}/notify-selected", null);
        notify.StatusCode.Should().Be(HttpStatusCode.OK);

        var got = 0;
        var deadline = DateTime.UtcNow.AddSeconds(45); // mais folga para o Dispatcher (poll ~5s)
        while (got < expected && DateTime.UtcNow < deadline)
        {
            var result = await ch.BasicGetAsync(q.QueueName, autoAck: true);
            if (result is not null)
            {
                var json = Encoding.UTF8.GetString(result.Body.Span);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("data", out var dataEl) || doc.RootElement.TryGetProperty("Data", out dataEl))
                {
                    JsonElement payloadEl;
                    if (dataEl.ValueKind == JsonValueKind.String)
                    {
                        using var pd = JsonDocument.Parse(dataEl.GetString()!);
                        payloadEl = pd.RootElement.Clone();
                    }
                    else
                    {
                        payloadEl = dataEl.Clone();
                    }

                    if (payloadEl.ValueKind == JsonValueKind.Object &&
                        (payloadEl.TryGetProperty("RetreatId", out var ridEl) || payloadEl.TryGetProperty("retreatId", out ridEl)))
                    {
                        var ridStr = ridEl.ValueKind == JsonValueKind.String ? ridEl.GetString() : ridEl.ToString();
                        if (Guid.TryParse(ridStr, out var rid) && rid == retreatId)
                            got++;
                    }
                }
            }
            else
            {
                await Task.Delay(300);
            }
        }

        got.Should().Be(expected);
    }

    private static async Task<IConnection> CreateRabbitConnectionAsync(RabbitOutboxWebAppFactory factory)
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port     = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        return await cf.CreateConnectionAsync("samtests-consumer");
    }

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class PreviewDto { public List<Guid> Male { get; set; } = new(); public List<Guid> Female { get; set; } = new(); }
}
