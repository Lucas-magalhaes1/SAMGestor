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
        "70433344007", "05353313030", "01394288007", "62345678901",
        "72345678902", "82345678903", "92345678904", "12345678905"
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
        bool enabled = true,
        string city = "Cidade")
        => new Registration(
            new FullName(name),
            new CPF(NextCpf()), 
            new EmailAddress($"{Guid.NewGuid():N}@x.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            city,
            status,
            retreatId
        );
    
    private static Family Fam(Guid retreatId, string name = "Família X", int capacity = 4, string colorName = "Azul", bool locked = false)
    {
        var color = FamilyColor.FromName(colorName);
        var f = new Family(new FamilyName(name), retreatId, capacity, color);
        if (locked) f.Lock();
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

        famRepo.Setup(f => f.GetUsedColorsInRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<string>());

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Registration>());
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(Array.Empty<Registration>());

        fmRepo.Setup(m => m.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<FamilyMember>());
        fmRepo.Setup(m => m.ListByFamilyIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Dictionary<Guid, List<FamilyMember>>());
        fmRepo.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new GenerateFamiliesHandler(retRepo.Object, regRepo.Object, famRepo.Object, fmRepo.Object, uow.Object);
    }
    
    // ===== TESTES DE VALIDAÇÃO BÁSICA =====

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
            handler.Handle(new GenerateFamiliesCommand(Guid.NewGuid(), MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = NewRetreat(locked: true);
        var handler = Build(retreat, out _, out _, out _, out _, out _);

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task ReplaceExisting_true_e_existe_familia_lockada__falha()
    {
        var retreat = NewRetreat();

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

        famRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { Fam(retreat.Id, locked: true) });

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Desbloqueie*");
    }

    // ===== TESTES DE CORES =====

    [Fact]
    public async Task Falha_quando_nao_ha_cores_suficientes()
    {
        var retreat = NewRetreat();

        // Criar 8 pessoas (2 famílias de 4)
        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var m3 = Reg(retreat.Id, "Carlos Lima", Gender.Male);
        var m4 = Reg(retreat.Id, "Jose Costa", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);
        var f3 = Reg(retreat.Id, "Clara Souza", Gender.Female);
        var f4 = Reg(retreat.Id, "Diana Silva", Gender.Female);

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, m3, m4, f1, f2, f3, f4 });

        // Simular que já existem 24 famílias (todas as 25 cores - 1)
        var usedColors = FamilyColor.AvailableColors.Take(24).Select(c => c.Name).ToList();
        famRepo.Setup(f => f.GetUsedColorsInRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(usedColors);

        await FluentActions.Invoking(() =>
            handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: false, FillExistingFirst: false), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*cores suficientes*");
    }

    [Fact]
    public async Task Atribui_cores_unicas_automaticamente()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out var retRepo, out var regRepo, out var famRepo, out var fmRepo, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, f1 });
        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.PaymentConfirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m2, f2 });

        var createdFamilies = new List<Family>();
        famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Callback<Family, CancellationToken>((fam, _) => createdFamilies.Add(fam))
               .Returns(Task.CompletedTask);

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        createdFamilies.Should().HaveCount(1);
        createdFamilies[0].Color.Should().NotBeNull();
        FamilyColor.AvailableColors.Should().Contain(c => c.Name == createdFamilies[0].Color.Name);
    }

    // ===== TESTES DE GERAÇÃO =====

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

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        fmRepo.Setup(m => m.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        famRepo.Setup(f => f.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Version.Should().Be(retreat.FamiliesVersion);
        res.Families.Should().HaveCount(1);
        res.Families[0].TotalMembers.Should().Be(4);
        res.Families[0].ColorName.Should().NotBeNullOrEmpty();
        res.Families[0].ColorHex.Should().NotBeNullOrEmpty();
        
        famRepo.Verify(f => f.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        fmRepo.Verify(m => m.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        famRepo.Verify(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Once);
        fmRepo.Verify(m => m.AddRangeAsync(It.Is<IEnumerable<FamilyMember>>(x => x.Count() == 4), It.IsAny<CancellationToken>()), Times.Once);
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
    
    regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken ct) =>
           {
               var allRegs = new[] { m1, m2, f1, f2 };
               return allRegs.Where(reg => ids.Contains(reg.Id))
                            .ToDictionary(reg => reg.Id);
           });

    var res = await handler.Handle(
        new GenerateFamiliesCommand(
            retreat.Id, 
            MembersPerFamily: 4, 
            ReplaceExisting: false, 
            FillExistingFirst: false), 
        default);
    
  
    
    res.Families.Should().HaveCount(1);
    res.Families[0].Name.Should().Be("Família 2"); 
    res.Families[0].Capacity.Should().Be(4);
    res.Families[0].TotalMembers.Should().Be(3);
    res.Families[0].Remaining.Should().Be(1);
    res.Families[0].MaleCount.Should().Be(1); // m2
    res.Families[0].FemaleCount.Should().Be(2); // f1, f2
    res.Families[0].Members.Should().HaveCount(3);
    res.Families[0].Members.Should().Contain(m => m.RegistrationId == m2.Id);
    res.Families[0].Members.Should().Contain(m => m.RegistrationId == f1.Id);
    res.Families[0].Members.Should().Contain(m => m.RegistrationId == f2.Id);
    
    res.Families[0].Alerts.Should().Contain(a => a.Code == "DIFFERENT_FAMILY_SIZE");
    res.Families[0].Alerts.Should().Contain(a => a.Code == "MISSING_GODPARENTS");
}


    [Fact]
    public async Task Pool_vazio__retorna_lista_vazia()
    {
        var retreat = NewRetreat();
        var handler = Build(retreat, out _, out _, out _, out _, out _);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: false, FillExistingFirst: true), default);

        res.Families.Should().BeEmpty();
    }

    [Fact]
    public async Task Cria_familias_com_nomes_sequenciais()
    {
        var retreat = NewRetreat();

        // 8 pessoas = 2 famílias
        var pool = new List<Registration>
        {
            Reg(retreat.Id, "Joao Silva", Gender.Male),
            Reg(retreat.Id, "Pedro Souza", Gender.Male),
            Reg(retreat.Id, "Carlos Lima", Gender.Male),
            Reg(retreat.Id, "Jose Costa", Gender.Male),
            Reg(retreat.Id, "Ana Lima", Gender.Female),
            Reg(retreat.Id, "Bia Costa", Gender.Female),
            Reg(retreat.Id, "Clara Souza", Gender.Female),
            Reg(retreat.Id, "Diana Silva", Gender.Female)
        };

        var handler = Build(retreat, out _, out var regRepo, out var famRepo, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(pool);

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pool.ToDictionary(r => r.Id, r => r));

        var createdFamilies = new List<Family>();
        famRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
               .Callback<Family, CancellationToken>((fam, _) => createdFamilies.Add(fam))
               .Returns(Task.CompletedTask);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Families.Should().HaveCount(2);
        createdFamilies.Select(f => (string)f.Name).Should().Contain(new[] { "Família 1", "Família 2" });
    }

    [Fact]
    public async Task Padrinhos_e_madrinhas_sao_criados_vazios()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out _, out var regRepo, out _, out var fmRepo, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, f1, f2 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        var addedMembers = new List<FamilyMember>();
        fmRepo.Setup(m => m.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>()))
              .Callback<IEnumerable<FamilyMember>, CancellationToken>((members, _) => addedMembers.AddRange(members))
              .Returns(Task.CompletedTask);

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        addedMembers.Should().HaveCount(4);
        addedMembers.Should().AllSatisfy(m =>
        {
            m.IsPadrinho.Should().BeFalse();
            m.IsMadrinha.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Resposta_inclui_email_e_telefone_dos_membros()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out _, out var regRepo, out _, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, f1, f2 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Families.Should().HaveCount(1);
        var family = res.Families[0];
        
        family.Members.Should().AllSatisfy(m =>
        {
            m.Email.Should().NotBeNullOrEmpty();
            m.Phone.Should().NotBeNullOrEmpty();
            m.IsPadrinho.Should().BeFalse();
            m.IsMadrinha.Should().BeFalse();
        });
    }

    [Fact]
    public async Task Resposta_inclui_percentuais_de_genero()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out _, out var regRepo, out _, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, f1, f2 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Families.Should().HaveCount(1);
        var family = res.Families[0];
        
        family.MaleCount.Should().Be(2);
        family.FemaleCount.Should().Be(2);
        family.TotalMembers.Should().Be(4);
        family.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task Gera_alertas_usando_calculator_compartilhado()
    {
        var retreat = NewRetreat();

        // 2 pessoas da mesma cidade
        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male, city: "Recife");
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male, city: "Recife");
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female, city: "Olinda");
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female, city: "Caruaru");

        var handler = Build(retreat, out _, out var regRepo, out _, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, f1, f2 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [f1.Id] = f1, [f2.Id] = f2
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Families.Should().HaveCount(1);
        var family = res.Families[0];
        
        family.Alerts.Should().NotBeEmpty();
        family.Alerts.Should().Contain(a => a.Code == "SAME_CITY");
        family.Alerts.Should().Contain(a => a.Code == "MISSING_GODPARENTS");
    }

    [Fact]
    public async Task Distribui_sobras_quando_nao_divide_exatamente()
    {
        var retreat = NewRetreat();

        // 5 pessoas para famílias de 4 = 1 família completa + 1 desalocada
        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Souza", Gender.Male);
        var m3 = Reg(retreat.Id, "Carlos Lima", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Lima", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Costa", Gender.Female);

        var handler = Build(retreat, out _, out var regRepo, out _, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, m3, f1, f2 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [m3.Id] = m3, [f1.Id] = f1, [f2.Id] = f2
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        // Deve criar 2 famílias (ceiling(5/4) = 2)
        res.Families.Should().HaveCount(2);
        
        // Uma família terá 3 membros e outra 2 (ou 4 e 1 dependendo do algoritmo)
        var totalMembers = res.Families.Sum(f => f.TotalMembers);
        totalMembers.Should().Be(5);
    }

    [Fact]
    public async Task Evita_alocar_mesmo_sobrenome_na_mesma_familia()
    {
        var retreat = NewRetreat();

        var m1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var m2 = Reg(retreat.Id, "Pedro Silva", Gender.Male); // Mesmo sobrenome
        var m3 = Reg(retreat.Id, "Carlos Lima", Gender.Male);
        var m4 = Reg(retreat.Id, "Jose Costa", Gender.Male);
        var f1 = Reg(retreat.Id, "Ana Souza", Gender.Female);
        var f2 = Reg(retreat.Id, "Bia Rocha", Gender.Female);
        var f3 = Reg(retreat.Id, "Clara Mendes", Gender.Female);
        var f4 = Reg(retreat.Id, "Diana Alves", Gender.Female);

        var handler = Build(retreat, out _, out var regRepo, out _, out _, out _);

        regRepo.Setup(r => r.ListAsync(retreat.Id, nameof(RegistrationStatus.Confirmed), null, 0, int.MaxValue, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new[] { m1, m2, m3, m4, f1, f2, f3, f4 });

        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [m1.Id] = m1, [m2.Id] = m2, [m3.Id] = m3, [m4.Id] = m4,
                   [f1.Id] = f1, [f2.Id] = f2, [f3.Id] = f3, [f4.Id] = f4
               });

        var res = await handler.Handle(new GenerateFamiliesCommand(retreat.Id, MembersPerFamily: 4, ReplaceExisting: true, FillExistingFirst: false), default);

        res.Families.Should().HaveCount(2);

        // Verificar que João Silva e Pedro Silva NÃO estão na mesma família
        foreach (var family in res.Families)
        {
            var silvaMembers = family.Members.Where(m => m.Name.Contains("Silva")).ToList();
            silvaMembers.Should().HaveCountLessThan(2, "algoritmo deve evitar sobrenomes repetidos");
        }
    }
}
