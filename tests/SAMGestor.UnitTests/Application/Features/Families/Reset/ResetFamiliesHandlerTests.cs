using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Reset;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.Reset;

public sealed class ResetFamiliesHandlerTests
{
    private static Retreat NewRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            10, 10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50)
        );

    private static Family NewFamily(Guid retreatId, string name = "Família X", int capacity = 4, bool locked = false)
    {
        var fam = new Family(new FamilyName(name), retreatId, capacity);
        if (locked) fam.Lock();
        return fam;
    }

    [Fact]
    public async Task NotFound_retiro_deve_lancar_NotFoundException()
    {
        var cmd = new ResetFamiliesCommand(Guid.NewGuid(), ForceLockedFamilies: false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Lock_global_bloqueia_reset()
    {
        var retreat = NewRetreat();
        retreat.LockFamilies(); 

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            new Mock<IFamilyRepository>().Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Sem_familias_retorna_counts_zero_sem_deletar()
    {
        var retreat = NewRetreat();
        var initialVersion = retreat.FamiliesVersion;

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family>());

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        var uow = new Mock<IUnitOfWork>();

        var handler = new ResetFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, uow.Object);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(initialVersion); 
        res.FamiliesDeleted.Should().Be(0);
        res.MembersDeleted.Should().Be(0);

        familyRepo.Verify(r => r.DeleteAllByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Com_familias_sem_locks_deleta_tudo_bumpa_versao_e_retorna_totais()
    {
        var retreat = NewRetreat();
        var initialVersion = retreat.FamiliesVersion;

        var fam1 = NewFamily(retreat.Id, "Família 1");
        var fam2 = NewFamily(retreat.Id, "Família 2");

        var members = new List<FamilyMember>
        {
            new FamilyMember(retreat.Id, fam1.Id, Guid.NewGuid(), 0),
            new FamilyMember(retreat.Id, fam1.Id, Guid.NewGuid(), 1),
            new FamilyMember(retreat.Id, fam2.Id, Guid.NewGuid(), 0),
        };

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { fam1, fam2 });
        familyRepo.Setup(r => r.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(members);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new ResetFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, uow.Object);

        var res = await handler.Handle(cmd, default);
        
        res.Version.Should().Be(initialVersion + 1);
        res.FamiliesDeleted.Should().Be(2);
        res.MembersDeleted.Should().Be(3);

        familyRepo.Verify(r => r.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Com_familias_lockadas_e_force_false_deve_falhar()
    {
        var retreat = NewRetreat();

        var locked = NewFamily(retreat.Id, "Família L", locked: true);
        var normal = NewFamily(retreat.Id, "Família N", locked: false);

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { locked, normal });

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            familyRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*"); 
    }

    [Fact]
    public async Task Todas_familias_lockadas_e_force_true_deve_falhar()
    {
        var retreat = NewRetreat();

        var locked1 = NewFamily(retreat.Id, "Família 1", locked: true);
        var locked2 = NewFamily(retreat.Id, "Família 2", locked: true);

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { locked1, locked2 });

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            familyRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Todas as famílias estão bloqueadas*");
    }

    [Fact]
    public async Task Mistura_de_lock_e_unlock_com_force_true_deve_apagar_tudo()
    {
        var retreat = NewRetreat();
        var initialVersion = retreat.FamiliesVersion;

        var locked  = NewFamily(retreat.Id, "Família L", locked: true);
        var normal1 = NewFamily(retreat.Id, "Família A");
        var normal2 = NewFamily(retreat.Id, "Família B");

        var members = new List<FamilyMember>
        {
            new FamilyMember(retreat.Id, normal1.Id, Guid.NewGuid(), 0),
            new FamilyMember(retreat.Id, locked.Id,  Guid.NewGuid(), 0),
        };

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { locked, normal1, normal2 });
        familyRepo.Setup(r => r.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var fmRepo = new Mock<IFamilyMemberRepository>();
        fmRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(members);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new ResetFamiliesHandler(retreatRepo.Object, familyRepo.Object, fmRepo.Object, uow.Object);

        var res = await handler.Handle(cmd, default);

        res.Version.Should().Be(initialVersion + 1);
        res.FamiliesDeleted.Should().Be(3);
        res.MembersDeleted.Should().Be(2);

        familyRepo.Verify(r => r.DeleteAllByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task Se_alguma_familia_estiver_com_grupo_em_criacao_deve_falhar()
    {
        
        var retreat = NewRetreat();

        var famSemGrupo   = NewFamily(retreat.Id, "Família Normal");
        var famComGrupo   = NewFamily(retreat.Id, "Família Com Grupo");
        
        famComGrupo.MarkGroupCreating(); 

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Family> { famSemGrupo, famComGrupo });

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            familyRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );

        // act + assert
        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*grupos*"); 
    }
    
    [Fact]
    public async Task Se_alguma_familia_estiver_com_grupo_ativo_deve_falhar()
    {
       
        var retreat = NewRetreat();

        var famNormal = NewFamily(retreat.Id, "Família Normal");
        var famAtiva  = NewFamily(retreat.Id, "Família Com Grupo Ativo");
        
        var groupStatusProp = typeof(Family)
            .GetProperty("GroupStatus");

        groupStatusProp.Should().NotBeNull("a entidade Family deve ter a propriedade GroupStatus");

        groupStatusProp!.SetValue(famAtiva, GroupStatus.Active);

        var cmd = new ResetFamiliesCommand(retreat.Id, ForceLockedFamilies: true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Family> { famNormal, famAtiva });

        var handler = new ResetFamiliesHandler(
            retreatRepo.Object,
            familyRepo.Object,
            new Mock<IFamilyMemberRepository>().Object,
            new Mock<IUnitOfWork>().Object
        );
        
        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*grupos*");
    }


}
