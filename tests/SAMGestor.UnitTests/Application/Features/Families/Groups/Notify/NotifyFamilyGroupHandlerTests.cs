using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups.Notify;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.Notify;

public class NotifyFamilyGroupHandlerTests
{
    private static Retreat OpenRetreat(bool locked = true)
    {
        var r = new Retreat(
            new FullName("Retiro Notificação"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));
        if (locked) r.LockFamilies();
        return r;
    }

    private static Family Fam(Guid retreatId, string name, int capacity = 4, bool locked = false)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity, FamilyColor.FromName("Azul"));
        if (locked) f.Lock();
        return f;
    }

    private static Registration NewReg(Guid retreatId, string name, Gender g,
        string? email = null, string phone = "11999999999") =>
        new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress(email ?? $"{Guid.NewGuid():N}@example.com"),
            phone,
            new DateOnly(1990,1,1),
            g,
            "SP",
            RegistrationStatus.Confirmed,
            retreatId);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static NotifyFamilyGroupHandler BuildHandler(
        Retreat retreat,
        Family family,
        List<FamilyMember> links,
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

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        famRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(family);

        fmRepo.Setup(r => r.ListByFamilyAsync(family.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(links);

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(regsMap);

        famRepo.Setup(r => r.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        bus.Setup(b => b.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new NotifyFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);
    }
    

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retreatId = Guid.NewGuid();
        var familyId  = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();
        var fmRepo  = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var bus     = new Mock<IEventBus>();
        var uow     = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var sut = new NotifyFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);

        await FluentActions.Invoking(() => sut.Handle(new NotifyFamilyGroupCommand(retreatId, familyId, false), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_existe()
    {
        var retreat = OpenRetreat(locked: true);
        var familyId = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();
        var fmRepo  = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var bus     = new Mock<IEventBus>();
        var uow     = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        famRepo.Setup(r => r.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Family?)null);

        var sut = new NotifyFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);

        await FluentActions.Invoking(() => sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, familyId, false), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_pertence_ao_retiro()
    {
        var retreat = OpenRetreat(locked: true);
        var other   = OpenRetreat(locked: true);

        var family = Fam(other.Id, "Fam X");
        var sut = BuildHandler(
            retreat,
            family,
            links: new List<FamilyMember>(),
            regsMap: new Dictionary<Guid, Registration>(),
            out _, out _, out _, out _, out _, out _
        );

        await FluentActions.Invoking(() => sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }

    [Fact]
    public async Task Falha_quando_sem_lock_global_e_sem_lock_da_familia()
    {
        var retreat = OpenRetreat(locked: false); 
        var family  = Fam(retreat.Id, "Fam A", locked: false);

        var sut = BuildHandler(
            retreat,
            family,
            links: new List<FamilyMember>(),
            regsMap: new Dictionary<Guid, Registration>(),
            out _, out _, out _, out _, out _, out _
        );

        await FluentActions.Invoking(() => sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*travar o retiro ou a família*");
    }

    [Fact]
    public async Task Falha_quando_familia_incompleta()
    {
        var retreat = OpenRetreat(locked: true);
        var family  = Fam(retreat.Id, "Fam A", capacity: 4);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Bea Souza", Gender.Female, "b@mail.com");
        var r3 = NewReg(retreat.Id, "Caio Lima", Gender.Male,   "c@mail.com");

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r1.Id, 0),
            Link(retreat.Id, family.Id, r2.Id, 1),
            Link(retreat.Id, family.Id, r3.Id, 2),
        };

        var regs = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3 };

        var sut = BuildHandler(retreat, family, links, regs, out _, out _, out _, out _, out _, out _);

        await FluentActions.Invoking(() => sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*incompleta*");
    }

    [Fact]
    public async Task ForceRecreate_false_com_GroupLink_presente_retorna_skipped_sem_publicar()
    {
        var retreat = OpenRetreat(locked: true);
        var family  = Fam(retreat.Id, "Fam A", capacity: 4); // ✓ Ajustado para 4

        var r1 = NewReg(retreat.Id, "Ana Silva",    Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Diego Rocha",  Gender.Male,   "d@mail.com");
        var r3 = NewReg(retreat.Id, "Carla Mendes", Gender.Female, "c@mail.com"); // ✓ Novo
        var r4 = NewReg(retreat.Id, "Eduardo Reis", Gender.Male,   "e@mail.com"); // ✓ Novo

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r1.Id, 0),
            Link(retreat.Id, family.Id, r2.Id, 1),
            Link(retreat.Id, family.Id, r3.Id, 2), // ✓ 3º membro
            Link(retreat.Id, family.Id, r4.Id, 3), // ✓ 4º membro
        };
        var regs = new Dictionary<Guid, Registration> { 
            [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 
        };
        
        family.SetGroup("https://chat/abc", "ext-1", "whatsapp", DateTimeOffset.UtcNow);
        var initialVersion = family.GroupVersion;

        var sut = BuildHandler(retreat, family, links, regs, out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow);

        var res = await sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, ForceRecreate:false), default);

        res.Queued.Should().BeFalse();
        res.Skipped.Should().BeTrue();
        res.Version.Should().Be(initialVersion);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        famRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sucesso_quando_retreat_locked_familia_completa_publica_evento_marca_creating_e_incrementa_versao()
    {
        var retreat = OpenRetreat(locked: true);
        var family  = Fam(retreat.Id, "Fam B", capacity: 4); // ✓ Ajustado para 4

        var r1 = NewReg(retreat.Id, "Beatriz Souza", Gender.Female, "b@mail.com");
        var r2 = NewReg(retreat.Id, "Carlos Pires",  Gender.Male,   "c@mail.com");
        var r3 = NewReg(retreat.Id, "Diana Costa",   Gender.Female, "d@mail.com"); // ✓ Novo
        var r4 = NewReg(retreat.Id, "Fabio Alves",   Gender.Male,   "f@mail.com"); // ✓ Novo

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r2.Id, 0), // ✓ Mantendo ordem original (r2 primeiro)
            Link(retreat.Id, family.Id, r1.Id, 1),
            Link(retreat.Id, family.Id, r3.Id, 2), // ✓ 3º membro
            Link(retreat.Id, family.Id, r4.Id, 3), // ✓ 4º membro
        };
        var regs = new Dictionary<Guid, Registration> { 
            [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 
        };

        var initialVersion = family.GroupVersion;

        var sut = BuildHandler(retreat, family, links, regs, out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow);

        FamilyGroupCreateRequestedV1? captured = null;
        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupCreateRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string,string,object,string?,CancellationToken>((t,s,data,tr,ct) => captured = (FamilyGroupCreateRequestedV1)data)
           .Returns(Task.CompletedTask);

        var res = await sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, ForceRecreate:false), default);
        
        captured.Should().NotBeNull();
        captured!.RetreatId.Should().Be(retreat.Id);
        captured.FamilyId.Should().Be(family.Id);
        captured.ForceRecreate.Should().BeFalse();
        captured.Members.Select(m => m.RegistrationId).Should().ContainInOrder(r2.Id, r1.Id, r3.Id, r4.Id); // ✓ Ordem atualizada
        
        family.GroupStatus.Should().Be(GroupStatus.Creating);
        family.GroupVersion.Should().Be(initialVersion + 1);
        
        famRepo.Verify(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        res.Queued.Should().BeTrue();
        res.Skipped.Should().BeFalse();
        res.Version.Should().Be(family.GroupVersion);
    }

    [Fact]
    public async Task Sucesso_quando_apenas_lock_da_familia_mesmo_sem_lock_global()
    {
        var retreat = OpenRetreat(locked: false); 
        var family  = Fam(retreat.Id, "Fam C", capacity: 4, locked: true); // ✓ Ajustado para 4

        var r1 = NewReg(retreat.Id, "Ana Silva",     Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "João Lima",     Gender.Male,   "j@mail.com");
        var r3 = NewReg(retreat.Id, "Bianca Santos", Gender.Female, "b@mail.com"); // ✓ Novo
        var r4 = NewReg(retreat.Id, "Lucas Rocha",   Gender.Male,   "l@mail.com"); // ✓ Novo

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r1.Id, 0),
            Link(retreat.Id, family.Id, r2.Id, 1),
            Link(retreat.Id, family.Id, r3.Id, 2), // ✓ 3º membro
            Link(retreat.Id, family.Id, r4.Id, 3), // ✓ 4º membro
        };
        var regs = new Dictionary<Guid, Registration> { 
            [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 
        };

        var sut = BuildHandler(retreat, family, links, regs, out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow);

        var res = await sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, ForceRecreate:false), default);

        res.Queued.Should().BeTrue();
        bus.Verify(b => b.EnqueueAsync(EventTypes.FamilyGroupCreateRequestedV1, "sam.core", It.IsAny<FamilyGroupCreateRequestedV1>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceRecreate_true_publica_evento_mesmo_com_GroupLink()
    {
        var retreat = OpenRetreat(locked: true);
        var family  = Fam(retreat.Id, "Fam D", capacity: 4); // ✓ Ajustado para 4

        var r1 = NewReg(retreat.Id, "Ana Silva",    Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Pedro Souza",  Gender.Male,   "p@mail.com");
        var r3 = NewReg(retreat.Id, "Clara Nunes",  Gender.Female, "c@mail.com"); // ✓ Novo
        var r4 = NewReg(retreat.Id, "Rafael Dias",  Gender.Male,   "r@mail.com"); // ✓ Novo

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r1.Id, 0),
            Link(retreat.Id, family.Id, r2.Id, 1),
            Link(retreat.Id, family.Id, r3.Id, 2), // ✓ 3º membro
            Link(retreat.Id, family.Id, r4.Id, 3), // ✓ 4º membro
        };
        var regs = new Dictionary<Guid, Registration> { 
            [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 
        };
        
        family.SetGroup("https://chat/exists", "ext-7", "whatsapp", DateTimeOffset.UtcNow);
        var prevVersion = family.GroupVersion;

        var sut = BuildHandler(retreat, family, links, regs, out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus, out var uow);

        FamilyGroupCreateRequestedV1? captured = null;
        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupCreateRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string,string,object,string?,CancellationToken>((t,s,data,tr,ct) => captured = (FamilyGroupCreateRequestedV1)data)
           .Returns(Task.CompletedTask);

        var res = await sut.Handle(new NotifyFamilyGroupCommand(retreat.Id, family.Id, ForceRecreate:true), default);

        captured.Should().NotBeNull();
        captured!.ForceRecreate.Should().BeTrue();

        family.GroupStatus.Should().Be(GroupStatus.Creating);
        family.GroupVersion.Should().Be(prevVersion + 1);

        famRepo.Verify(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        res.Queued.Should().BeTrue();
        res.Skipped.Should().BeFalse();
        res.Version.Should().Be(family.GroupVersion);
    }
}
