using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups;
using SAMGestor.Application.Features.Families.Groups.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.Create;

public class CreateFamilyGroupsBulkHandlerTests
{
    private static Retreat OpenRetreatLocked()
    {
        var r = new Retreat(
            new FullName("Retiro Y"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

        r.LockFamilies();
        return r;
    }

    private static Registration NewReg(Guid retreatId, string name, Gender g,
        string? email = null, string phone = "11999999999")
    {
        return new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress(email ?? $"{Guid.NewGuid():N}@example.com"),
            phone,
            new DateOnly(1990,1,1),
            g,
            "SP",
            RegistrationStatus.Confirmed,
            ParticipationCategory.Guest,
            "Oeste",
            retreatId);
    }

    private static Family NewFamily(Guid retreatId, string name, int capacity)
        => new Family(new FamilyName(name), retreatId, capacity);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    private static CreateFamilyGroupsBulkHandler BuildHandler(
        Retreat retreat,
        List<Family> families,
        Dictionary<Guid, List<FamilyMember>> linksByFamily,
        Dictionary<Guid, Registration> regsMap,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> familyRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IRegistrationRepository> regRepo,
        out Mock<IEventBus> bus,
        out Mock<IUnitOfWork> uow)
    {
        retRepo   = new Mock<IRetreatRepository>();
        familyRepo= new Mock<IFamilyRepository>();
        fmRepo    = new Mock<IFamilyMemberRepository>();
        regRepo   = new Mock<IRegistrationRepository>();
        bus       = new Mock<IEventBus>();
        uow       = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(families);

        fmRepo.Setup(f => f.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(linksByFamily);

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(regsMap);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        familyRepo.Setup(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        bus.Setup(b => b.EnqueueAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new CreateFamilyGroupsBulkHandler(
            retRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);
    }

    
    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retreatId = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        var familyRepo = new Mock<IFamilyRepository>();
        var fmRepo = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var bus = new Mock<IEventBus>();
        var uow = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var sut = new CreateFamilyGroupsBulkHandler(retRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object, bus.Object, uow.Object);

        var cmd = new CreateFamilyGroupsCommand(retreatId, DryRun: false, ForceRecreate: false);

        await FluentActions.Invoking(() => sut.Handle(cmd, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_retiro_nao_travado()
    {
        var retreat = new Retreat(
            new FullName("Retiro Z"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50)); 

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var sut = new CreateFamilyGroupsBulkHandler(
            retRepo.Object, Mock.Of<IFamilyRepository>(), Mock.Of<IFamilyMemberRepository>(),
            Mock.Of<IRegistrationRepository>(), Mock.Of<IEventBus>(), Mock.Of<IUnitOfWork>());

        var cmd = new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false);

        await FluentActions.Invoking(() => sut.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*travado*");
    }

    [Fact]
    public async Task Retorna_zero_quando_nao_ha_familias()
    {
        var retreat = OpenRetreatLocked();

        var sut = BuildHandler(
            retreat,
            families: new List<Family>(),
            linksByFamily: new Dictionary<Guid, List<FamilyMember>>(),
            regsMap: new Dictionary<Guid, Registration>(),
            out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false), default);

        res.TotalFamilies.Should().Be(0);
        res.Queued.Should().Be(0);
        res.Skipped.Should().Be(0);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        familyRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pula_familia_incompleta_quando_links_menores_que_capacity()
    {
        var retreat = OpenRetreatLocked();

        var fam = NewFamily(retreat.Id, "Fam 1", capacity: 4);
        var r1 = NewReg(retreat.Id, "Ana silva", Gender.Female, "ana@mail.com");
        var r2 = NewReg(retreat.Id, "Bea souza", Gender.Female, "bea@mail.com");
        var r3 = NewReg(retreat.Id, "Caio castro", Gender.Male,   "caio@mail.com");

        var links = new List<FamilyMember> {
            Link(retreat.Id, fam.Id, r1.Id, 0),
            Link(retreat.Id, fam.Id, r2.Id, 1),
            Link(retreat.Id, fam.Id, r3.Id, 2),
        };

        var sut = BuildHandler(
            retreat,
            families: new List<Family> { fam },
            linksByFamily: new Dictionary<Guid, List<FamilyMember>> { [fam.Id] = links },
            regsMap: new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3 },
            out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false), default);

        res.TotalFamilies.Should().Be(1);
        res.Queued.Should().Be(0);
        res.Skipped.Should().Be(1);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        familyRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DryRun_true_nao_publica_nao_salva_nem_atualiza_mas_retorna_queued_simulado()
    {
        var retreat = OpenRetreatLocked();

        var fam1 = NewFamily(retreat.Id, "Fam 1", capacity: 3);
        var fam2 = NewFamily(retreat.Id, "Fam 2", capacity: 2);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Bea Triz", Gender.Female, "b@mail.com");
        var r3 = NewReg(retreat.Id, "Caio Castro", Gender.Male,   "c@mail.com");
        var r4 = NewReg(retreat.Id, "Davi Angelo", Gender.Male,   "d@mail.com");

        var linksByFamily = new Dictionary<Guid, List<FamilyMember>> {
            [fam1.Id] = new() {
                Link(retreat.Id, fam1.Id, r1.Id, 0),
                Link(retreat.Id, fam1.Id, r2.Id, 1),
                Link(retreat.Id, fam1.Id, r3.Id, 2),
            },
            [fam2.Id] = new() {
                Link(retreat.Id, fam2.Id, r4.Id, 0),
                Link(retreat.Id, fam2.Id, r3.Id, 1),
            }
        };

        var regsMap = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 };

        var sut = BuildHandler(
            retreat,
            families: new List<Family> { fam1, fam2 },
            linksByFamily: linksByFamily,
            regsMap: regsMap,
            out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:true, ForceRecreate:false), default);

        res.TotalFamilies.Should().Be(2);
        res.Queued.Should().Be(2);  
        res.Skipped.Should().Be(0);

        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        familyRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DryRun_false_publica_evento_para_cada_familia_completa_e_chama_update_e_savechanges()
    {
        var retreat = OpenRetreatLocked();

        var fam1 = NewFamily(retreat.Id, "Fam 1", capacity: 2);
        var fam2 = NewFamily(retreat.Id, "Fam 2", capacity: 2);

        var r1 = NewReg(retreat.Id, "Ana Silva", Gender.Female, "a@mail.com");
        var r2 = NewReg(retreat.Id, "Bea Triz", Gender.Female, "b@mail.com");
        var r3 = NewReg(retreat.Id, "Caio Castro", Gender.Male,   "c@mail.com");
        var r4 = NewReg(retreat.Id, "Davi Angelo", Gender.Male,   "d@mail.com");

        var linksByFamily = new Dictionary<Guid, List<FamilyMember>> {
            [fam1.Id] = new() {
                Link(retreat.Id, fam1.Id, r1.Id, 0),
                Link(retreat.Id, fam1.Id, r2.Id, 1),
            },
            [fam2.Id] = new() {
                Link(retreat.Id, fam2.Id, r3.Id, 0),
                Link(retreat.Id, fam2.Id, r4.Id, 1),
            }
        };

        var regsMap = new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4 };

        var sut = BuildHandler(
            retreat,
            families: new List<Family> { fam1, fam2 },
            linksByFamily: linksByFamily,
            regsMap: regsMap,
            out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false), default);

        res.TotalFamilies.Should().Be(2);
        res.Queued.Should().Be(2);
        res.Skipped.Should().Be(0);

        bus.Verify(b => b.EnqueueAsync(
            EventTypes.FamilyGroupCreateRequestedV1,
            "sam.core",
            It.IsAny<FamilyGroupCreateRequestedV1>(),
            null,
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        familyRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Evento_publicado_contem_membros_ordenados_por_position_e_dados_de_contato()
    {
        var retreat = OpenRetreatLocked();

        var fam = NewFamily(retreat.Id, "Fam 1", capacity: 4);

        var r0 = NewReg(retreat.Id, "Zuleica Zu", Gender.Female, "z@mail.com", "1100000000");
        var r1 = NewReg(retreat.Id, "Ana Silva",     Gender.Female, "a@mail.com",  "1111111111");
        var r2 = NewReg(retreat.Id, "Bea Triz",     Gender.Female, "b@mail.com",          "1222222222"); // email nulo permitido
        var r3 = NewReg(retreat.Id, "Caio Castro",    Gender.Male,   "c@mail.com",  "1333333333");

        var links = new List<FamilyMember> {
            Link(retreat.Id, fam.Id, r1.Id, 1),
            Link(retreat.Id, fam.Id, r3.Id, 3),
            Link(retreat.Id, fam.Id, r2.Id, 2),
            Link(retreat.Id, fam.Id, r0.Id, 0),
        };

        var regsMap = new Dictionary<Guid, Registration> {
            [r0.Id]=r0, [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3
        };

        var sut = BuildHandler(
            retreat,
            families: new List<Family> { fam },
            linksByFamily: new Dictionary<Guid, List<FamilyMember>> { [fam.Id] = links },
            regsMap: regsMap,
            out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
        );

        FamilyGroupCreateRequestedV1? captured = null;
        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupCreateRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string,string,object,string?,CancellationToken>((t, s, data, tr, ct) =>
           {
               captured = data as FamilyGroupCreateRequestedV1;
           })
           .Returns(Task.CompletedTask);

        var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false), default);

        captured.Should().NotBeNull();
        captured!.FamilyId.Should().Be(fam.Id);
        captured.Members.Should().HaveCount(4);
        
        captured.Members[0].RegistrationId.Should().Be(r0.Id);
        captured.Members[1].RegistrationId.Should().Be(r1.Id);
        captured.Members[2].RegistrationId.Should().Be(r2.Id);
        captured.Members[3].RegistrationId.Should().Be(r3.Id);

        captured.Members[0].Name.Should().Be((string)r0.Name);
        captured.Members[1].Email.Should().Be("a@mail.com");
        captured.Members[2].Email.Should().Be("b@mail.com"); 
        captured.Members[3].PhoneE164.Should().Be("1333333333");
    }
    
    
    [Fact]
public async Task ForceRecreate_false_pula_quando_Family_tem_GroupLink()
{
    var retreat = OpenRetreatLocked();

    var fam = NewFamily(retreat.Id, "Fam 1", capacity: 2);
    var r1 = NewReg(retreat.Id, "Ana Silva",  Gender.Female, "ana@mail.com");
    var r2 = NewReg(retreat.Id, "Caio Lima",  Gender.Male,   "caio@mail.com");
    
    var links = new List<FamilyMember> {
        Link(retreat.Id, fam.Id, r1.Id, 0),
        Link(retreat.Id, fam.Id, r2.Id, 1),
    };
    
    fam.MarkGroupActive(
        link: "https://chat.whatsapp.com/abc",
        externalId: "ext-123",
        channel: "whatsapp",
        createdAt: DateTimeOffset.UtcNow,
        notifiedAt: null);

    var sut = BuildHandler(
        retreat,
        families: new List<Family> { fam },
        linksByFamily: new Dictionary<Guid, List<FamilyMember>> { [fam.Id] = links },
        regsMap: new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2 },
        out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
    );

    var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:false), default);

    res.TotalFamilies.Should().Be(1);
    res.Queued.Should().Be(0);
    res.Skipped.Should().Be(1);

    bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    familyRepo.Verify(f => f.UpdateAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
    uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once); // DryRun=false sempre salva
}

[Fact]
public async Task ForceRecreate_true_enfileira_mesmo_com_GroupLink_e_marca_Creating()
{
    var retreat = OpenRetreatLocked();

    var fam = NewFamily(retreat.Id, "Fam 1", capacity: 2);
    var r1 = NewReg(retreat.Id, "Beatriz Souza", Gender.Female, "bea@mail.com");
    var r2 = NewReg(retreat.Id, "Diego Rocha",   Gender.Male,   "diego@mail.com");

    var links = new List<FamilyMember> {
        Link(retreat.Id, fam.Id, r1.Id, 0),
        Link(retreat.Id, fam.Id, r2.Id, 1),
    };
    
    fam.MarkGroupActive(
        link: "https://chat.whatsapp.com/xyz",
        externalId: "ext-999",
        channel: "whatsapp",
        createdAt: DateTimeOffset.UtcNow.AddDays(-1),
        notifiedAt: null);

    var initialVersion = fam.GroupVersion;

    var sut = BuildHandler(
        retreat,
        families: new List<Family> { fam },
        linksByFamily: new Dictionary<Guid, List<FamilyMember>> { [fam.Id] = links },
        regsMap: new Dictionary<Guid, Registration> { [r1.Id]=r1, [r2.Id]=r2 },
        out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var bus, out var uow
    );

    var res = await sut.Handle(new CreateFamilyGroupsCommand(retreat.Id, DryRun:false, ForceRecreate:true), default);

    res.TotalFamilies.Should().Be(1);
    res.Queued.Should().Be(1);
    res.Skipped.Should().Be(0);

    bus.Verify(b => b.EnqueueAsync(
        EventTypes.FamilyGroupCreateRequestedV1,
        "sam.core",
        It.IsAny<FamilyGroupCreateRequestedV1>(),
        null,
        It.IsAny<CancellationToken>()), Times.Once);

    familyRepo.Verify(f => f.UpdateAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
    uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

    fam.GroupStatus.Should().Be(GroupStatus.Creating);
    fam.GroupVersion.Should().Be(initialVersion + 1);
}

}
