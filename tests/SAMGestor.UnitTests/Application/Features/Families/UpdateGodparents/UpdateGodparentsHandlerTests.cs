using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.UpdateGodparents;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.UpdateGodparents;

public class UpdateGodparentsHandlerTests
{
    private static Retreat NewRetreat(bool locked = false)
    {
        var r = new Retreat(
            new FullName("Retiro X"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            10, 10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));
        
        if (locked) r.LockFamilies();
        return r;
    }

    private static Family NewFamily(Guid retreatId, string name = "Família 1", bool locked = false)
    {
        var color = FamilyColor.FromName("Azul");
        var f = new Family(new FamilyName(name), retreatId, 4, color);
        if (locked) f.Lock();
        return f;
    }

    private static Registration Reg(Guid retreatId, string name, Gender g)
        => new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress($"{Guid.NewGuid():N}@mail.com"),
            "11999999999",
            new DateOnly(1990, 1, 1),
            g,
            "Cidade",
            RegistrationStatus.Confirmed,
            retreatId);

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos)
        => new FamilyMember(retreatId, familyId, regId, pos);

    // ===== TESTES DE VALIDAÇÃO BÁSICA =====

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(Guid.NewGuid(), Guid.NewGuid(), null, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = NewRetreat(locked: true);
        
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, Guid.NewGuid(), null, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_existe()
    {
        var retreat = NewRetreat();
        
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Family?)null);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, Guid.NewGuid(), null, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task Falha_quando_familia_de_outro_retiro()
    {
        var retreat = NewRetreat();
        var otherRetreat = NewRetreat();
        var fam = NewFamily(otherRetreat.Id);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, null, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }

    [Fact]
    public async Task Falha_quando_familia_lockada()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, locked: true);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, null, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueada*");
    }

    [Fact]
    public async Task Falha_quando_mais_de_2_padrinhos()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(
            retreat.Id,
            fam.Id,
            PadrinhoIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }, // 3 padrinhos
            MadrinhaIds: null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*mais de 2 padrinhos*");
    }

    [Fact]
    public async Task Falha_quando_mais_de_2_madrinhas()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(
            retreat.Id,
            fam.Id,
            PadrinhoIds: null,
            MadrinhaIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }); // 3 madrinhas

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*mais de 2 madrinhas*");
    }

    // ===== TESTES DE VALIDAÇÃO DE PADRINHOS =====

    [Fact]
    public async Task Falha_quando_padrinho_nao_esta_na_familia()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Maria Lima", Gender.Female);
        var outsider = Guid.NewGuid();

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>
              {
                  Link(retreat.Id, fam.Id, r1.Id, 0),
                  Link(retreat.Id, fam.Id, r2.Id, 1)
              });

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            fmRepo.Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, new List<Guid> { outsider }, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertencem a esta família*");
    }

    [Fact]
    public async Task Falha_quando_overlap_padrinho_e_madrinha()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { Link(retreat.Id, fam.Id, r1.Id, 0) });

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            fmRepo.Object,
            new Mock<IRegistrationRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(
            retreat.Id,
            fam.Id,
            PadrinhoIds: new List<Guid> { r1.Id },
            MadrinhaIds: new List<Guid> { r1.Id }); // Mesmo ID em ambas listas

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pode ser padrinho E madrinha*");
    }

    [Fact]
    public async Task Falha_quando_padrinho_nao_e_masculino()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Maria Lima", Gender.Female);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>
              {
                  Link(retreat.Id, fam.Id, r1.Id, 0),
                  Link(retreat.Id, fam.Id, r2.Id, 1)
              });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r2.Id] = r2
               });

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, new List<Guid> { r2.Id }, null); // r2 é mulher!

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*gênero masculino*");
    }

    [Fact]
    public async Task Falha_quando_madrinha_nao_e_feminina()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Maria Lima", Gender.Female);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>
              {
                  Link(retreat.Id, fam.Id, r1.Id, 0),
                  Link(retreat.Id, fam.Id, r2.Id, 1)
              });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1
               });

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, new Mock<IUnitOfWork>().Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, null, new List<Guid> { r1.Id }); // r1 é homem!

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*gênero feminino*");
    }

    // ===== TESTES DE SUCESSO COM WARNINGS =====

    [Fact]
    public async Task Sucesso_atualiza_apenas_padrinhos_com_warning()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Pedro Lima", Gender.Male);
        var r3 = Reg(retreat.Id, "Maria Santos", Gender.Female);
        var r4 = Reg(retreat.Id, "Ana Costa", Gender.Female);

        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var fm2 = Link(retreat.Id, fam.Id, r2.Id, 1);
        var fm3 = Link(retreat.Id, fam.Id, r3.Id, 2);
        var fm4 = Link(retreat.Id, fam.Id, r4.Id, 3);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1, fm2, fm3, fm4 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1,
                   [r2.Id] = r2
               });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var versionBefore = retreat.FamiliesVersion;

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, new List<Guid> { r1.Id, r2.Id }, null);
        var res = await handler.Handle(cmd, default);

        res.Success.Should().BeTrue();
        res.Version.Should().Be(versionBefore + 1);
        res.Warnings.Should().Contain(w => w.Contains("madrinha")); // Warning sobre falta de madrinhas
        
        fm1.IsPadrinho.Should().BeTrue();
        fm2.IsPadrinho.Should().BeTrue();
        fm3.IsPadrinho.Should().BeFalse();
        fm4.IsPadrinho.Should().BeFalse();

        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_atualiza_apenas_madrinhas_com_warning()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Pedro Lima", Gender.Male);
        var r3 = Reg(retreat.Id, "Maria Santos", Gender.Female);
        var r4 = Reg(retreat.Id, "Ana Costa", Gender.Female);

        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var fm2 = Link(retreat.Id, fam.Id, r2.Id, 1);
        var fm3 = Link(retreat.Id, fam.Id, r3.Id, 2);
        var fm4 = Link(retreat.Id, fam.Id, r4.Id, 3);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1, fm2, fm3, fm4 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r3.Id] = r3,
                   [r4.Id] = r4
               });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, null, new List<Guid> { r3.Id, r4.Id });
        var res = await handler.Handle(cmd, default);

        res.Success.Should().BeTrue();
        res.Warnings.Should().Contain(w => w.Contains("padrinho")); // Warning sobre falta de padrinhos
        
        fm3.IsMadrinha.Should().BeTrue();
        fm4.IsMadrinha.Should().BeTrue();

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_atualiza_padrinhos_e_madrinhas_sem_warnings()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Pedro Lima", Gender.Male);
        var r3 = Reg(retreat.Id, "Maria Santos", Gender.Female);
        var r4 = Reg(retreat.Id, "Ana Costa", Gender.Female);

        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var fm2 = Link(retreat.Id, fam.Id, r2.Id, 1);
        var fm3 = Link(retreat.Id, fam.Id, r3.Id, 2);
        var fm4 = Link(retreat.Id, fam.Id, r4.Id, 3);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1, fm2, fm3, fm4 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1,
                   [r2.Id] = r2,
                   [r3.Id] = r3,
                   [r4.Id] = r4
               });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        var versionBefore = retreat.FamiliesVersion;

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, new List<Guid> { r1.Id, r2.Id }, new List<Guid> { r3.Id, r4.Id });
        var res = await handler.Handle(cmd, default);

        res.Success.Should().BeTrue();
        res.Version.Should().Be(versionBefore + 1);
        res.Warnings.Should().BeEmpty(); // Sem warnings pois tem 2 de cada
        
        fm1.IsPadrinho.Should().BeTrue();
        fm2.IsPadrinho.Should().BeTrue();
        fm3.IsMadrinha.Should().BeTrue();
        fm4.IsMadrinha.Should().BeTrue();

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_limpa_padrinhos_anteriores()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Pedro Lima", Gender.Male);

        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var fm2 = Link(retreat.Id, fam.Id, r2.Id, 1);
        
        // Marcar ambos como padrinhos inicialmente
        fm1.MarkAsPadrinho();
        fm2.MarkAsPadrinho();

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1, fm2 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        // Atualizar para ter apenas r1 como padrinho
        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, new List<Guid> { r1.Id }, null);
        await handler.Handle(cmd, default);

        fm1.IsPadrinho.Should().BeTrue();
        fm2.IsPadrinho.Should().BeFalse(); // Deve ser desmarcado!
    }

    [Fact]
    public async Task Sucesso_aceita_listas_vazias_com_warning()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(
            retRepo.Object,
            famRepo.Object,
            fmRepo.Object,
            new Mock<IRegistrationRepository>().Object,
            uow.Object);

        var cmd = new UpdateGodparentsCommand(retreat.Id, fam.Id, null, null);
        var res = await handler.Handle(cmd, default);

        res.Success.Should().BeTrue();
        res.Warnings.Should().Contain(w => w.Contains("não possui padrinhos nem madrinhas"));
        
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_remove_duplicados_automaticamente()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var r1 = Reg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = Reg(retreat.Id, "Maria Lima", Gender.Female);

        var fm1 = Link(retreat.Id, fam.Id, r1.Id, 0);
        var fm2 = Link(retreat.Id, fam.Id, r2.Id, 1);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var famRepo = new Mock<IFamilyRepository>();
        famRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(fam);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { fm1, fm2 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [r1.Id] = r1,
                   [r2.Id] = r2
               });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new UpdateGodparentsHandler(retRepo.Object, famRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);

        // Passar IDs duplicados
        var cmd = new UpdateGodparentsCommand(
            retreat.Id,
            fam.Id,
            PadrinhoIds: new List<Guid> { r1.Id, r1.Id, r1.Id }, // Duplicado
            MadrinhaIds: new List<Guid> { r2.Id, r2.Id }); // Duplicado

        var res = await handler.Handle(cmd, default);

        res.Success.Should().BeTrue();
        
        // Deve ter tratado como 1 padrinho e 1 madrinha apenas
        fm1.IsPadrinho.Should().BeTrue();
        fm2.IsMadrinha.Should().BeTrue();
    }
}
