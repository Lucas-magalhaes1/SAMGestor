using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups.RetryFailed;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.RetryFailed;

public class RetryFailedGroupsHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro Retry"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"), new Money(0, "BRL"),
            new Percentage(50), new Percentage(50));

    private static Family Fam(Guid retreatId, string name, int capacity = 4, GroupStatus status = GroupStatus.None)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity);
        switch (status)
        {
            case GroupStatus.Creating: f.MarkGroupCreating(); break;
            case GroupStatus.Active: f.MarkGroupActive("https://chat/x", "ext", "whatsapp", DateTimeOffset.UtcNow, null); break;
            case GroupStatus.Failed: f.MarkGroupFailed(); break;
        }
        return f;
    }

    private static Registration NewReg(Guid retreatId, string name, Gender g, string? email = null, string phone = "11999999999")
        => new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress(email ?? $"{Guid.NewGuid():N}@example.com"),
            phone,
            new DateOnly(1990, 1, 1),
            g,
            "SP",
            RegistrationStatus.Confirmed,
            ParticipationCategory.Guest,
            "Oeste",
            retreatId);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static RetryFailedGroupsHandler BuildHandler(
        Retreat retreat,
        List<Family> families,
        Dictionary<Guid, List<FamilyMember>> linksByFamily,
        Dictionary<Guid, Registration> regsMap,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> famRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IRegistrationRepository> regRepo,
        out Mock<IEventBus> bus,
        out Mock<IUnitOfWork> uow)
    {
        retRepo = new Mock<IRetreatRepository>();
        famRepo = new Mock<IFamilyRepository>();
        fmRepo  = new Mock<IFamilyMemberRepository>();
        regRepo = new Mock<IRegistrationRepository>();
        bus     = new Mock<IEventBus>();
        uow     = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        famRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(families);
        fmRepo.Setup(r => r.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(linksByFamily);
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(regsMap);
        famRepo.Setup(r => r.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        bus.Setup(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new RetryFailedGroupsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);
    }

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retreatId = Guid.NewGuid();
        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();
        var fmRepo  = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var bus     = new Mock<IEventBus>();
        var uow     = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

        var sut = new RetryFailedGroupsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);

        await FluentActions.Invoking(() => sut.Handle(new RetryFailedGroupsCommand(retreatId), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Retorna_zero_quando_nao_ha_familias_failed()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "A", status: GroupStatus.None);
        var f2 = Fam(retreat.Id, "B", status: GroupStatus.Active);

        var sut = BuildHandler(
            retreat,
            new List<Family> { f1, f2 },
            new Dictionary<Guid, List<FamilyMember>>(),
            new Dictionary<Guid, Registration>(),
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new RetryFailedGroupsCommand(retreat.Id), default);

        res.TotalFailed.Should().Be(0);
        res.Queued.Should().Be(0);
        res.Skipped.Should().Be(0);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        famRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pula_quando_incompleta_skipped_incrementa_e_salva_uma_vez()
    {
        var retreat = OpenRetreat();
        var failed = Fam(retreat.Id, "Fail 1", capacity: 3, status: GroupStatus.Failed);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Bruno Souza", Gender.Male, "b@mail.com");

        var linksByFamily = new Dictionary<Guid, List<FamilyMember>> {
            [failed.Id] = new() {
                Link(retreat.Id, failed.Id, r1.Id, 0),
                Link(retreat.Id, failed.Id, r2.Id, 1)
            }
        };
        var regsMap = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2 };

        var sut = BuildHandler(
            retreat,
            new List<Family> { failed },
            linksByFamily,
            regsMap,
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new RetryFailedGroupsCommand(retreat.Id), default);

        res.TotalFailed.Should().Be(1);
        res.Queued.Should().Be(0);
        res.Skipped.Should().Be(1);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        famRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Pula_quando_sem_entrada_em_linksByFamily_equivale_a_zero_membros()
    {
        var retreat = OpenRetreat();
        var failed = Fam(retreat.Id, "Fail 2", capacity: 2, status: GroupStatus.Failed);

        var sut = BuildHandler(
            retreat,
            new List<Family> { failed },
            new Dictionary<Guid, List<FamilyMember>>(), 
            new Dictionary<Guid, Registration>(),
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new RetryFailedGroupsCommand(retreat.Id), default);

        res.TotalFailed.Should().Be(1);
        res.Queued.Should().Be(0);
        res.Skipped.Should().Be(1);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        famRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_enfileira_todas_failed_completas_marca_creating_e_salva()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "Fail A", capacity: 2, status: GroupStatus.Failed);
        var f2 = Fam(retreat.Id, "Fail B", capacity: 2, status: GroupStatus.Failed);
        var other = Fam(retreat.Id, "Ok", capacity: 2, status: GroupStatus.Active);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Bea Souza", Gender.Female, "b@mail.com");
        var r3 = NewReg(retreat.Id, "Caio Lima", Gender.Male, "c@mail.com");
        var r4 = NewReg(retreat.Id, "Davi Rocha", Gender.Male, "d@mail.com");

        var linksByFamily = new Dictionary<Guid, List<FamilyMember>> {
            [f1.Id] = new() { Link(retreat.Id, f1.Id, r1.Id, 1), Link(retreat.Id, f1.Id, r2.Id, 0) },
            [f2.Id] = new() { Link(retreat.Id, f2.Id, r3.Id, 0), Link(retreat.Id, f2.Id, r4.Id, 1) },
            [other.Id] = new() { Link(retreat.Id, other.Id, r1.Id, 0), Link(retreat.Id, other.Id, r3.Id, 1) }
        };

        var regsMap = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 };

        var captured = new List<FamilyGroupCreateRequestedV1>();

        var sut = BuildHandler(
            retreat,
            new List<Family> { f1, f2, other },
            linksByFamily,
            regsMap,
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupCreateRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string, string, object, string?, CancellationToken>((t, s, data, tr, ct) =>
           {
               captured.Add((FamilyGroupCreateRequestedV1)data);
           })
           .Returns(Task.CompletedTask);

        var v1 = f1.GroupVersion;
        var v2 = f2.GroupVersion;

        var res = await sut.Handle(new RetryFailedGroupsCommand(retreat.Id), default);

        res.TotalFailed.Should().Be(2);
        res.Queued.Should().Be(2);
        res.Skipped.Should().Be(0);

        captured.Should().HaveCount(2);
        captured.All(e => e.ForceRecreate).Should().BeTrue();
        captured.Select(e => e.FamilyId).Should().BeEquivalentTo(new[] { f1.Id, f2.Id });

        f1.GroupStatus.Should().Be(GroupStatus.Creating);
        f2.GroupStatus.Should().Be(GroupStatus.Creating);
        f1.GroupVersion.Should().Be(v1 + 1);
        f2.GroupVersion.Should().Be(v2 + 1);

        famRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Evento_monta_membros_ordenados_por_position_e_email_nulo_eh_aceito()
    {
        var retreat = OpenRetreat();
        var f = Fam(retreat.Id, "Fail M", capacity: 3, status: GroupStatus.Failed);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com", "111");
        var r2 = NewReg(retreat.Id, "Bruno Lima", Gender.Male, "b@mail.com", "222");
        var r3 = NewReg(retreat.Id, "Carla Pires", Gender.Female, "c@mail.com", "333");

        var linksByFamily = new Dictionary<Guid, List<FamilyMember>> {
            [f.Id] = new() {
                Link(retreat.Id, f.Id, r2.Id, 2),
                Link(retreat.Id, f.Id, r1.Id, 0),
                Link(retreat.Id, f.Id, r3.Id, 1)
            }
        };
        var regsMap = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3 };

        var sut = BuildHandler(
            retreat,
            new List<Family> { f },
            linksByFamily,
            regsMap,
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        FamilyGroupCreateRequestedV1? captured = null;
        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupCreateRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string, string, object, string?, CancellationToken>((t, s, data, tr, ct) => captured = (FamilyGroupCreateRequestedV1)data)
           .Returns(Task.CompletedTask);

        var res = await sut.Handle(new RetryFailedGroupsCommand(retreat.Id), default);

        captured.Should().NotBeNull();
        captured!.Members.Select(m => m.RegistrationId).Should().ContainInOrder(r1.Id, r3.Id, r2.Id);
        captured.Members[1].Email.Should().Be("c@mail.com");
        captured.Members[2].Email.Should().Be("b@mail.com");
    }
}
