using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using RabbitMQ.Client;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class AdminNotificationsRabbitE2EIntegrationTests(RabbitOutboxWebAppFactory factory)
    : IClassFixture<RabbitOutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private const int GenderMale = 0;
    private const int GenderFemale = 1;

    private static object NewRetreatBodyOpenNow(string name = "Retiro RABBIT", int maleSlots = 2, int femaleSlots = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var uniq = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            name = new { value = $"{name} {uniq}" },
            edition = $"ED-RAB-{uniq}",
            theme = "Tema",
            startDate = today.AddDays(30).ToString("yyyy-MM-dd"),
            endDate = today.AddDays(32).ToString("yyyy-MM-dd"),
            maleSlots,
            femaleSlots,
            registrationStart = today.AddDays(-1).ToString("yyyy-MM-dd"),
            registrationEnd = today.AddDays(7).ToString("yyyy-MM-dd"),
            feeFazer = new { amount = 0, currency = "BRL" },
            feeServir = new { amount = 0, currency = "BRL" },
            westRegionPct = new { value = 50 },
            otherRegionPct = new { value = 50 }
        };
    }

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender) => new
    {
        name = new { value = name },
        cpf = new { value = cpf },
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
        instagramHandle = "fulano.ig",

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
        drugUseFrequency = (string?)null,
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

    private static string NewEmail(string prefix = "t")
        => $"{prefix}-{Guid.NewGuid():N}@t.com";

    // CPF válido (gera dígitos verificadores)
    private static string NewValidCpf()
    {
        var r = new Random();
        int[] n = new int[9];

        for (int i = 0; i < 9; i++) n[i] = r.Next(0, 10);

        int d1 = 0;
        for (int i = 0; i < 9; i++) d1 += n[i] * (10 - i);
        d1 = 11 - (d1 % 11);
        if (d1 >= 10) d1 = 0;

        int d2 = 0;
        for (int i = 0; i < 9; i++) d2 += n[i] * (11 - i);
        d2 += d1 * 2;
        d2 = 11 - (d2 % 11);
        if (d2 >= 10) d2 = 0;

        return string.Concat(n) + d1 + d2;
    }

    [Fact]
    public async Task NotifySelected_publica_no_Rabbit_um_evento_por_selecionado()
    {
        // 1) cria retiro
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        // 2) cria inscrições (únicas)
        var regs = new[]
        {
            NewRegistrationBody(retreatId, "M1 Teste", NewValidCpf(), NewEmail("m1"), GenderMale),
            NewRegistrationBody(retreatId, "M2 Teste", NewValidCpf(), NewEmail("m2"), GenderMale),
            NewRegistrationBody(retreatId, "M3 Teste", NewValidCpf(), NewEmail("m3"), GenderMale),
            NewRegistrationBody(retreatId, "F1 Teste", NewValidCpf(), NewEmail("f1"), GenderFemale),
            NewRegistrationBody(retreatId, "F2 Teste", NewValidCpf(), NewEmail("f2"), GenderFemale),
        };

        foreach (var payload in regs)
        {
            var r = await _client.PostAsJsonAsync("/api/Registrations", payload);
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }

        // 3) commit do sorteio
        var commit = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/commit", null);
        commit.StatusCode.Should().Be(HttpStatusCode.OK, await commit.Content.ReadAsStringAsync());
        var preview = await commit.Content.ReadFromJsonAsync<PreviewDto>();
        var expected = preview!.Male.Count + preview.Female.Count; // normalmente 3
        expected.Should().BeGreaterThan(0);

        // 4) cria consumer (antes do notify)
        using var conn = await CreateRabbitConnectionAsync(factory);
        using var ch = await conn.CreateChannelAsync();

        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);

        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "#");

        // 5) chama notify
        var notify = await _client.PostAsync($"/admin/notifications/retreats/{retreatId}/notify-selected", null);
        notify.StatusCode.Should().Be(HttpStatusCode.OK, await notify.Content.ReadAsStringAsync());

        // valida o count do endpoint (se for 0, não adianta esperar Rabbit)
        var notifyBody = await notify.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(notifyBody))
        {
            if (doc.RootElement.TryGetProperty("count", out var c))
            {
                c.GetInt32().Should().Be(expected, $"notify-selected deveria enfileirar {expected} itens (body: {notifyBody})");
            }
        }

        // 6) lê Rabbit
        var got = 0;
        var deadline = DateTime.UtcNow.AddSeconds(45);

        while (got < expected && DateTime.UtcNow < deadline)
        {
            var result = await ch.BasicGetAsync(q.QueueName, autoAck: true);
            if (result is null)
            {
                await Task.Delay(300);
                continue;
            }

            var json = Encoding.UTF8.GetString(result.Body.Span);

            // ✅ robusto: procura retreatId em qualquer lugar (inclusive aninhado / string json)
            if (TryFindGuidInAnyShape(json, retreatId))
                got++;
        }

        if (got != expected)
        {
            // 🔎 diagnósticos do outbox quando não chegou Rabbit
            var summary = await _client.GetAsync("/admin/outbox/summary");
            var summaryText = await summary.Content.ReadAsStringAsync();

            var pendingList = await _client.GetAsync("/admin/outbox?processed=false&limit=10");
            var pendingText = await pendingList.Content.ReadAsStringAsync();

            throw new Xunit.Sdk.XunitException(
                $"Expected got={expected}, but got={got}. " +
                $"OutboxSummary: {summaryText}. " +
                $"OutboxPending: {pendingText}."
            );
        }

        got.Should().Be(expected);
    }

    private static bool TryFindGuidInAnyShape(string body, Guid target)
    {
        // atalho bem efetivo (e seguro pro teste): se o guid aparecer no texto, conta
        if (body.Contains(target.ToString(), StringComparison.OrdinalIgnoreCase))
            return true;

        // se não apareceu, tenta parsear e procurar por chaves comuns
        try
        {
            using var doc = JsonDocument.Parse(body);
            return ContainsGuidRecursive(doc.RootElement, target);
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsGuidRecursive(JsonElement el, Guid target)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in el.EnumerateObject())
                {
                    // se for um campo "retreatId" / "RetreatId", tenta parse direto
                    if (p.NameEquals("retreatId") || p.NameEquals("RetreatId"))
                    {
                        var s = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
                        if (Guid.TryParse(s, out var g) && g == target) return true;
                    }

                    // se algum valor for uma string com JSON dentro, tenta parsear de novo
                    if (p.Value.ValueKind == JsonValueKind.String)
                    {
                        var s = p.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(s) &&
                            s.Length > 2 &&
                            (s.TrimStart().StartsWith("{") || s.TrimStart().StartsWith("[")))
                        {
                            try
                            {
                                using var inner = JsonDocument.Parse(s);
                                if (ContainsGuidRecursive(inner.RootElement, target)) return true;
                            }
                            catch { /* ignore */ }
                        }
                    }

                    if (ContainsGuidRecursive(p.Value, target)) return true;
                }
                return false;

            case JsonValueKind.Array:
                foreach (var item in el.EnumerateArray())
                    if (ContainsGuidRecursive(item, target)) return true;
                return false;

            case JsonValueKind.String:
                var str = el.GetString();
                return !string.IsNullOrWhiteSpace(str) &&
                       str.Contains(target.ToString(), StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    private static async Task<IConnection> CreateRabbitConnectionAsync(RabbitOutboxWebAppFactory factory)
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        return await cf.CreateConnectionAsync("samtests-consumer");
    }

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class PreviewDto { public List<Guid> Male { get; set; } = new(); public List<Guid> Female { get; set; } = new(); }
}
