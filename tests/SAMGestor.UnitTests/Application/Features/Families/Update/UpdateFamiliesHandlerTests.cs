using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

using FamilyInput = SAMGestor.Application.Features.Families.Update.UpdateFamilyDto;
using MemberInput = SAMGestor.Application.Features.Families.Update.UpdateMemberDto;

namespace SAMGestor.UnitTests.Application.Features.Families.Update;

public sealed class UpdateFamiliesHandlerTests
{
    
    private static Retreat NewOpenRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50)
        );

    private static Registration R(Guid retreatId, string name, Gender g, string city = "SP")
        => new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress($"{Guid.NewGuid():N}@mail.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            RegistrationStatus.Confirmed,
            retreatId
        );

    private static Family F(Guid retreatId, string name = "Família X", int capacity = 4, bool locked = false)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity);
        if (locked) f.Lock();
        return f;
    }

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static UpdateFamiliesCommand Cmd(
        Guid retreatId,
        int version,
        IEnumerable<(Family family, (Guid regId, int pos)[] members)> fams,
        bool ignoreWarnings = true)
    {
        var families = fams
            .Select<(Family family, (Guid regId, int pos)[] members), FamilyInput>(x =>
                new FamilyInput(
                    FamilyId: x.family.Id,
                    Name: (string)x.family.Name,
                    Capacity: x.family.Capacity,
                    Members: x.members
                        .Select<(Guid regId, int pos), MemberInput>(m => new MemberInput(m.regId, m.pos))
                        .ToList()
                )
            )
            .ToList();

        return new UpdateFamiliesCommand(
            RetreatId: retreatId,
            Version: version,
            Families: families,
            IgnoreWarnings: ignoreWarnings
        );
    }

    private static (Mock<IRetreatRepository> retRepo,
                    Mock<IFamilyRepository> famRepo,
                    Mock<IFamilyMemberRepository> fmRepo,
                    Mock<IRegistrationRepository> regRepo,
                    Mock<IRelationshipService> relSvc,
                    Mock<IUnitOfWork> uow)
        Mocks()
    {
        return (new Mock<IRetreatRepository>(),
                new Mock<IFamilyRepository>(),
                new Mock<IFamilyMemberRepository>(),
                new Mock<IRegistrationRepository>(),
                new Mock<IRelationshipService>(),
                new Mock<IUnitOfWork>());
    }

    
    [Fact]
    public async Task NotFound_Retreat_returns_error_payload_without_throw()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();

        var retreatId = Guid.NewGuid();
        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreatId, 0, new List<FamilyInput>(), true);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(0);
        res.Errors.Should().ContainSingle(e => e.Code == "NOT_FOUND");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Global_lock_throws_BusinessRuleException()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retreat.LockFamilies();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreat.Id, retreat.FamiliesVersion, new List<FamilyInput>(), true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Version_mismatch_returns_error_payload_without_persist()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family>());

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(retreat.Id, 1, new List<FamilyInput>(), true);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Errors.Should().ContainSingle(e => e.Code == "VERSION_MISMATCH");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Touch_locked_family_returns_422_like_payload_FAMILY_LOCKED()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fLocked = F(retreat.Id, "Família L", 4, locked: true);
        var fOpen   = F(retreat.Id, "Família A");

        var r1 = R(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = R(retreat.Id, "Maria Souza", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Lima", Gender.Male);
        var r4 = R(retreat.Id, "Ana Santos", Gender.Female);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fLocked, fOpen });

        fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(fLocked.Id) && ids.Contains(fOpen.Id)), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
              {
                  [fLocked.Id] = new() { Link(retreat.Id, fLocked.Id, r1.Id, 0), Link(retreat.Id, fLocked.Id, r2.Id, 1) },
                  [fOpen.Id]   = new() { Link(retreat.Id, fOpen.Id,   r3.Id, 0), Link(retreat.Id, fOpen.Id,   r4.Id, 1) }
              });

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1, [r2.Id] = r2, [r3.Id] = r3, [r4.Id] = r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = Cmd(
            retreat.Id,
            retreat.FamiliesVersion,
            new[]
            {
                (fLocked, new (Guid,int)[]{ (r1.Id,0), (r3.Id,1), (r2.Id,2), (r4.Id,3) }),
                (fOpen,   new (Guid,int)[]{ (r3.Id,0), (r4.Id,1), (r1.Id,2), (r2.Id,3) })
            },
            ignoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "FAMILY_LOCKED" && e.FamilyId == fLocked.Id);
        res.Families.Should().BeEmpty();
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_family_returns_error_payload_and_does_not_persist()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fKnown = F(retreat.Id, "Família A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fKnown });

        var fakeFamId = Guid.NewGuid();
        var r1 = R(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = R(retreat.Id, "Ana Lima", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Costa", Gender.Male);
        var r4 = R(retreat.Id, "Bea Souza", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(
            RetreatId: retreat.Id,
            Version: retreat.FamiliesVersion,
            Families: new List<FamilyInput>
            {
                new FamilyInput(
                    fakeFamId, "X", 4,
                    new List<MemberInput>
                    {
                        new MemberInput(r1.Id,0),
                        new MemberInput(r2.Id,1),
                        new MemberInput(r3.Id,2),
                        new MemberInput(r4.Id,3),
                    })
            },
            IgnoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_FAMILY" && e.FamilyId == fakeFamId);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unknown_registration_returns_error_payload()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>());

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var ghost = Guid.NewGuid();
        var cmd = new UpdateFamiliesCommand(
            retreat.Id,
            retreat.FamiliesVersion,
            new List<FamilyInput>
            {
                new FamilyInput(
                    fam.Id, "Fam A", 4,
                    new List<MemberInput>
                    {
                        new MemberInput(ghost,0),
                        new MemberInput(ghost,1),
                        new MemberInput(ghost,2),
                        new MemberInput(ghost,3),
                    })
            },
            true);

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_REGISTRATION");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Capacity_invalid_capacity_mismatch_and_composition_errors_are_reported()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A", capacity: 4);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "A A", Gender.Male);
        var r2 = R(retreat.Id, "B B", Gender.Male);
        var r3 = R(retreat.Id, "C C", Gender.Male);
        var r4 = R(retreat.Id, "D D", Gender.Male);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4
               });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = new UpdateFamiliesCommand(
            RetreatId: retreat.Id,
            Version: retreat.FamiliesVersion,
            Families: new List<FamilyInput>
            {
                new FamilyInput(
                    fam.Id, "Fam A", 3, 
                    new List<MemberInput>
                    {
                        new MemberInput(r1.Id,0),
                        new MemberInput(r2.Id,1),
                        new MemberInput(r3.Id,2),
                        new MemberInput(r4.Id,3),
                    })
            },
            IgnoreWarnings: true
        );

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "CAPACITY_INVALID");
        res.Errors.Should().Contain(e => e.Code == "CAPACITY_MISMATCH");
        res.Errors.Should().Contain(e => e.Code == "COMPOSITION_INVALID");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Relationship_conflict_and_same_surname_are_reported()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = R(retreat.Id, "Maria Silva", Gender.Female);
        var r3 = R(retreat.Id, "Pedro Souza", Gender.Male);
        var r4 = R(retreat.Id, "Ana Santos", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        
        relSvc.Setup(s => s.AreSpousesAsync(r1.Id, r2.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        relSvc.Setup(s => s.AreDirectRelativesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
        {
            (fam, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) })
        });

        var res = await handler.Handle(cmd, default);

        res.Errors.Should().Contain(e => e.Code == "RELATIONSHIP_CONFLICT");
        res.Errors.Should().Contain(e => e.Code == "SAME_SURNAME");
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Same_city_warnings_stop_when_ignoreWarnings_false_and_persist_when_true()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "A A", Gender.Male, city: "Recife");
        var r2 = R(retreat.Id, "B B", Gender.Female, city: "Recife");
        var r3 = R(retreat.Id, "C C", Gender.Male, city: "São Paulo");
        var r4 = R(retreat.Id, "D D", Gender.Female, city: "Rio");

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd1 = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
        {
            (fam, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) })
        }, ignoreWarnings: false);

        var res1 = await handler.Handle(cmd1, default);
        res1.Warnings.Should().NotBeEmpty();
        res1.Families.Should().BeEmpty();
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        
        famRepo.Invocations.Clear();
        uow.Invocations.Clear();

        famRepo.Setup(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.RemoveByFamilyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        retRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });
        fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
              {
                  [fam.Id] = new() {
                      Link(retreat.Id, fam.Id, r1.Id,0),
                      Link(retreat.Id, fam.Id, r2.Id,1),
                      Link(retreat.Id, fam.Id, r3.Id,2),
                      Link(retreat.Id, fam.Id, r4.Id,3),
                  }
              });
        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var cmd2 = cmd1 with { IgnoreWarnings = true };

        var prev = retreat.FamiliesVersion;
        var res2 = await handler.Handle(cmd2, default);

        res2.Errors.Should().BeEmpty();
        res2.Families.Should().NotBeEmpty();
        res2.Version.Should().Be(prev + 1);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Happy_path_updates_members_and_bumps_version()
    {
        var (retRepo, famRepo, fmRepo, regRepo, relSvc, uow) = Mocks();
        var retreat = NewOpenRetreat();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var fam = F(retreat.Id, "Fam A");
        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });

        var r1 = R(retreat.Id, "Joao A", Gender.Male);
        var r2 = R(retreat.Id, "Maria B", Gender.Female);
        var r3 = R(retreat.Id, "Pedro C", Gender.Male);
        var r4 = R(retreat.Id, "Ana D", Gender.Female);

        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        relSvc.Setup(s => s.AreSpousesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        relSvc.Setup(s => s.AreDirectRelativesAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        famRepo.Setup(x => x.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.RemoveByFamilyIdAsync(fam.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        fmRepo.Setup(x => x.AddRangeAsync(It.Is<IEnumerable<FamilyMember>>(ls => ls.Count() == 4), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        retRepo.Setup(x => x.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        famRepo.Setup(x => x.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Family> { fam });
        fmRepo.Setup(x => x.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>
              {
                  [fam.Id] = new()
                  {
                      Link(retreat.Id, fam.Id, r1.Id,0),
                      Link(retreat.Id, fam.Id, r2.Id,1),
                      Link(retreat.Id, fam.Id, r3.Id,2),
                      Link(retreat.Id, fam.Id, r4.Id,3),
                  }
              });
        regRepo.Setup(x => x.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4 });

        var handler = new UpdateFamiliesHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, relSvc.Object, uow.Object);

        var cmd = Cmd(retreat.Id, retreat.FamiliesVersion, new[]
        {
            (fam, new (Guid,int)[]{ (r1.Id,0),(r2.Id,1),(r3.Id,2),(r4.Id,3) })
        });

        var prev = retreat.FamiliesVersion;
        var res  = await handler.Handle(cmd, default);

        res.Errors.Should().BeEmpty();
        res.Warnings.Should().BeEmpty();
        res.Families.Should().HaveCount(1);
        res.Version.Should().Be(prev + 1);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
