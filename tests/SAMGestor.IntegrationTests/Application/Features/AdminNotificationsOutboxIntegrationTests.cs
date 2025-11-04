using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.IntegrationTests.Shared;
using SAMGestor.Infrastructure.Messaging.Outbox;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.Application.Interfaces;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class AdminNotificationsOutboxIntegrationTests(OutboxWebAppFactory factory)
    : IClassFixture<OutboxWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private const int GenderMale = 0;
    private const int GenderFemale = 1;
    private const int ParticipationGuest = 0;
    private const string EventSource = "sam.core";

    private static object NewRetreatBodyOpenNow(string name = "Retiro OUTBOX", int maleSlots = 2, int femaleSlots = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new {
            name = new { value = name },
            edition = "ED-OUT",
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

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class PreviewDto { public List<Guid> Male { get; set; } = new(); public List<Guid> Female { get; set; } = new(); }

    private static bool TryGetProperty(JsonElement obj, out JsonElement value, params string[] names)
    {
        foreach (var n in names)
        {
            if (obj.TryGetProperty(n, out value)) return true;
        }
        value = default;
        return false;
    }

    private static bool TryGetGuid(JsonElement obj, out Guid guid, params string[] names)
    {
        guid = Guid.Empty;
        if (!TryGetProperty(obj, out var el, names)) return false;
        string? s = el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            _ => el.ToString()
        };
        return Guid.TryParse(s, out guid);
    }

    [Fact]
    public async Task NotifySelected_grava_no_outbox_um_por_selecionado_com_payload_correto()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        var regs = new[] {
            NewRegistrationBody(retreatId, "M1 Teste", "52998224725", "m1@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M2 Teste", "15350946056", "m2@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M3 Teste", "11144477735", "m3@t.com", GenderMale),
            NewRegistrationBody(retreatId, "F1 Teste", "93541134780", "f1@t.com", GenderFemale),
            NewRegistrationBody(retreatId, "F2 Teste", "28625587887", "f2@t.com", GenderFemale)
        };
        foreach (var regPayload in regs)
        {
            var r = await _client.PostAsJsonAsync("/api/Registrations", regPayload);
            r.StatusCode.Should().Be(HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
        }

        var commit = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/commit", null);
        commit.StatusCode.Should().Be(HttpStatusCode.OK, await commit.Content.ReadAsStringAsync());
        var preview = await commit.Content.ReadFromJsonAsync<PreviewDto>();
        var expected = preview!.Male.Count + preview.Female.Count;

        var notify = await _client.PostAsync($"/admin/notifications/retreats/{retreatId}/notify-selected", null);
        notify.StatusCode.Should().Be(HttpStatusCode.OK, await notify.Content.ReadAsStringAsync());

        using var scope = factory.Services.CreateScope();
        var resolvedBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        resolvedBus.Should().BeOfType<OutboxEventBus>();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();

        var allMsgs = await db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Source == EventSource)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var ours = new List<string>();
        foreach (var msg in allMsgs)
        {
            try
            {
                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;
                if (!TryGetProperty(root, out var dataEl, "data", "Data")) continue;

                JsonElement payloadEl;
                if (dataEl.ValueKind == JsonValueKind.String)
                {
                    var s = dataEl.GetString();
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    using var payloadDoc = JsonDocument.Parse(s);
                    payloadEl = payloadDoc.RootElement.Clone();
                }
                else
                {
                    payloadEl = dataEl.Clone();
                }

                if (payloadEl.ValueKind != JsonValueKind.Object) continue;
                if (!TryGetGuid(payloadEl, out var rid, "RetreatId", "retreatId")) continue;
                if (rid == retreatId) ours.Add(msg.Data);
            }
            catch { }
        }

        if (ours.Count == 0)
        {
            var diag = new StringBuilder();
            diag.AppendLine("Nenhuma mensagem do Outbox para este RetreatId foi encontrada. Dump das últimas 10 mensagens do source 'sam.core':");
            foreach (var m in allMsgs.TakeLast(10))
            {
                diag.AppendLine($"- {m.CreatedAt:u} | Type={m.Type} | TraceId={m.TraceId}");
            }
            Assert.Fail(diag.ToString());
        }

        ours.Should().HaveCount(expected);

        foreach (var raw in ours)
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            TryGetProperty(root, out var dataEl, "data", "Data").Should().BeTrue();

            JsonElement payloadEl;
            if (dataEl.ValueKind == JsonValueKind.String)
            {
                using var payloadDoc = JsonDocument.Parse(dataEl.GetString()!);
                payloadEl = payloadDoc.RootElement;
            }
            else
            {
                payloadEl = dataEl;
            }

            TryGetGuid(payloadEl, out var rid, "RetreatId", "retreatId").Should().BeTrue();
            rid.Should().Be(retreatId);

            TryGetGuid(payloadEl, out var regId, "RegistrationId", "registrationId").Should().BeTrue();
            regId.Should().NotBeEmpty();

            var hasName = TryGetProperty(payloadEl, out var nameEl, "Name", "name");
            hasName.Should().BeTrue();
            var nameStr = nameEl.ValueKind == JsonValueKind.String ? nameEl.GetString() : nameEl.ToString();
            nameStr.Should().NotBeNullOrWhiteSpace();
        }
    }
}
