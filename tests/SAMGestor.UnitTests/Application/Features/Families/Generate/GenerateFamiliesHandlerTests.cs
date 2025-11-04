using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Generate;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.Generate;

public class GenerateFamiliesHandlerTests
{
    
    private static Retreat NewRetreat(bool locked = false)
    {
        var r = new Retreat(
            new FullName("Retiro X"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            100, 100,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"), new Money(0, "BRL"),
            new Percentage(50), new Percentage(50)
        );
        if (locked) r.LockFamilies();
        return r;
    }
    
    private static readonly string[] _validCpfs =
    {
        "52998224725", "11144477735", "15350946056", "93541134780",
        "35719260006", "28625172040", "39053344705", "84512725004",
        "74697131401", "07748231005", "73008130040", "91234567809",
        "70433344007", "05353313030", "01394288007" 
    };
    private static int _cpfIdx = 0;

    private static string NextCpf()
    {
        var cpf = _validCpfs[_cpfIdx % _validCpfs.Length];
        _cpfIdx++;
        return cpf;
    }
    
    private static Registration Reg(
        Guid retreatId,
        string name,
        Gender g,
        RegistrationStatus status = RegistrationStatus.Confirmed,
        bool enabled = true)
        => new Registration(
            new FullName(name),
            new CPF(NextCpf()), 
            new EmailAddress($"{Guid.NewGuid():N}@x.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            "Cidade",
            status,
            retreatId
        );
    
    private static Family Fam(Guid retreatId, string name = "Família X", int capacity = 4, bool locked = false)
    {
        var f = new Family(new FamilyName(name), retreatId, capacity);
        if (locked)
        {
            var prop = typeof(Family).GetProperty("IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            prop?.SetValue(f, true);
        }
        return f;
    }

    private static GenerateFamiliesHandler Build(
        Retreat retreat,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IRegistrationRepository> regRepo,
        out Mock<IFamilyRepository> famRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IUnitOfWork> uow)
    {
        retRepo = new Mock<IRetreatRepository>();
        regRepo = new Mock<IRegistrationRepository>();
        famRepo = new Mock<IFamilyRepository>();
        fmRepo  = new Mock<IFamilyMemberRepository>();
        uow     = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Family>());

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Registration>());
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Registration>());

        fmRepo.Setup(m => m.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<FamilyMember>());
        fmRepo.Setup(m => m.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>());

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new GenerateFamiliesHandler(retRepo.Object, regRepo.Object, famRepo.Object, fmRepo.Object, uow.Object);
    }
    
    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new GenerateFamiliesHandler(
            retRepo.Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(Guid.NewGuid(), Capacity: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = NewRetreat(locked: true);
        var handler = Build(retreat, out _, out _, out _, out _, out _);

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Falha_quando_capacity_invalido()
    {
        var retreat = NewRetreat();
        var handler = Build(retreat, out _, out _, out _, out _, out _);

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 0, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Capacity*");
    }

    [Fact]
    public async Task ReplaceExisting_true_e_existe_familia_lockada__falha()
    {
        var retreat = NewRetreat();

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

        famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { Fam(retreat.Id, locked: true) });

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Desbloqueie*");
    }

    [Fact]
    public async Task ReplaceExisting_true_sem_locks__apaga_tudo_e_cria_novas()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out var uow);
        
        famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Family>());

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, f1 });
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m2, f2 });

        fmRepo.Setup(m => m.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        famRepo.Setup(f => f.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        fmRepo.Setup(m => m.AddAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Families.Should().HaveCount(1);               
        famRepo.Verify(f => f.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        fmRepo.Verify(m => m.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        famRepo.Verify(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Once);
        fmRepo.Verify(m => m.AddAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()), Times.AtLeast(4));
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceExisting_false_exclui_ja_alocados_do_pool()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var fam = Fam(retreat.Id, "Família 1");

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

        famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { fam });
        
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, f1 });
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m2, f2 });
        
        fmRepo.Setup(m => m.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new[]
              {
                  new FamilyMember(retreat.Id, fam.Id, m1.Id, 0)
              });

        famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);
        fmRepo.Setup(m => m.AddAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: false, FillExistingFirst: false), default);
        
        res.Families.Should().BeEmpty();
    }

    [Fact]
public async Task FillExistingFirst_completa_somente_nao_travadas_e_respeita_posicoes_livres()
{
    var retreat = NewRetreat();

    var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
    var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
    var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
    var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

    var famLocked    = Fam(retreat.Id, "Família 1", locked: true);
    var famUnlocked  = Fam(retreat.Id, "Família 2", locked: false);

    var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

    famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new[] { famLocked, famUnlocked });

    // pool = 2M + 2F
    regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new[] { m1, f1 });
    regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
           .ReturnsAsync(new[] { m2, f2 });
    
    var occMale   = Reg(retreat.Id, "Mario A", Gender.Male);
    var occFemale = Reg(retreat.Id, "Paula B", Gender.Female);
    
    var existingLinks = new Dictionary<Guid, List<FamilyMember>>
    {
        [famUnlocked.Id] = new()
        {
            new FamilyMember(retreat.Id, famUnlocked.Id, occMale.Id,   position: 0),
            new FamilyMember(retreat.Id, famUnlocked.Id, occFemale.Id, position: 2)
        },
        [famLocked.Id] = new()
    };

    fmRepo.Setup(m => m.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
          .ReturnsAsync(existingLinks.Values.SelectMany(x => x).ToList());

    fmRepo.Setup(m => m.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(existingLinks);
    
    var existingRegMap = new Dictionary<Guid, Registration>
    {
        [occMale.Id] = occMale,
        [occFemale.Id] = occFemale
    };
    regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
               ids.Distinct().ToDictionary(id => id, id =>
                   existingRegMap.TryGetValue(id, out var reg) ? reg
                   // fallback: se algum id do pool cair aqui, devolve o próprio do pool
                   : new[] { m1, m2, f1, f2 }.First(x => x.Id == id)
               ));

    // AddAsync deve ser chamado apenas para completar a família DESBLOQUEADA,
    // e deve usar os "slots" livres (1 e 3)
    var addedPositions = new List<int>();
    fmRepo.Setup(m => m.AddAsync(It.IsAny<FamilyMember>(), It.IsAny<CancellationToken>()))
          .Callback<FamilyMember, CancellationToken>((fm, _) =>
          {
              if (fm.FamilyId == famUnlocked.Id) addedPositions.Add(fm.Position);
          })
          .Returns(Task.CompletedTask);

    famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

    var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: false, FillExistingFirst: true), default);

    addedPositions.Should().OnlyContain(p => p == 1 || p == 3);
    addedPositions.Should().NotBeEmpty();
}


    [Fact]
    public async Task Pool_vazio__retorna_lista_vazia()
    {
        var retreat = NewRetreat();
        var handler = Build(retreat, out _, out _, out _, out _, out _);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, Capacity: 4, ReplaceExisting: false, FillExistingFirst: true), default);

        res.Families.Should().BeEmpty();
    }
}
