using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class LotteryIntegrationTests(CustomWebAppFactory factory) : IClassFixture<CustomWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Enums como inteiros (sem JsonStringEnumConverter)
    private const int GenderMale = 0;    // Gender.Male
    private const int GenderFemale = 1;  // Gender.Female
    private const int ParticipationGuest = 0; // ParticipationCategory.Guest

    // CPFs válidos (checksum ok) — pode trocar se seu VO exigir outros
    private const string CpfM1 = "52998224725";
    private const string CpfM2 = "15350946056";
    private const string CpfM3 = "11144477735";
    private const string CpfF1 = "93541134780";
    private const string CpfF2 = "28625587887";
    private const string CpfM11 = "76253361006"; // manual select
    private const string CpfM12 = "04133866006"; // manual select

    private static object NewRetreatBodyOpenNow(string name = "Retiro LOTT", int maleSlots = 2, int femaleSlots = 1)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new
        {
            name = new { value = name }, // FullName -> 2 palavras: "Retiro LOTT"
            edition = "ED-LOT",
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

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, int gender)
        => new
        {
            name  = new { value = name }, // 2 palavras (FullName)
            cpf   = new { value = cpf },  // CPF válido
            email = new { value = email },
            phone = "11999999999",
            birthDate = "2000-01-01",
            gender,                       // int (0 ou 1)
            city = "SP",
            participationCategory = ParticipationGuest, // int (0)
            region = "Oeste",
            retreatId
        };

    [Fact]
    public async Task Preview_e_Commit_funcionam_e_respeitam_slots()
    {
        // 1) cria retiro
        var createRetResp = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        var bodyCreateRet = await createRetResp.Content.ReadAsStringAsync();
        createRetResp.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {bodyCreateRet}");

        var created = await createRetResp.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        // 2) cria inscrições (3 homens, 2 mulheres)
        var regs = new[]
        {
            NewRegistrationBody(retreatId, "M1 Teste", CpfM1, "m1@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M2 Teste", CpfM2, "m2@t.com", GenderMale),
            NewRegistrationBody(retreatId, "M3 Teste", CpfM3, "m3@t.com", GenderMale),
            NewRegistrationBody(retreatId, "F1 Teste", CpfF1, "f1@t.com", GenderFemale),
            NewRegistrationBody(retreatId, "F2 Teste", CpfF2, "f2@t.com", GenderFemale)
        };

        foreach (var body in regs)
        {
            var r = await _client.PostAsJsonAsync("/api/Registrations", body);
            var err = await r.Content.ReadAsStringAsync();
            r.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {err}");
        }

        // 3) preview
        var prevResp = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/preview", null);
        var prevErr = await prevResp.Content.ReadAsStringAsync();
        prevResp.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {prevErr}");

        var preview = await prevResp.Content.ReadFromJsonAsync<PreviewDto>();
        preview!.MaleCap.Should().Be(2);
        preview.FemaleCap.Should().Be(1);
        preview.Male.Count.Should().Be(2);
        preview.Female.Count.Should().Be(1);

        // 4) commit
        var commitResp = await _client.PostAsync($"/api/retreats/{retreatId}/lottery/commit", null);
        var commitErr = await commitResp.Content.ReadAsStringAsync();
        commitResp.StatusCode.Should().Be(HttpStatusCode.OK, $"body: {commitErr}");

        var committed = await commitResp.Content.ReadFromJsonAsync<PreviewDto>();
        committed!.Male.Count.Should().Be(2);
        committed.Female.Count.Should().Be(1);
    }

    [Fact]
    public async Task ManualSelect_e_Unselect_endpoints()
    {
        // cria retiro com 1 vaga masculina e 0 feminina
        var body = NewRetreatBodyOpenNow("Retiro Manual", maleSlots: 1, femaleSlots: 0);
        var resp = await _client.PostAsJsonAsync("/api/Retreats", body);
        var bodyCreate = await resp.Content.ReadAsStringAsync();
        resp.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {bodyCreate}");

        var created = await resp.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = created!.RetreatId;

        // cria 2 inscrições masculinas
        var reg1 = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "M1 Teste", CpfM11, "m11@t.com", GenderMale));
        var reg2 = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "M2 Teste", CpfM12, "m12@t.com", GenderMale));

        var reg1Err = await reg1.Content.ReadAsStringAsync();
        var reg2Err = await reg2.Content.ReadAsStringAsync();
        reg1.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {reg1Err}");
        reg2.StatusCode.Should().Be(HttpStatusCode.Created, $"body: {reg2Err}");

        var reg1Created = await reg1.Content.ReadFromJsonAsync<CreatedRegistrationDto>();
        var reg2Created = await reg2.Content.ReadFromJsonAsync<CreatedRegistrationDto>();
        var regId1 = reg1Created!.RegistrationId;
        var regId2 = reg2Created!.RegistrationId;

        // contempla 1
        var sel1 = await _client.PostAsync($"/api/retreats/{retreatId}/selections/{regId1}", null);
        var sel1Err = await sel1.Content.ReadAsStringAsync();
        sel1.StatusCode.Should().Be(HttpStatusCode.NoContent, $"body: {sel1Err}");

        // tentar contemplar o segundo deve dar 422 (regra de negócio)
        var sel2 = await _client.PostAsync($"/api/retreats/{retreatId}/selections/{regId2}", null);
        var sel2Err = await sel2.Content.ReadAsStringAsync();
        sel2.StatusCode.Should().Be(HttpStatusCode.BadRequest, $"body: {sel2Err}");


        // desfaz a seleção do primeiro
        var unsel = await _client.DeleteAsync($"/api/retreats/{retreatId}/selections/{regId1}");
        var unselErr = await unsel.Content.ReadAsStringAsync();
        unsel.StatusCode.Should().Be(HttpStatusCode.NoContent, $"body: {unselErr}");
    }

    private sealed class PreviewDto
    {
        public List<Guid> Male { get; set; } = new();
        public List<Guid> Female { get; set; } = new();
        public int MaleCap { get; set; }
        public int FemaleCap { get; set; }
    }

    private sealed class CreatedRetreatDto
    {
        public Guid RetreatId { get; set; }
    }

    private sealed class CreatedRegistrationDto
    {
        public Guid RegistrationId { get; set; }
    }
}
