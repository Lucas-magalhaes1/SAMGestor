using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.GetById;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.GetById;

public class GetTentByIdHandlerTests
{
    private static Tent NewTent(Guid retreatId, int number = 7, TentCategory category = TentCategory.Male, int capacity = 4)
    {
        var tent = new Tent(new TentNumber(number), category, capacity, retreatId);
        return tent;
    }

    private static void SetTentId(Tent tent, Guid tentId)
    {
        // se Id não tiver setter público, seta por reflection
        typeof(Tent).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(tent, tentId);
    }

    private static GetTentByIdHandler BuildHandler(
        Tent? tent = null,
        int assignedCount = 0)
    {
        var tentRepo = new Mock<ITentRepository>();
        var regRepo  = new Mock<IRegistrationRepository>();

        tentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tent);

        regRepo.Setup(r => r.CountByTentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(assignedCount);

        return new GetTentByIdHandler(tentRepo.Object, regRepo.Object);
    }

    [Fact]
    public async Task Should_return_tent_with_assigned_count_on_happy_path()
    {
        var retreatId = Guid.NewGuid();
        var tentId    = Guid.NewGuid();

        var tent = NewTent(retreatId, number: 12, category: TentCategory.Female, capacity: 6);
        SetTentId(tent, tentId);

        var assigned = 3;
        var handler = BuildHandler(tent, assigned);

        var query = new GetTentByIdQuery(RetreatId: retreatId, TentId: tentId);
        var res   = await handler.Handle(query, CancellationToken.None);

        res.TentId.Should().Be(tentId);
        res.RetreatId.Should().Be(retreatId);
        res.Number.Should().Be("12");
        res.Category.Should().Be(TentCategory.Female);
        res.Capacity.Should().Be(6);
        res.IsActive.Should().Be(tent.IsActive);
        res.IsLocked.Should().Be(tent.IsLocked);
        res.Notes.Should().Be(tent.Notes);
        res.AssignedCount.Should().Be(assigned);
    }

    [Fact]
    public async Task Should_throw_NotFound_when_tent_does_not_exist()
    {
        var handler = BuildHandler(tent: null);

        var query = new GetTentByIdQuery(RetreatId: Guid.NewGuid(), TentId: Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tent*");
    }

    [Fact]
    public async Task Should_throw_NotFound_when_tent_belongs_to_another_retreat()
    {
        var queryRetreatId = Guid.NewGuid();
        var tentRetreatId  = Guid.NewGuid();
        var tentId         = Guid.NewGuid();

        var tent = NewTent(tentRetreatId, number: 5, category: TentCategory.Male, capacity: 4);
        SetTentId(tent, tentId);

        var handler = BuildHandler(tent, assignedCount: 0);

        var query = new GetTentByIdQuery(RetreatId: queryRetreatId, TentId: tentId);
        await FluentActions.Invoking(() => handler.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tent*");
    }

    [Fact]
    public async Task Should_call_CountByTent_with_correct_id()
    {
        var retreatId = Guid.NewGuid();
        var tentId    = Guid.NewGuid();

        var tent = NewTent(retreatId, number: 3, category: TentCategory.Male, capacity: 2);
        SetTentId(tent, tentId);

        var tentRepo = new Mock<ITentRepository>();
        var regRepo  = new Mock<IRegistrationRepository>();

        tentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tent);

        regRepo.Setup(r => r.CountByTentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(0);

        var handler = new GetTentByIdHandler(tentRepo.Object, regRepo.Object);

        var query = new GetTentByIdQuery(RetreatId: retreatId, TentId: tentId);
        _ = await handler.Handle(query, CancellationToken.None);

        regRepo.Verify(r => r.CountByTentAsync(tentId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
