using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.API.Controllers.Registration;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Application.Features.Registrations.GetById;
using SAMGestor.Application.Features.Registrations.Update;
using SAMGestor.Application.Interfaces;            
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;                  
using SAMGestor.Domain.ValueObjects;
using System.Text;

namespace SAMGestor.UnitTests.API.Controllers;

public class RegistrationsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<IRegistrationRepository> _regRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private readonly RegistrationsController _controller;

    public RegistrationsControllerTests()
    {
        _controller = new RegistrationsController(
            _mediator.Object,
            _storage.Object,
            _regRepo.Object,
            _uow.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Create_Returns_CreatedAtRoute_With_RegistrationId()
    {
        var regId = Guid.NewGuid();
        _mediator
            .Setup(m => m.Send(It.IsAny<CreateRegistrationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateRegistrationResponse(regId));

        var cmd = MakeValidCreateCommand();
        var result = await _controller.Create(cmd);
        
        var created = result as CreatedAtRouteResult;
        created.Should().NotBeNull();
        created!.RouteName.Should().Be(nameof(RegistrationsController.GetById));
        created.RouteValues!["id"].Should().Be(regId);
        created.Value.Should().BeOfType<CreateRegistrationResponse>()
               .Which.RegistrationId.Should().Be(regId);
    }
    
    [Fact]
    public async Task List_Returns_Ok_With_Query_Response()
    {
        var retreatId = Guid.NewGuid();
        var emptyItems = new List<RegistrationDto>();
        
        var response = new PagedResult<RegistrationDto>(emptyItems, 0, 0, 20);

        _mediator
            .Setup(m => m.Send(
                It.Is<GetAllRegistrationsQuery>(q =>
                    q.retreatId == retreatId && q.skip == 0 && q.take == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        
        var result = await _controller.List(
            retreatId,
            status: null,
            gender: null,
            minAge: null,
            maxAge: null,
            city: null,
            state: null,
            search: null,
            hasPhoto: null,
            skip: 0,
            take: 20
        );
    
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task UploadPhoto_Returns_BadRequest_When_File_Is_Null()
    {
        var result = await _controller.UploadPhoto(Guid.NewGuid(), null);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("Arquivo de foto é obrigatório.");
    }

    [Fact]
    public async Task UploadPhoto_Returns_BadRequest_When_ContentType_Invalid()
    {
        var file = CreateMockFile("test.txt", "text/plain", 100);
        
        var result = await _controller.UploadPhoto(Guid.NewGuid(), file.Object);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("A foto deve ser JPG ou PNG.");
    }

    [Fact]
    public async Task UploadPhoto_Returns_BadRequest_When_File_Too_Large()
    {
        var file = CreateMockFile("test.jpg", "image/jpeg", 6 * 1024 * 1024); // 6MB
        
        var result = await _controller.UploadPhoto(Guid.NewGuid(), file.Object);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("A foto deve ter no máximo 5MB.");
    }

    [Fact]
    public async Task UploadPhoto_Returns_NotFound_When_Registration_Not_Exists()
    {
        var file = CreateMockFile("test.jpg", "image/jpeg", 1024);
        var regId = Guid.NewGuid();

        _regRepo
            .Setup(r => r.GetByIdForUpdateAsync(regId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Registration?)null);

        var result = await _controller.UploadPhoto(regId, file.Object);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UploadPhoto_Returns_Created_When_Success()
    {
        var file = CreateMockFile("test.jpg", "image/jpeg", 1024);
        var retreatId = Guid.NewGuid();
        var regId = Guid.NewGuid();
        var reg = CreateMockRegistration(regId, retreatId);

        _regRepo
            .Setup(r => r.GetByIdForUpdateAsync(regId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);

        _storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("saved-key", 1024));

        _storage
            .Setup(s => s.GetPublicUrl("saved-key"))
            .Returns("https://storage.com/photo.jpg");

        _uow
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UploadPhoto(regId, file.Object);

        var created = result as CreatedResult;
        created.Should().NotBeNull();
        created!.Location.Should().Be("https://storage.com/photo.jpg");
    }

    [Fact]
    public async Task UploadDocument_Returns_BadRequest_When_File_Is_Null()
    {
        var result = await _controller.UploadDocument(Guid.NewGuid(), null, IdDocumentType.RG, "123");

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("Arquivo de documento é obrigatório.");
    }

    [Fact]
    public async Task UploadDocument_Returns_BadRequest_When_ContentType_Invalid()
    {
        var file = CreateMockFile("test.txt", "text/plain", 100);
        
        var result = await _controller.UploadDocument(Guid.NewGuid(), file.Object, IdDocumentType.RG, "123");

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("Documento deve ser JPG, PNG ou PDF.");
    }

    [Fact]
    public async Task UploadDocument_Returns_BadRequest_When_File_Too_Large()
    {
        var file = CreateMockFile("test.pdf", "application/pdf", 11 * 1024 * 1024); // 11MB
        
        var result = await _controller.UploadDocument(Guid.NewGuid(), file.Object, IdDocumentType.RG, "123");

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("O documento deve ter no máximo 10MB.");
    }

    [Fact]
    public async Task UploadDocument_Returns_Created_When_Success()
    {
        var file = CreateMockFile("test.pdf", "application/pdf", 2048);
        var retreatId = Guid.NewGuid();
        var regId = Guid.NewGuid();
        var reg = CreateMockRegistration(regId, retreatId);

        _regRepo
            .Setup(r => r.GetByIdForUpdateAsync(regId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reg);

        _storage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("saved-doc-key", 2048));

        _storage
            .Setup(s => s.GetPublicUrl("saved-doc-key"))
            .Returns("https://storage.com/doc.pdf");

        _uow
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UploadDocument(regId, file.Object, IdDocumentType.RG, "12345678");

        var created = result as CreatedResult;
        created.Should().NotBeNull();
        created!.Location.Should().Be("https://storage.com/doc.pdf");
    }

    [Fact]
    public async Task Update_Returns_BadRequest_When_Request_Is_Null()
    {
        var result = await _controller.Update(Guid.NewGuid(), null!, null, null);

        var bad = result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.Value.Should().Be("Request body is required.");
    }

    [Fact]
    public async Task Update_Returns_Ok_When_Success()
    {
        var regId = Guid.NewGuid();
        var request = MakeValidUpdateRequest();

        _mediator
            .Setup(m => m.Send(It.IsAny<UpdateRegistrationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateRegistrationResponse(regId, null, null));

        var result = await _controller.Update(regId, request, null, null);

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeOfType<UpdateRegistrationResponse>()
           .Which.RegistrationId.Should().Be(regId);
    }

    [Fact]
    public void GetOptions_Returns_Ok_With_Enums_And_Constraints()
    {
        var result = _controller.GetOptions();

        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        
        var value = ok!.Value;
        value.Should().NotBeNull();
        
        var valueType = value!.GetType();
        valueType.GetProperty("enums").Should().NotBeNull();
        valueType.GetProperty("constraints").Should().NotBeNull();
        valueType.GetProperty("rules").Should().NotBeNull();
    }

    // Helper methods
    private static CreateRegistrationCommand MakeValidCreateCommand() =>
        new CreateRegistrationCommand(
            RetreatId: Guid.NewGuid(),
            Name: new FullName("Fulano Teste"),
            Cpf: new CPF("12345678901"),
            Email: new EmailAddress("x@x.com"),
            Phone: "11999999999",
            BirthDate: new DateOnly(2000, 1, 1),
            Gender: Gender.Male,
            City: "SP",
            MaritalStatus: MaritalStatus.Married,
            Pregnancy: PregnancyStatus.None,
            ShirtSize: ShirtSize.M,
            WeightKg: 80m,
            HeightCm: 180m,
            Profession: "Dev",
            StreetAndNumber: "Rua A, 123",
            Neighborhood: "Centro",
            State: UF.SP,
            Whatsapp: "11988887777",
            FacebookUsername: "fulano.fb",
            InstagramHandle: "fulano.ig",
            NeighborPhone: "1133334444",
            RelativePhone: "11911112222",
            FatherStatus: ParentStatus.Alive,
            FatherName: "Pai Teste",
            FatherPhone: "1133332222",
            MotherStatus: ParentStatus.Alive,
            MotherName: "Mae Teste",
            MotherPhone: "11911113333",
            HadFamilyLossLast6Months: false,
            FamilyLossDetails: null,
            HasRelativeOrFriendSubmitted: false,
            SubmitterRelationship: RelationshipDegree.None,
            SubmitterNames: null,
            Religion: "Católica",
            PreviousUncalledApplications: RahaminAttempt.None,
            RahaminVidaCompleted: RahaminVidaEdition.None,
            AlcoholUse: AlcoholUsePattern.None,
            Smoker: false,
            UsesDrugs: false,
            DrugUseFrequency: null,
            HasAllergies: false,
            AllergiesDetails: null,
            HasMedicalRestriction: false,
            MedicalRestrictionDetails: null,
            TakesMedication: false,
            MedicationsDetails: null,
            PhysicalLimitationDetails: null,
            RecentSurgeryOrProcedureDetails: null,
            TermsAccepted: true,
            TermsVersion: "2025-10-01",
            MarketingOptIn: true,
            ClientIp: "127.0.0.1",
            UserAgent: "UnitTest"
        );

    private static RegistrationsController.UpdateRegistrationRequest MakeValidUpdateRequest() =>
        new()
        {
            Name = "João Silva",
            Cpf = "12345678901",
            Email = "joao@test.com",
            Phone = "11999999999",
            BirthDate = new DateOnly(2000, 1, 1),
            Gender = Gender.Male,
            City = "São Paulo",
            MaritalStatus = MaritalStatus.Single,
            Pregnancy = PregnancyStatus.None,
            ShirtSize = ShirtSize.M,
            WeightKg = 80m,
            HeightCm = 180m,
            Profession = "Dev",
            StreetAndNumber = "Rua A, 123",
            Neighborhood = "Centro",
            State = UF.SP,
            Whatsapp = "11988887777",
            FacebookUsername = "joao.fb",
            InstagramHandle = "joao.ig",
            NeighborPhone = "1133334444",
            RelativePhone = "11911112222",
            FatherStatus = ParentStatus.Alive,
            FatherName = "Pai",
            FatherPhone = "1133332222",
            MotherStatus = ParentStatus.Alive,
            MotherName = "Mae",
            MotherPhone = "11911113333",
            HadFamilyLossLast6Months = false,
            FamilyLossDetails = null,
            HasRelativeOrFriendSubmitted = false,
            SubmitterRelationship = RelationshipDegree.None,
            SubmitterNames = null,
            Religion = "Católica",
            PreviousUncalledApplications = RahaminAttempt.None,
            RahaminVidaCompleted = RahaminVidaEdition.None,
            AlcoholUse = AlcoholUsePattern.None,
            Smoker = false,
            UsesDrugs = false,
            DrugUseFrequency = null,
            HasAllergies = false,
            AllergiesDetails = null,
            HasMedicalRestriction = false,
            MedicalRestrictionDetails = null,
            TakesMedication = false,
            MedicationsDetails = null,
            PhysicalLimitationDetails = null,
            RecentSurgeryOrProcedureDetails = null,
            DocumentType = null,
            DocumentNumber = null
        };

    private static Mock<IFormFile> CreateMockFile(string fileName, string contentType, long length)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes("fake file content")));
        return file;
    }

    private static Domain.Entities.Registration CreateMockRegistration(Guid regId, Guid retreatId)
    {
        var reg = new Domain.Entities.Registration(
            new FullName("Test User"),
            new CPF("12345678901"),
            new EmailAddress("test@test.com"),
            "11999999999",
            new DateOnly(2000, 1, 1),
            Gender.Male,
            "São Paulo",
            RegistrationStatus.NotSelected,
            retreatId
        );
        
        // Use reflection to set Id if needed, or rely on entity's constructor
        return reg;
    }
}
