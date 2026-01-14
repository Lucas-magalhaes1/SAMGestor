using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers.Family;
using SAMGestor.Application.Features.Families.Create;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Features.Families.GetAll;
using SAMGestor.Application.Features.Families.GetById;
using SAMGestor.Application.Features.Families.Lock;
using SAMGestor.Application.Features.Families.Reset;
using SAMGestor.Application.Features.Families.Unassigned;
using SAMGestor.Application.Features.Families.Update;
using SAMGestor.Application.Features.Families.UpdateGodparents;

using GetAllFamilyDto = SAMGestor.Application.Features.Families.GetAll.FamilyDto;
using GetByIdFamilyDto = SAMGestor.Application.Features.Families.GetById.FamilyDto;
using GetByIdMemberDto = SAMGestor.Application.Features.Families.GetById.MemberDto;
using GetByIdFamilyAlertDto = SAMGestor.Application.Features.Families.GetById.FamilyAlertDto;
using UpdateFamilyDtoAlias = SAMGestor.Application.Features.Families.Update.UpdateFamilyDto;
using UpdateFamilyAlertDtoAlias = SAMGestor.Application.Features.Families.Update.FamilyAlertDto;
using UpdateMemberReadDtoAlias = SAMGestor.Application.Features.Families.Update.MemberDto;
using UpdateFamilyReadDtoAlias = SAMGestor.Application.Features.Families.Update.FamilyDto;

namespace SAMGestor.UnitTests.API.Controllers;

public sealed class RetreatFamiliesControllerTests
{
    private static RetreatFamiliesController NewController(Mock<IMediator> mediator)
        => new(mediator.Object);

    // ===== TESTES DE GENERATE =====

    [Fact]
    public async Task Generate_returns_Ok_and_overrides_RetreatId_from_route()
    {
        var m = new Mock<IMediator>();
        var routeId = Guid.NewGuid();
        var bodyMembersPerFamily = 4;

        var expected = new GenerateFamiliesResponse(
            Version: 3,
            Families: new List<GeneratedFamilyDto>());

        m.Setup(x => x.Send(It.IsAny<GenerateFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var body = new RetreatFamiliesController.GenerateFamiliesRequest(
            MembersPerFamily: bodyMembersPerFamily,
            ReplaceExisting: true,
            FillExistingFirst: false
        );

        var res = await ctrl.Generate(routeId, body, default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<GenerateFamiliesCommand>(c =>
                c.RetreatId == routeId &&
                c.MembersPerFamily == bodyMembersPerFamily &&
                c.ReplaceExisting == true &&
                c.FillExistingFirst == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== TESTES DE LIST =====

    [Fact]
    public async Task List_returns_Ok_with_query_params()
    {
        var m = new Mock<IMediator>();
        var routeId = Guid.NewGuid();

        var expected = new GetAllFamiliesResponse(
            Version: 5,
            FamiliesLocked: false,
            Families: new List<GetAllFamilyDto>());

        m.Setup(x => x.Send(It.IsAny<GetAllFamiliesQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var res = await ctrl.List(routeId, includeAlerts: true, ct: default);

        res.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<GetAllFamiliesQuery>(q => q.RetreatId == routeId && q.IncludeAlerts),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_defaults_includeAlerts_to_true()
    {
        var m = new Mock<IMediator>();
        var routeId = Guid.NewGuid();

        m.Setup(x => x.Send(It.IsAny<GetAllFamiliesQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new GetAllFamiliesResponse(0, false, new List<GetAllFamilyDto>()));

        var ctrl = NewController(m);
        await ctrl.List(routeId, ct: default);

        m.Verify(x => x.Send(
            It.Is<GetAllFamiliesQuery>(q => q.IncludeAlerts == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== TESTES DE GETBYID =====

    [Fact]
    public async Task GetById_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new GetFamilyByIdResponse(
            Version: 2,
            Family: new GetByIdFamilyDto(
                FamilyId: familyId,
                Name: "Família X",
                ColorName: "Azul",
                ColorHex: "#0000FF",
                Capacity: 4,
                TotalMembers: 4,
                MaleCount: 2,
                FemaleCount: 2,
                Remaining: 0,
                IsLocked: false,
                GroupStatus: null,
                GroupLink: null,
                GroupExternalId: null,
                GroupChannel: null,
                GroupCreatedAt: null,
                GroupLastNotifiedAt: null,
                GroupVersion: 0,
                MalePercentage: 50,
                FemalePercentage: 50,
                Members: new List<GetByIdMemberDto>(),
                Alerts: new List<GetByIdFamilyAlertDto>()
            )
        );

        m.Setup(x => x.Send(It.IsAny<GetFamilyByIdQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);
        var res = await ctrl.GetById(retreatId, familyId, includeAlerts: true, ct: default);

        res.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<GetFamilyByIdQuery>(q => q.RetreatId == retreatId && q.FamilyId == familyId && q.IncludeAlerts),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_returns_404_when_family_not_found()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new GetFamilyByIdResponse(Version: 2, Family: null);

        m.Setup(x => x.Send(It.IsAny<GetFamilyByIdQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);
        var res = await ctrl.GetById(retreatId, familyId, includeAlerts: true, ct: default);

        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ===== TESTES DE CREATE =====

    [Fact]
    public async Task CreateFamily_returns_201_and_sets_Location_when_created_true()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var createdFamilyId = Guid.NewGuid();

        var mediatorResult = new CreateFamilyResult(
            Created: true,
            FamilyId: createdFamilyId,
            Version: 9,
            Warnings: new List<CreateFamilyWarningDto>());

        m.Setup(x => x.Send(It.IsAny<CreateFamilyCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(mediatorResult);

        var ctrl = NewController(m);

        var body = new CreateFamilyRequest(
            Name: "Família XPTO",
            ColorName: "Azul",
            MemberIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            Capacity: 4,
            PadrinhoIds: new List<Guid> { Guid.NewGuid() },
            MadrinhaIds: new List<Guid> { Guid.NewGuid() },
            IgnoreWarnings: true
        );

        var res = await ctrl.CreateFamily(retreatId, body, default);

        var created = res as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.ActionName.Should().Be(nameof(RetreatFamiliesController.GetById));
        created.RouteValues!.Should().ContainKey("retreatId").WhoseValue.Should().Be(retreatId);
        created.RouteValues!.Should().ContainKey("familyId").WhoseValue.Should().Be(createdFamilyId);

        var payload = created.Value!.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(created.Value));

        payload.Should().ContainKey("created");
        payload.Should().ContainKey("familyId");
        payload.Should().ContainKey("version");
        payload.Should().ContainKey("warnings");

        payload["created"].Should().Be(true);
        payload["familyId"].Should().Be(createdFamilyId);
        payload["version"].Should().Be(9);
        ((IReadOnlyList<CreateFamilyWarningDto>)payload["warnings"]!).Should().BeEmpty();

        m.Verify(x => x.Send(
            It.Is<CreateFamilyCommand>(c =>
                c.RetreatId == retreatId &&
                c.Name == body.Name &&
                c.ColorName == body.ColorName &&
                c.MemberIds.SequenceEqual(body.MemberIds!) &&
                c.PadrinhoIds!.SequenceEqual(body.PadrinhoIds!) &&
                c.MadrinhaIds!.SequenceEqual(body.MadrinhaIds!) &&
                c.IgnoreWarnings == body.IgnoreWarnings
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFamily_returns_422_with_version_and_warnings_when_created_false()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var warnings = new List<CreateFamilyWarningDto>
        {
            new("warning", "SAME_CITY", "msg", new List<Guid>())
        };

        var mediatorResult = new CreateFamilyResult(
            Created: false,
            FamilyId: null,
            Version: 8,
            Warnings: warnings
        );

        m.Setup(x => x.Send(It.IsAny<CreateFamilyCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(mediatorResult);

        var ctrl = NewController(m);

        var body = new CreateFamilyRequest(
            Name: "Família Y",
            ColorName: "Verde",
            MemberIds: null,
            Capacity: 4,
            PadrinhoIds: null,
            MadrinhaIds: null,
            IgnoreWarnings: false
        );

        var res = await ctrl.CreateFamily(retreatId, body, default);
        res.Should().BeOfType<UnprocessableEntityObjectResult>();
        
        var unp = (UnprocessableEntityObjectResult)res;
        var props = unp.Value!.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(unp.Value));

        props.Should().ContainKey("created");
        props.Should().ContainKey("version");
        props.Should().ContainKey("warnings");

        props["created"].Should().Be(false);
        props["version"].Should().Be(8);
        ((IReadOnlyList<CreateFamilyWarningDto>)props["warnings"]!).Should().BeEquivalentTo(warnings);
    }

    // ===== TESTES DE UPDATE =====

    [Fact]
    public async Task Update_returns_Ok_when_persisted()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var handlerResult = new UpdateFamiliesResponse(
            Version: 4,
            Families: new List<UpdateFamilyReadDtoAlias> {
                new(
                    FamilyId: Guid.NewGuid(),
                    Name: "Família 1",
                    ColorName: "Azul",
                    ColorHex: "#0000FF",
                    Capacity: 4,
                    TotalMembers: 4,
                    MaleCount: 2,
                    FemaleCount: 2,
                    Remaining: 0,
                    MalePercentage: 50,
                    FemalePercentage: 50,
                    IsLocked: false,
                    Members: new List<UpdateMemberReadDtoAlias>(),
                    Alerts: new List<UpdateFamilyAlertDtoAlias>())
            },
            Errors: new List<SAMGestor.Application.Features.Families.Update.FamilyErrorDto>(),
            Warnings: new List<SAMGestor.Application.Features.Families.Update.FamilyAlertDto>());

        m.Setup(x => x.Send(It.IsAny<UpdateFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(handlerResult);

        var ctrl = NewController(m);

        var body = new UpdateFamiliesCommand(
            RetreatId: Guid.NewGuid(),
            Version: 4,
            Families: new List<UpdateFamilyDtoAlias>(),
            IgnoreWarnings: true
        );

        var res = await ctrl.Update(retreatId, body, default);

        res.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(handlerResult);

        m.Verify(x => x.Send(
            It.Is<UpdateFamiliesCommand>(c => c.RetreatId == retreatId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_returns_422_when_not_persisted_and_has_errors_or_warnings()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var handlerResult = new UpdateFamiliesResponse(
            Version: 4,
            Families: new List<UpdateFamilyReadDtoAlias>(),
            Errors: new List<SAMGestor.Application.Features.Families.Update.FamilyErrorDto> {
                new("ANY", "erro", null, Array.Empty<Guid>())
            },
            Warnings: new List<SAMGestor.Application.Features.Families.Update.FamilyAlertDto>());

        m.Setup(x => x.Send(It.IsAny<UpdateFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(handlerResult);

        var ctrl = NewController(m);

        var body = new UpdateFamiliesCommand(
            RetreatId: Guid.NewGuid(),
            Version: 4,
            Families: new List<UpdateFamilyDtoAlias>(),
            IgnoreWarnings: false
        );

        var res = await ctrl.Update(retreatId, body, default);

        res.Should().BeOfType<UnprocessableEntityObjectResult>()
           .Which.Value.Should().Be(handlerResult);
    }

    // ===== TESTES DE UPDATEGODPARENTS (NOVO) =====

    [Fact]
    public async Task UpdateGodparents_returns_Ok_with_result()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new UpdateGodparentsResult(
            Success: true,
            Version: 10,
            Warnings: new List<string>());

        m.Setup(x => x.Send(It.IsAny<UpdateGodparentsCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var body = new UpdateGodparentsRequest(
            PadrinhoIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            MadrinhaIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        );

        var res = await ctrl.UpdateGodparents(retreatId, familyId, body, default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<UpdateGodparentsCommand>(c =>
                c.RetreatId == retreatId &&
                c.FamilyId == familyId &&
                c.PadrinhoIds.SequenceEqual(body.PadrinhoIds!) &&
                c.MadrinhaIds.SequenceEqual(body.MadrinhaIds!)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGodparents_handles_null_lists()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new UpdateGodparentsResult(
            Success: true,
            Version: 10,
            Warnings: new List<string> { "Família não possui padrinhos nem madrinhas definidos." });

        m.Setup(x => x.Send(It.IsAny<UpdateGodparentsCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var body = new UpdateGodparentsRequest(
            PadrinhoIds: null,
            MadrinhaIds: null
        );

        var res = await ctrl.UpdateGodparents(retreatId, familyId, body, default);

        res.Result.Should().BeOfType<OkObjectResult>();

        m.Verify(x => x.Send(
            It.Is<UpdateGodparentsCommand>(c =>
                c.PadrinhoIds.Count == 0 &&
                c.MadrinhaIds.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGodparents_returns_Ok_with_warnings()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new UpdateGodparentsResult(
            Success: true,
            Version: 10,
            Warnings: new List<string> { "Família tem apenas 1 padrinho(s). Recomendado: 2." });

        m.Setup(x => x.Send(It.IsAny<UpdateGodparentsCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var body = new UpdateGodparentsRequest(
            PadrinhoIds: new List<Guid> { Guid.NewGuid() },
            MadrinhaIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        );

        var res = await ctrl.UpdateGodparents(retreatId, familyId, body, default);

        var result = res.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = result.Value as UpdateGodparentsResult;
        
        value.Should().NotBeNull();
        value!.Success.Should().BeTrue();
        value.Warnings.Should().NotBeEmpty();
    }

    // ===== TESTES DE LOCK =====

    [Fact]
    public async Task Lock_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var expected = new LockFamiliesResponse(Version: 10, Locked: true);
        m.Setup(x => x.Send(It.IsAny<LockFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var res = await ctrl.Lock(retreatId, new RetreatFamiliesController.LockFamiliesRequest(true), default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<LockFamiliesCommand>(c => c.RetreatId == retreatId && c.Lock),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LockFamily_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new LockSingleFamilyResponse(FamilyId: familyId, Locked: false, Version: 7);
        m.Setup(x => x.Send(It.IsAny<LockSingleFamilyCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);

        var res = await ctrl.LockFamily(retreatId, familyId, new RetreatFamiliesController.LockFamilyRequest(false), default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<LockSingleFamilyCommand>(c => c.RetreatId == retreatId && c.FamilyId == familyId && !c.Lock),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== TESTES DE DELETE =====

    [Fact]
    public async Task DeleteFamily_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();

        var expected = new DeleteFamilyResponse(Version: 5, FamilyName: "Família 1", MembersDeleted: 3);
        m.Setup(x => x.Send(It.IsAny<DeleteFamilyCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);
        var res = await ctrl.DeleteFamily(retreatId, familyId, default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<DeleteFamilyCommand>(c => c.RetreatId == retreatId && c.FamilyId == familyId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== TESTES DE UNASSIGNED =====

    [Fact]
    public async Task Unassigned_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var expected = new GetUnassignedResponse(
            new List<UnassignedMemberDto>
            {
                new(Guid.NewGuid(), "Ana Silva", "Female", "Recife", "ana@x.com")
            });

        m.Setup(x => x.Send(It.IsAny<GetUnassignedQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);
        var res = await ctrl.Unassigned(retreatId, gender: "female", city: "recife", search: "ana", ct: default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<GetUnassignedQuery>(q =>
                q.RetreatId == retreatId && q.Gender == "female" && q.City == "recife" && q.Search == "ana"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unassigned_accepts_null_filters()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        m.Setup(x => x.Send(It.IsAny<GetUnassignedQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new GetUnassignedResponse(new List<UnassignedMemberDto>()));

        var ctrl = NewController(m);
        await ctrl.Unassigned(retreatId, ct: default);

        m.Verify(x => x.Send(
            It.Is<GetUnassignedQuery>(q =>
                q.Gender == null && q.City == null && q.Search == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== TESTES DE RESET =====

    [Fact]
    public async Task Reset_returns_Ok()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        var expected = new ResetFamiliesResponse(
            Version: 12,
            FamiliesDeleted: 3,
            MembersDeleted: 10);

        m.Setup(x => x.Send(It.IsAny<ResetFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(expected);

        var ctrl = NewController(m);
        var res = await ctrl.Reset(retreatId, new RetreatFamiliesController.ResetFamiliesRequest(ForceLockedFamilies: true), default);

        res.Result.Should().BeOfType<OkObjectResult>()
           .Which.Value.Should().Be(expected);

        m.Verify(x => x.Send(
            It.Is<ResetFamiliesCommand>(c => c.RetreatId == retreatId && c.ForceLockedFamilies),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reset_defaults_ForceLockedFamilies_to_false()
    {
        var m = new Mock<IMediator>();
        var retreatId = Guid.NewGuid();

        m.Setup(x => x.Send(It.IsAny<ResetFamiliesCommand>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new ResetFamiliesResponse(0, 0, 0));

        var ctrl = NewController(m);
        await ctrl.Reset(retreatId, new RetreatFamiliesController.ResetFamiliesRequest(ForceLockedFamilies: false), default);

        m.Verify(x => x.Send(
            It.Is<ResetFamiliesCommand>(c => c.ForceLockedFamilies == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
