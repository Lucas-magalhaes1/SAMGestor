using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Application.Interfaces;            
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;                  
using SAMGestor.Domain.ValueObjects;

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
        var response = new GetAllRegistrationsResponse(emptyItems, 0, 0, 20);

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
        
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(response);
    }
    
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
}
