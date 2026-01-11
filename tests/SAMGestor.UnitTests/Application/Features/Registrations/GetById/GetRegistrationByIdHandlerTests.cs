using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Registrations.GetById;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;



namespace SAMGestor.UnitTests.Application.Features.Registrations.GetById;

public class GetRegistrationByIdHandlerTests
{
    private readonly Mock<IRegistrationRepository> _regRepo = new();
    private readonly Mock<IFamilyMemberRepository> _familyMemberRepo = new();
    private readonly Mock<IFamilyRepository> _familyRepo = new();
    private readonly GetRegistrationByIdHandler _handler;
    private readonly Mock<IStorageService> _storage = new();

    public GetRegistrationByIdHandlerTests()
    {
        
        _storage
            .Setup(s => s.GetPublicUrl(It.IsAny<string>()))
            .Returns((string key) => $"http://storage.test/{key}");

        _handler = new GetRegistrationByIdHandler(
            _regRepo.Object,
            _familyMemberRepo.Object,
            _familyRepo.Object,
            _storage.Object,
            Mock.Of<IManualPaymentProofRepository>()
        );
    }

    [Fact]
    public async Task Handle_returns_null_when_registration_not_found()
    {
        var id = Guid.NewGuid();
        _regRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Registration?)null);

        var resp = await _handler.Handle(new GetRegistrationByIdQuery(id), CancellationToken.None);

        resp.Should().BeNull();
        _familyMemberRepo.Verify(m => m.GetByRegistrationIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _familyRepo.Verify(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_mapped_dto_without_family()
    {
        var retreatId = Guid.NewGuid();
        var reg = MakeReg(
            retreatId,
            "Joao Silva",
            "52998224725",
            "joao@t.com",
            Gender.Male,
            RegistrationStatus.Confirmed,
            yearsAgo: 25,
            city: "SP"
        );

        SetPhoto(reg);
        SetIdDocument(reg);

        _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(reg);

        _familyMemberRepo.Setup(m => m.GetByRegistrationIdAsync(retreatId, reg.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((FamilyMember?)null);

        var resp = await _handler.Handle(new GetRegistrationByIdQuery(reg.Id), CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.Id.Should().Be(reg.Id);
        resp.Name.Should().Be("Joao Silva");
        resp.Cpf.Should().Be("52998224725");
        resp.Email.Should().Be("joao@t.com");
        resp.Phone.Should().Be("11999999999");
        resp.City.Should().Be("SP");
        resp.Gender.Should().Be(nameof(Gender.Male));
        resp.Status.Should().Be(nameof(RegistrationStatus.Confirmed));
        resp.Enabled.Should().BeTrue();
        resp.RetreatId.Should().Be(retreatId);
        resp.TentId.Should().BeNull();
        resp.TeamId.Should().BeNull();
        DateOnly.Parse(resp.BirthDate).Should().Be(reg.BirthDate);
        resp.PhotoUrl.Should().Be("https://cdn.example.com/p.jpg");
        resp.CompletedRetreat.Should().BeFalse();
        resp.RegistrationDate.Should().BeCloseTo(reg.RegistrationDate, TimeSpan.FromSeconds(1));
        resp.Family.Should().BeNull();
        resp.Age.Should().Be(reg.GetAgeOn(DateOnly.FromDateTime(DateTime.UtcNow)));

        resp.Personal.Pregnancy.Should().Be(nameof(PregnancyStatus.None));
        resp.ReligionHistory.PreviousUncalledApplications.Should().Be(nameof(RahaminAttempt.None));
        resp.Health.AlcoholUse.Should().Be(nameof(AlcoholUsePattern.None));
        resp.Consents.TermsAccepted.Should().BeFalse();

        resp.Media.PhotoStorageKey.Should().Be("retreats/key/reg/photo.jpg");
        resp.Media.PhotoContentType.Should().Be("image/jpeg");
        resp.Media.PhotoSizeBytes.Should().Be(1234);
        resp.Media.PhotoUploadedAt.Should().NotBeNull();
        resp.Media.PhotoUrl.Should().Be("https://cdn.example.com/p.jpg");

        resp.Media.IdDocumentType.Should().Be(nameof(IdDocumentType.RG));
        resp.Media.IdDocumentNumber.Should().Be("123456");
        resp.Media.IdDocumentStorageKey.Should().Be("retreats/key/reg/id.pdf");
        resp.Media.IdDocumentContentType.Should().Be("application/pdf");
        resp.Media.IdDocumentSizeBytes.Should().Be(2048);
        resp.Media.IdDocumentUploadedAt.Should().NotBeNull();
        resp.Media.IdDocumentUrl.Should().Be("https://cdn.example.com/id.pdf");
    }

    [Fact]
    public async Task Handle_returns_mapped_dto_with_family_membership()
    {
        var retreatId = Guid.NewGuid();
        var reg = MakeReg(
            retreatId,
            "Maria Souza",
            "15350946056",
            "maria@t.com",
            Gender.Female,
            RegistrationStatus.Selected,
            yearsAgo: 30,
            city: "RJ"
        );

        var fam = new Family(
            new FamilyName("Souza Family"),
            retreatId,
            capacity: 5
        );

        var link = new FamilyMember(
            retreatId,
            fam.Id,
            reg.Id,
            position: 2
        );

        _regRepo.Setup(r => r.GetByIdAsync(reg.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);

        _familyMemberRepo.Setup(m => m.GetByRegistrationIdAsync(retreatId, reg.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        _familyRepo.Setup(fr => fr.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fam);

        var resp = await _handler.Handle(new GetRegistrationByIdQuery(reg.Id), CancellationToken.None);

        resp.Should().NotBeNull();
        resp!.Family.Should().NotBeNull();
        resp.Family!.FamilyId.Should().Be(fam.Id);
        resp.Family.FamilyName.Should().Be("Souza Family");
        resp.Family.Position.Should().Be(2);
    }

    private static Registration MakeReg(
        Guid retreatId,
        string name,
        string cpf,
        string email,
        Gender gender,
        RegistrationStatus status,
        int yearsAgo,
        string city)
    {
        var birth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-yearsAgo).Date);
        return new Registration(
            new FullName(name),
            new CPF(cpf),
            new EmailAddress(email),
            "11999999999",
            birth,
            gender,
            city,
            status,
            retreatId
        );
    }

    private static void SetPhoto(Registration reg)
    {
        var url = new UrlAddress("https://cdn.example.com/p.jpg");
        reg.SetPhoto("retreats/key/reg/photo.jpg", "image/jpeg", 1234, DateTime.UtcNow, url);
    }

    private static void SetIdDocument(Registration reg)
    {
        var url = new UrlAddress("https://cdn.example.com/id.pdf");
        reg.SetIdDocument(
            IdDocumentType.RG,
            "123456",
            "retreats/key/reg/id.pdf",
            "application/pdf",
            2048,
            DateTime.UtcNow,
            url
        );
    }
}
