using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups.Resend;
using SAMGestor.Application.Interfaces;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.Resend;

public class ResendFamilyGroupHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro Reenvio"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    private static Family Fam(Guid retreatId, string name, int capacity = 4)
        => new Family(new FamilyName(name), retreatId, capacity);

    private static Registration NewReg(Guid retreatId, string name, Gender g,
        string? email = null, string phone = "11999999999")
        => new Registration(
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

    private static ResendFamilyGroupHandler BuildHandler(
        Retreat retreat,
        Family family,
        List<FamilyMember> links,
        Dictionary<Guid, Registration> regsMap,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> famRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IRegistrationRepository> regRepo,
        out Mock<IEventBus> bus)
    {
        retRepo = new Mock<IRetreatRepository>();
        famRepo = new Mock<IFamilyRepository>();
        fmRepo  = new Mock<IFamilyMemberRepository>();
        regRepo = new Mock<IRegistrationRepository>();
        bus     = new Mock<IEventBus>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        famRepo.Setup(r => r.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(family);

        fmRepo.Setup(r => r.ListByFamilyAsync(family.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(links);

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(regsMap);

        bus.Setup(b => b.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new ResendFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object);
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

        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var sut = new ResendFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object);

        await FluentActions.Invoking(() => sut.Handle(new ResendFamilyGroupCommand(retreatId, familyId), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_existe()
    {
        var retreat = OpenRetreat();
        var familyId = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();
        var fmRepo  = new Mock<IFamilyMemberRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var bus     = new Mock<IEventBus>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        famRepo.Setup(r => r.GetByIdAsync(familyId, It.IsAny<CancellationToken>()))
               .ReturnsAsync((Family?)null);

        var sut = new ResendFamilyGroupHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, bus.Object);

        await FluentActions.Invoking(() => sut.Handle(new ResendFamilyGroupCommand(retreat.Id, familyId), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task Falha_quando_familia_de_outro_retiro()
    {
        var retreat = OpenRetreat();
        var other   = OpenRetreat();
        var family  = Fam(other.Id, "Fam X");

        var sut = BuildHandler(
            retreat,
            family,
            links: new List<FamilyMember>(),
            regsMap: new Dictionary<Guid, Registration>(),
            out _, out _, out _, out _, out _
        );

        await FluentActions.Invoking(() => sut.Handle(new ResendFamilyGroupCommand(retreat.Id, family.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }

    [Fact]
    public async Task Retorna_reason_NO_GROUP_LINK_quando_nao_tem_link()
    {
        var retreat = OpenRetreat();
        var family  = Fam(retreat.Id, "Fam Sem Link");

        var sut = BuildHandler(
            retreat,
            family,
            links: new List<FamilyMember> { }, 
            regsMap: new Dictionary<Guid, Registration>(),
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus
        );

        var res = await sut.Handle(new ResendFamilyGroupCommand(retreat.Id, family.Id), default);

        res.Queued.Should().BeFalse();
        res.Reason.Should().Be("NO_GROUP_LINK");
        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Retorna_reason_NO_MEMBERS_quando_sem_membros_vinculados()
    {
        var retreat = OpenRetreat();
        var family  = Fam(retreat.Id, "Fam Sem Membros");
        family.SetGroup("https://chat/exist", "ext", "whatsapp", DateTimeOffset.UtcNow); // tem link

        var sut = BuildHandler(
            retreat,
            family,
            links: new List<FamilyMember>(),
            regsMap: new Dictionary<Guid, Registration>(),
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus
        );

        var res = await sut.Handle(new ResendFamilyGroupCommand(retreat.Id, family.Id), default);

        res.Queued.Should().BeFalse();
        res.Reason.Should().Be("NO_MEMBERS");
        bus.Verify(b => b.EnqueueAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sucesso_publica_evento_notify_com_membros_ordenados_e_groupLink_da_familia()
    {
        var retreat = OpenRetreat();
        var family  = Fam(retreat.Id, "Fam OK", capacity: 3);
        family.SetGroup("https://chat/ok", "ext-1", "whatsapp", DateTimeOffset.UtcNow);

        var r1 = NewReg(retreat.Id, "Ana Silva",  Gender.Female, "a@mail.com", "1111111111");
        var r2 = NewReg(retreat.Id, "Bruno Lima", Gender.Male,   "b@mail.com",         "2222222222"); // email nulo permitido
        var r3 = NewReg(retreat.Id, "Carla Pires",Gender.Female, "c@mail.com", "3333333333");

        var links = new List<FamilyMember> {
            Link(retreat.Id, family.Id, r2.Id, 2),
            Link(retreat.Id, family.Id, r1.Id, 0),
            Link(retreat.Id, family.Id, r3.Id, 1),
        };

        var regsMap = new Dictionary<Guid, Registration> {
            [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3
        };

        var sut = BuildHandler(
            retreat, family, links, regsMap,
            out var retRepo, out var famRepo, out var fmRepo, out var regRepo, out var bus
        );

        FamilyGroupNotifyRequestedV1? captured = null;
        bus.Setup(b => b.EnqueueAsync(
                EventTypes.FamilyGroupNotifyRequestedV1,
                "sam.core",
                It.IsAny<object>(),
                null,
                It.IsAny<CancellationToken>()))
           .Callback<string,string,object,string?,CancellationToken>((t,s,data,tr,ct) => captured = (FamilyGroupNotifyRequestedV1)data)
           .Returns(Task.CompletedTask);

        var res = await sut.Handle(new ResendFamilyGroupCommand(retreat.Id, family.Id), default);
        
        res.Queued.Should().BeTrue();
        res.Reason.Should().BeNull();
        
        captured.Should().NotBeNull();
        captured!.RetreatId.Should().Be(retreat.Id);
        captured.FamilyId.Should().Be(family.Id);
        captured.GroupLink.Should().Be("https://chat/ok");

        captured.Members.Select(m => m.RegistrationId).Should()
            .ContainInOrder(r1.Id, r3.Id, r2.Id); 

        captured.Members[0].Name.Should().Be((string)r1.Name);
        captured.Members[1].Email.Should().Be("c@mail.com");
        captured.Members[2].Email.Should().Be("b@mail.com");
    }
}
