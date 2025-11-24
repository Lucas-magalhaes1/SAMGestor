using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
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
using GetAllFamilyDto = SAMGestor.Application.Features.Families.GetAll.FamilyDto;
using GetByIdFamilyDto = SAMGestor.Application.Features.Families.GetById.FamilyDto;
using GetByIdMemberDto = SAMGestor.Application.Features.Families.GetById.MemberDto;
using GetByIdFamilyAlertDto = SAMGestor.Application.Features.Families.GetById.FamilyAlertDto;
using UpdateFamilyDtoAlias = SAMGestor.Application.Features.Families.Update.UpdateFamilyDto;
using UpdateFamilyAlertDtoAlias = SAMGestor.Application.Features.Families.Update.FamilyAlertDto;
using UpdateMemberReadDtoAlias = SAMGestor.Application.Features.Families.Update.MemberDto;
using UpdateFamilyReadDtoAlias = SAMGestor.Application.Features.Families.Update.FamilyDto;


namespace SAMGestor.UnitTests.API.Controllers
{
    public sealed class RetreatFamiliesControllerTests
    {
        private static RetreatFamiliesController NewController(Mock<IMediator> mediator)
            => new RetreatFamiliesController(mediator.Object);
        

        [Fact]
        public async Task Generate_returns_Ok_and_overrides_RetreatId_from_route()
        {
            var m = new Mock<IMediator>();
            var routeId = Guid.NewGuid();
            var bodyId  = Guid.NewGuid(); 

            var expected = new GenerateFamiliesResponse(
                Version: 3,
                Families: new List<GeneratedFamilyDto>());

            m.Setup(x => x.Send(It.IsAny<GenerateFamiliesCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(expected);

            var ctrl = NewController(m);

            var body = new GenerateFamiliesCommand(
                RetreatId: bodyId,
                Capacity: 4,
                ReplaceExisting: true,
                FillExistingFirst: false
            );

            var res = await ctrl.Generate(routeId, body, default);

            res.Result.Should().BeOfType<OkObjectResult>()
               .Which.Value.Should().Be(expected);

            m.Verify(x => x.Send(
                It.Is<GenerateFamiliesCommand>(c => c.RetreatId == routeId),
                It.IsAny<CancellationToken>()), Times.Once);
        }


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
        public async Task GetById_returns_Ok()
        {
            var m = new Mock<IMediator>();
            var retreatId = Guid.NewGuid();
            var familyId  = Guid.NewGuid();

            var expected = new GetFamilyByIdResponse(
                Version: 2,
                Family: new GetByIdFamilyDto(
                    FamilyId: familyId,
                    Name: "Família X",
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
                        Capacity: 4,
                        TotalMembers: 4,
                        MaleCount: 2,
                        FemaleCount: 2,
                        Remaining: 0,
                        Members: new List<UpdateMemberReadDtoAlias>(),
                        Alerts: new List<UpdateFamilyAlertDtoAlias>())},
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

            m.Verify(x => x.Send(
                It.Is<UpdateFamiliesCommand>(c => c.RetreatId == retreatId),
                It.IsAny<CancellationToken>()), Times.Once);
        }
        

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
            var familyId  = Guid.NewGuid();

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
        

        [Fact]
        public async Task DeleteFamily_returns_NoContent()
        {
            var m = new Mock<IMediator>();
            var retreatId = Guid.NewGuid();
            var familyId  = Guid.NewGuid();

            m.Setup(x => x.Send(It.IsAny<DeleteFamilyCommand>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(Unit.Value);

            var ctrl = NewController(m);
            var res = await ctrl.DeleteFamily(retreatId, familyId, default);

            res.Should().BeOfType<NoContentResult>();

            m.Verify(x => x.Send(
                It.Is<DeleteFamilyCommand>(c => c.RetreatId == retreatId && c.FamilyId == familyId),
                It.IsAny<CancellationToken>()), Times.Once);
        }


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
                MemberIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
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

            payload.Should().ContainKey("familyId");
            payload.Should().ContainKey("version");
            payload.Should().ContainKey("warnings");

            payload["familyId"].Should().Be(createdFamilyId);
            payload["version"].Should().Be(9);
            ((IReadOnlyList<CreateFamilyWarningDto>)payload["warnings"]!).Should().BeEmpty();

            m.Verify(x => x.Send(
                It.Is<CreateFamilyCommand>(c =>
                    c.RetreatId == retreatId &&
                    c.Name == body.Name &&
                    c.MemberIds.SequenceEqual(body.MemberIds!) &&
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
                MemberIds: null!,          
                IgnoreWarnings: false
            );

            var res = await ctrl.CreateFamily(retreatId, body, default);
            res.Should().BeOfType<UnprocessableEntityObjectResult>();
            var unp = (UnprocessableEntityObjectResult)res;
            
            var props = unp.Value!.GetType().GetProperties()
                .ToDictionary(p => p.Name, p => p.GetValue(unp.Value));

            props.Should().ContainKey("version");
            props.Should().ContainKey("warnings");

            props["version"].Should().Be(8);
            ((IReadOnlyList<CreateFamilyWarningDto>)props["warnings"]!).Should().BeEquivalentTo(warnings);

            m.Verify(x => x.Send(
                It.Is<CreateFamilyCommand>(c =>
                    c.RetreatId == retreatId &&
                    c.Name == body.Name &&
                    c.MemberIds != null && c.MemberIds.Count == 0 &&  
                    c.IgnoreWarnings == body.IgnoreWarnings
                ),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
