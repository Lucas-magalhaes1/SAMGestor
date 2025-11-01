using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions; 
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.Create;

public class CreateTentHandlerTests
{
    private static Retreat MakeRetreat(Guid id, bool? tentsLocked = null)
    {
        var r = (Retreat)FormatterServices.GetUninitializedObject(typeof(Retreat));
        
        var idProp = typeof(Retreat).GetProperty("Id");
        if (idProp != null && idProp.CanWrite)
            idProp.SetValue(r, id);
        else
        {
            var idField = typeof(Retreat).GetField("<Id>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            idField?.SetValue(r, id);
        }
        
        if (tentsLocked is not null)
        {
            var lockProp = typeof(Retreat).GetProperty("TentsLocked",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (lockProp is not null && lockProp.CanWrite)
                lockProp.SetValue(r, tentsLocked.Value);
            else
            {
                var lockField = typeof(Retreat).GetField("<TentsLocked>k__BackingField",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                lockField?.SetValue(r, tentsLocked.Value);
            }
        }

        return r;
    }

    private static (CreateTentHandler handler,
                    Mock<IRetreatRepository> retreatRepo,
                    Mock<ITentRepository> tentRepo,
                    Mock<IUnitOfWork> uow)
        BuildHandler(Retreat retreat, bool existsNumber = false)
    {
        var retreatRepo = new Mock<IRetreatRepository>(MockBehavior.Strict);
        var tentRepo    = new Mock<ITentRepository>(MockBehavior.Strict);
        var uow         = new Mock<IUnitOfWork>(MockBehavior.Strict);

        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        tentRepo.Setup(t => t.ExistsNumberAsync(
                           It.IsAny<Guid>(),
                           It.IsAny<TentCategory>(),
                           It.IsAny<TentNumber>(),
                           It.IsAny<Guid?>(),
                           It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid _, TentCategory _, TentNumber _, Guid? _, CancellationToken _) => existsNumber);

        tentRepo.Setup(t => t.AddAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new CreateTentHandler(retreatRepo.Object, tentRepo.Object, uow.Object);
        return (handler, retreatRepo, tentRepo, uow);
    }

    [Fact]
    public async Task Should_create_tent_successfully_and_save()
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(retreatId);

        var (handler, _, tentRepo, uow) = BuildHandler(retreat, existsNumber: false);

        Tent? captured = null;
        tentRepo.Setup(t => t.AddAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
                .Callback<Tent, CancellationToken>((t, _) => captured = t)
                .Returns(Task.CompletedTask);

        var cmd = new CreateTentCommand(
            RetreatId: retreatId,
            Number: "12",
            Category: TentCategory.Male,
            Capacity: 6,
            Notes: "  perto do muro  "
        );

        var res = await handler.Handle(cmd, CancellationToken.None);

        res.RetreatId.Should().Be(retreatId);
        res.TentId.Should().NotBeEmpty();

        captured.Should().NotBeNull();
        captured!.Number.Value.Should().Be(12);
        captured.Category.Should().Be(TentCategory.Male);
        captured.Capacity.Should().Be(6);
        captured.Notes.Should().Be("perto do muro");
        
        tentRepo.Verify(t => t.ExistsNumberAsync(
            retreatId,
            TentCategory.Male,
            It.Is<TentNumber>(n => n.Value == 12),
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        tentRepo.Verify(t => t.AddAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var retreatRepo = new Mock<IRetreatRepository>(MockBehavior.Strict);
        var tentRepo    = new Mock<ITentRepository>(MockBehavior.Strict);
        var uow         = new Mock<IUnitOfWork>(MockBehavior.Strict);

        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var handler = new CreateTentHandler(retreatRepo.Object, tentRepo.Object, uow.Object);

        var cmd = new CreateTentCommand(Guid.NewGuid(), "1", TentCategory.Female, 4, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_block_when_tents_locked_in_retreat()
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(retreatId, tentsLocked: true);

        var (handler, _, _, _) = BuildHandler(retreat, existsNumber: false);

        var cmd = new CreateTentCommand(retreatId, "3", TentCategory.Female, 4, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*travadas*");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Should_fail_when_number_is_not_numeric(string number)
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(retreatId);

        var (handler, _, _, _) = BuildHandler(retreat, existsNumber: false);

        var cmd = new CreateTentCommand(retreatId, number, TentCategory.Male, 2, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Number inválido*");
    }

    [Fact]
    public async Task Should_fail_when_duplicated_number_in_same_retreat_and_category()
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(retreatId);

        var (handler, _, tentRepo, _) = BuildHandler(retreat, existsNumber: true);

        var cmd = new CreateTentCommand(retreatId, "10", TentCategory.Female, 4, null);

        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Já existe barraca*");

        tentRepo.Verify(t => t.AddAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}