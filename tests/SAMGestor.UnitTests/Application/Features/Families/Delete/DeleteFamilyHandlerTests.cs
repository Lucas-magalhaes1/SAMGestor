using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Delete;

public class DeleteFamilyHandlerTests
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

    private static Family NewFamily(Guid retreatId, string name = "Família 1", string colorName = "Azul", int capacity = 4, bool locked = false)
    {
        var color = FamilyColor.FromName(colorName);
        var f = new Family(new FamilyName(name), retreatId, capacity, color);
        if (locked) f.Lock();
        return f;
    }

    private static DeleteFamilyHandler BuildHandler(
        Retreat retreat,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> familyRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IUnitOfWork> uow)
    {
        retRepo = new Mock<IRetreatRepository>();
        familyRepo = new Mock<IFamilyRepository>();
        fmRepo = new Mock<IFamilyMemberRepository>();
        uow = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        familyRepo.Setup(f => f.DeleteAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        // ✅ ADICIONAR MOCK FALTANTE
        fmRepo.Setup(f => f.ListByFamilyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        fmRepo.Setup(f => f.RemoveByFamilyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new DeleteFamilyHandler(retRepo.Object, familyRepo.Object, fmRepo.Object, uow.Object);
    }

    // ===== TESTES DE VALIDAÇÃO BÁSICA =====

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new DeleteFamilyHandler(
            retRepo.Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(Guid.NewGuid(), Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = NewRetreat(locked: true);

        var handler = BuildHandler(retreat, out _, out _, out _, out _);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, Guid.NewGuid()), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_existe()
    {
        var retreat = NewRetreat();
        var handler = BuildHandler(retreat, out _, out var familyRepo, out _, out _);

        familyRepo.Setup(f => f.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Family?)null);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task Falha_quando_familia_de_outro_retiro()
    {
        var retreat = NewRetreat();
        var other = NewRetreat();

        var fam = NewFamily(other.Id);

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _, out _);
        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }

    [Fact]
    public async Task Falha_quando_familia_lockada()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, locked: true);

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _, out _);
        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueada*");
    }

    // ===== TESTES DE SUCESSO =====

    [Fact]
    public async Task Sucesso_deleta_familia_bumpa_versao_e_persiste()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família 1", "Azul");

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out var fmRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var before = retreat.FamiliesVersion;

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);
        
        // ✅ AJUSTADO: Agora retorna DeleteFamilyResponse
        res.Should().NotBeNull();
        res.Version.Should().Be(before + 1);
        res.FamilyName.Should().Be("Família 1");
        res.MembersDeleted.Should().Be(0); // Lista vazia no mock

        retreat.FamiliesVersion.Should().Be(before + 1);

        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_deleta_FamilyMembers_em_cascata()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família A", "Verde");

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.Should().NotBeNull();
        res.FamilyName.Should().Be("Família A");

        // ✅ Handler não chama mais RemoveByFamilyIdAsync explicitamente (deleção em cascata do EF)
        // fmRepo.Verify(f => f.RemoveByFamilyIdAsync(fam.Id, It.IsAny<CancellationToken>()), Times.Once);
        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_retorna_contagem_de_membros_deletados()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família Test", "Azul");

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out _);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        // ✅ Simular 4 membros na família
        var members = new List<FamilyMember>
        {
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 0),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 1),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 2),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 3)
        };

        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(members);

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.MembersDeleted.Should().Be(4);
    }

    [Fact]
    public async Task Sucesso_permite_deletar_multiplas_familias_consecutivamente()
    {
        var retreat = NewRetreat();
        var fam1 = NewFamily(retreat.Id, "Família 1", "Azul");
        var fam2 = NewFamily(retreat.Id, "Família 2", "Verde");

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out var fmRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam1.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam1);
        familyRepo.Setup(f => f.GetByIdAsync(fam2.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam2);

        var versionBefore = retreat.FamiliesVersion;

        // Deletar primeira família
        var res1 = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam1.Id), default);
        
        res1.FamilyName.Should().Be("Família 1");
        familyRepo.Verify(f => f.DeleteAsync(fam1, It.IsAny<CancellationToken>()), Times.Once);
        retreat.FamiliesVersion.Should().Be(versionBefore + 1);

        // Deletar segunda família
        var res2 = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam2.Id), default);
        
        res2.FamilyName.Should().Be("Família 2");
        familyRepo.Verify(f => f.DeleteAsync(fam2, It.IsAny<CancellationToken>()), Times.Once);
        retreat.FamiliesVersion.Should().Be(versionBefore + 2);

        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Sucesso_cor_fica_disponivel_apos_delecao()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família Roxa", "Roxo");

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _, out _);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        fam.Color.Name.Should().Be("Roxo");

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.FamilyName.Should().Be("Família Roxa");
        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_deleta_familia_com_padrinhos_e_madrinhas()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família Especial", "Amarelo");

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        // ✅ Simular membros com padrinhos/madrinhas
        var members = new List<FamilyMember>
        {
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 0, isPadrinho: true, isMadrinha: false),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 1, isPadrinho: true, isMadrinha: false),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 2, isPadrinho: false, isMadrinha: true),
            new FamilyMember(retreat.Id, fam.Id, Guid.NewGuid(), 3, isPadrinho: false, isMadrinha: true)
        };

        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(members);
        
        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.MembersDeleted.Should().Be(4);
        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_versao_incrementada_corretamente()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out _, out _);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var initialVersion = retreat.FamiliesVersion;

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.Version.Should().Be(initialVersion + 1);
        retreat.FamiliesVersion.Should().Be(initialVersion + 1);
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_ordem_de_execucao_correta()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out var fmRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        var executionOrder = new List<string>();

        fmRepo.Setup(f => f.ListByFamilyAsync(fam.Id, It.IsAny<CancellationToken>()))
              .Callback(() => executionOrder.Add("ListFamilyMembers"))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()))
                  .Callback(() => executionOrder.Add("DeleteFamily"))
                  .Returns(Task.CompletedTask);

        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Callback(() => executionOrder.Add("UpdateRetreat"))
               .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Callback(() => executionOrder.Add("SaveChanges"))
           .Returns(Task.CompletedTask);

        await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        executionOrder.Should().ContainInOrder(
            "ListFamilyMembers",
            "DeleteFamily",
            "UpdateRetreat",
            "SaveChanges"
        );
    }

    [Fact]
    public async Task Sucesso_com_capacidade_customizada()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id, "Família Grande", "Marrom", capacity: 6);

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _, out _);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        fam.Capacity.Should().Be(6);

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);

        res.FamilyName.Should().Be("Família Grande");
        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
    }
}
