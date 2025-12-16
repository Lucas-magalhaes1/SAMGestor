using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.IntegrationTests.Shared;
using SAMGestor.IntegrationTests.TestDoubles;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class AdminNotificationsIntegrationTests(NotificationsWebAppFactory factory)
    : IClassFixture<NotificationsWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Enums como inteiros (iguais ao runtime)
    private const int GenderMale = 0;
    private const int GenderFemale = 1;

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class CreatedRegistrationDto { public Guid RegistrationId { get; set; } }

    private sealed class PreviewDto
    {
        public List<Guid> Male { get; set; } = new();
        public List<Guid> Female { get; set; } = new();
        public int MaleCap { get; set; }
        public int FemaleCap { get; set; }
    }

    // DTO para resposta do notify em massa
    private sealed class NotifyManyResult
    {
        public Guid retreatId { get; set; }
        public int count { get; set; }
    }

    private CapturingEventBus GetBus()
        => factory.Services.GetRequiredService<CapturingEventBus>();

    // ========= DATA FACTORY (evita colisão no DB compartilhado da classe) =========

    private static object NewRetreatBodyOpenNow(string name = "Retiro NOTIF", int maleSlots = 2, int femaleSlots = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var uniq = Guid.NewGuid().ToString("N")[..8];

        return new
        {
            name = new { value = $"{name} {uniq}" },
            edition = $"ED-NOTIF-{uniq}",
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

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender)
        => new
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

    // ============================== TESTS ==============================

    [Fact]
    public async Task NotifySelectedForRetreat_enfileira_um_evento_por_selecionado_e_retorna_count()
    {
        var bus = GetBus();
        bus.Clear();

        // 1) cria retiro (2M + 1F)
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        // 2) cria 3M e 2F (CPF + email únicos para não colidir entre tests)
        var regs = new[]
        {
            NewRegistrationBody(retreatId, "M1 Teste", NewValidCpf(), NewEmail("m1"), GenderMale),
            NewRegistrationBody(retreatId, "M2 Teste", NewValidCpf(), NewEmail("m2"), GenderMale),
            NewRegistrationBody(retreatId, "M3 Teste", NewValidCpf(), NewEmail("m3"), GenderMale),
            NewRegistrationBody(retreatId, "F1 Teste", NewValidCpf(), NewEmail("f1"), GenderFemale),
            NewRegistrationBody(retreatId, "F2 Teste", NewValidCpf(), NewEmail("f2"), GenderFemale)
        };

        foreach (var regPayload in regs)
        {
            var r = await _client.PostAsJsonAsync("/api/Registrations", regPayload);
            var respText = await r.Content.ReadAsStringAsync();
            r.StatusCode.Should().Be(HttpStatusCode.Created, $"resp: {respText}");
        }

        // 3) sorteio (commit) -> seleciona 2M e 1F
        var commit = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/commit", content: null);
        commit.StatusCode.Should().Be(HttpStatusCode.OK, await commit.Content.ReadAsStringAsync());
        var committed = await commit.Content.ReadFromJsonAsync<PreviewDto>();
        var expectedSelected = committed!.Male.Count + committed.Female.Count;
        expectedSelected.Should().Be(3);

        // 4) notificação em massa
        var notify = await _client.PostAsync($"/admin/notifications/retreats/{retreatId}/notify-selected", null);
        notify.StatusCode.Should().Be(HttpStatusCode.OK, await notify.Content.ReadAsStringAsync());

        var result = await notify.Content.ReadFromJsonAsync<NotifyManyResult>();
        result.Should().NotBeNull();
        result!.retreatId.Should().Be(retreatId);
        result.count.Should().Be(expectedSelected);

        // 5) assert: N enqueues
        bus.Items.Count.Should().Be(expectedSelected);
    }

    [Fact]
    public async Task NotifyOne_Selected_retorna_200_e_enfileira_1_evento()
    {
        var bus = GetBus();
        bus.Clear();

        // cria retiro 1 vaga M
        var createRet = await _client.PostAsJsonAsync("/api/Retreats",
            NewRetreatBodyOpenNow("Retiro NOTIF ONE", maleSlots: 1, femaleSlots: 0));

        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        // duas inscrições (únicas)
        var reg1 = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "M1 Teste", NewValidCpf(), NewEmail("m1"), GenderMale));

        var reg2 = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "M2 Teste", NewValidCpf(), NewEmail("m2"), GenderMale));

        reg1.StatusCode.Should().Be(HttpStatusCode.Created, await reg1.Content.ReadAsStringAsync());
        reg2.StatusCode.Should().Be(HttpStatusCode.Created, await reg2.Content.ReadAsStringAsync());

        var created1 = await reg1.Content.ReadFromJsonAsync<CreatedRegistrationDto>();

        // seleciona manualmente o 1º
        var sel1 = await _client.PostAsync($"/api/retreats/{retreatId}/selections/{created1!.RegistrationId}", null);
        sel1.StatusCode.Should().Be(HttpStatusCode.NoContent, await sel1.Content.ReadAsStringAsync());

        // notify do selecionado
        var notify = await _client.PostAsync($"/admin/notifications/registrations/{created1!.RegistrationId}/notify", null);
        notify.StatusCode.Should().Be(HttpStatusCode.OK, await notify.Content.ReadAsStringAsync());

        bus.Items.Count.Should().Be(1);
    }

    [Fact]
    public async Task NotifyOne_nao_Selected_retorna_400_e_nao_enfileira()
    {
        var bus = GetBus();
        bus.Clear();

        // 🔴 ANTES: maleSlots:0 femaleSlots:0  -> isso pode fazer RegistrationWindowOpen() retornar false
        // ✅ AGORA: garante que o CreateRegistration não falha, mas continua "não selecionado"
        var createRet = await _client.PostAsJsonAsync("/api/Retreats",
            NewRetreatBodyOpenNow("Retiro NOTIF BAD", maleSlots: 1, femaleSlots: 0));

        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var created = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        var reg = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "M1 Teste", NewValidCpf(), NewEmail("m3"), GenderMale));

        reg.StatusCode.Should().Be(HttpStatusCode.Created, await reg.Content.ReadAsStringAsync());
        var regCreated = await reg.Content.ReadFromJsonAsync<CreatedRegistrationDto>();

        var notify = await _client.PostAsync($"/admin/notifications/registrations/{regCreated!.RegistrationId}/notify", null);
        notify.StatusCode.Should().Be(HttpStatusCode.BadRequest, await notify.Content.ReadAsStringAsync());

        bus.Items.Count.Should().Be(0);
    }

    [Fact]
    public async Task NotifyOne_inexistente_retorna_404_e_nao_enfileira()
    {
        var bus = GetBus();
        bus.Clear();

        var notify = await _client.PostAsync($"/admin/notifications/registrations/{Guid.NewGuid()}/notify", null);
        notify.StatusCode.Should().Be(HttpStatusCode.NotFound, await notify.Content.ReadAsStringAsync());

        bus.Items.Count.Should().Be(0);
    }
}
