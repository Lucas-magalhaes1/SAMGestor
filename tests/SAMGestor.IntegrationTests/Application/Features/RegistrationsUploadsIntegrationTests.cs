using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using SAMGestor.Application.Features.Registrations.GetById;
using SAMGestor.Domain.Enums;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class RegistrationsUploadsIntegrationTests(UploadsWebAppFactory factory)
    : IClassFixture<UploadsWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private sealed class CreatedRetreatDto { public Guid RetreatId { get; set; } }
    private sealed class CreatedRegistrationDto { public Guid RegistrationId { get; set; } }

    private sealed record UploadResp(string key, string url, int size);

    [Fact]
    public async Task UploadPhoto_and_document_flow_works_and_reflects_in_GetById()
    {
        // 1) cria retiro
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        createRet.StatusCode.Should().Be(HttpStatusCode.Created, await createRet.Content.ReadAsStringAsync());
        var createdRet = await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>();
        var retreatId = createdRet!.RetreatId;

        // 2) cria inscrição
        var createReg = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "Maria Teste", "93541134780", "maria@t.com", Gender.Female));
        createReg.StatusCode.Should().Be(HttpStatusCode.Created, await createReg.Content.ReadAsStringAsync());
        var createdReg = await createReg.Content.ReadFromJsonAsync<CreatedRegistrationDto>();
        var regId = createdReg!.RegistrationId;

        // 3) upload da foto (jpg)
        var jpgBytes = RandomBytes(1024);
        using var photoContent = new MultipartFormDataContent
        {
            { new ByteArrayContent(jpgBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg") } }, "file", "foto.jpg" }
        };
        var uploadPhoto = await _client.PostAsync($"/api/Registrations/{regId}/photo", photoContent);
        uploadPhoto.StatusCode.Should().Be(HttpStatusCode.Created, await uploadPhoto.Content.ReadAsStringAsync());
        var photoResp = await uploadPhoto.Content.ReadFromJsonAsync<UploadResp>();
        photoResp.Should().NotBeNull();
        photoResp!.key.Should().NotBeNullOrWhiteSpace();
        photoResp.url.Should().NotBeNullOrWhiteSpace();
        photoResp.size.Should().Be(jpgBytes.Length);

        // 4) upload do documento (pdf)
        var pdfBytes = RandomBytes(2048);
        using var docContent = new MultipartFormDataContent
        {
            { new ByteArrayContent(pdfBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") } }, "file", "doc.pdf" },
            { new StringContent("RG", Encoding.UTF8), "type" },
            { new StringContent("123456", Encoding.UTF8), "number" }
        };
        var uploadDoc = await _client.PostAsync($"/api/Registrations/{regId}/document", docContent);
        uploadDoc.StatusCode.Should().Be(HttpStatusCode.Created, await uploadDoc.Content.ReadAsStringAsync());
        var docResp = await uploadDoc.Content.ReadFromJsonAsync<UploadResp>();
        docResp.Should().NotBeNull();
        docResp!.key.Should().NotBeNullOrWhiteSpace();
        docResp.url.Should().NotBeNullOrWhiteSpace();
        docResp.size.Should().Be(pdfBytes.Length);

        // 5) GET by id deve refletir foto + doc
        var get = await _client.GetAsync($"/api/Registrations/{regId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());
        var dto = await get.Content.ReadFromJsonAsync<GetRegistrationByIdResponse>();

        dto!.Id.Should().Be(regId);

        // valida URLs usando o que o próprio endpoint de upload retornou
        (dto.PhotoUrl ?? dto.Media.PhotoUrl).Should().Be(photoResp.url);
        dto.Media.PhotoContentType.Should().Be("image/jpeg");
        dto.Media.PhotoSizeBytes.Should().Be(jpgBytes.Length);

        dto.Media.IdDocumentType.Should().Be(nameof(IdDocumentType.RG));
        dto.Media.IdDocumentContentType.Should().Be("application/pdf");
        dto.Media.IdDocumentSizeBytes.Should().Be(pdfBytes.Length);
        dto.Media.IdDocumentUrl.Should().Be(docResp.url);
    }

    [Fact]
    public async Task UploadPhoto_rejects_invalid_type()
    {
        var createRet = await _client.PostAsJsonAsync("/api/Retreats", NewRetreatBodyOpenNow());
        var retreatId = (await createRet.Content.ReadFromJsonAsync<CreatedRetreatDto>())!.RetreatId;

        var createReg = await _client.PostAsJsonAsync("/api/Registrations",
            NewRegistrationBody(retreatId, "Fulano Teste", "52998224725", "f@t.com", Gender.Male));
        var regId = (await createReg.Content.ReadFromJsonAsync<CreatedRegistrationDto>())!.RegistrationId;

        var badBytes = RandomBytes(64);
        using var badContent = new MultipartFormDataContent
        {
            { new ByteArrayContent(badBytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain") } }, "file", "nota.txt" }
        };
        var resp = await _client.PostAsync($"/api/Registrations/{regId}/photo", badContent);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ----- helpers -----

    private static object NewRetreatBodyOpenNow(string namePrefix = "Retiro UP", int maleSlots = 2, int femaleSlots = 2)
    {
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var uniq   = Guid.NewGuid().ToString("N")[..8]; // sufixo único
        var name   = $"{namePrefix} {uniq}";
        var edition= $"ED-UP-{uniq}";

        return new
        {
            name = new { value = name },
            edition,
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

    private static object NewRegistrationBody(Guid retreatId, string name, string cpf, string email, Gender gender) => new
    {
        retreatId,
        name  = new { value = name },
        cpf   = new { value = cpf },
        email = new { value = email },
        phone = "11999999999",
        birthDate = "2000-01-01",
        gender = gender.ToString(),
        city = "SP",
        state = "SP",
        maritalStatus = "Married",
        pregnancy = "None",
        shirtSize = "M",
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
        fatherStatus = "Alive",
        fatherName = "Pai Teste",
        fatherPhone = "1133332222",
        motherStatus = "Alive",
        motherName = "Mae Teste",
        motherPhone = "11911113333",
        hadFamilyLossLast6Months = false,
        familyLossDetails = (string?)null,
        hasRelativeOrFriendSubmitted = false,
        submitterRelationship = "None",
        submitterNames = (string?)null,
        religion = "Católica",
        previousUncalledApplications = "None",
        rahaminVidaCompleted = "None",
        alcoholUse = "None",
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

    private static byte[] RandomBytes(int len)
    {
        var b = new byte[len];

    new Random().NextBytes(b);
        return b;
    }
}
