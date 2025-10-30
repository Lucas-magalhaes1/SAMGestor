using System.Reflection;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.List;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.List;

public class ListTentsHandlerTests
{
    private static Tent NewTent(Guid retreatId, int number, TentCategory category, int capacity, bool isActive = true, bool isLocked = false, string? notes = null)
    {
        var t = new Tent(new TentNumber(number), category, capacity, retreatId);
        typeof(Tent).GetProperty("IsActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(t, isActive);
        typeof(Tent).GetProperty("IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(t, isLocked);
        typeof(Tent).GetProperty("Notes", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(t, notes);
        return t;
    }

    private static void SetTentId(Tent tent, Guid id)
        => typeof(Tent).GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(tent, id);

    private static ListTentsHandler BuildHandler(
        out Mock<ITentRepository> tentRepo,
        out Mock<IRegistrationRepository> regRepo,
        IEnumerable<Tent>? tents = null,
        IDictionary<Guid, int>? countMap = null)
    {
        tentRepo = new Mock<ITentRepository>();
        regRepo  = new Mock<IRegistrationRepository>();

        tentRepo.Setup(r => r.ListByRetreatAsync(It.IsAny<Guid>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tents?.ToList() ?? new List<Tent>());

        regRepo.Setup(r => r.GetAssignedCountMapByTentIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => countMap != null ? new Dictionary<Guid, int>(countMap) : new Dictionary<Guid, int>());

        return new ListTentsHandler(tentRepo.Object, regRepo.Object);
    }

    [Fact]
    public async Task Empty_list_should_return_empty_and_not_query_counts()
    {
        var retreatId = Guid.NewGuid();

        var handler = BuildHandler(out var tentRepo, out var regRepo, tents: Array.Empty<Tent>());

        var query = new ListTentsQuery(RetreatId: retreatId, Category: null, Active: null);
        var res = await handler.Handle(query, CancellationToken.None);

        res.Should().BeEmpty();

        regRepo.Verify(r => r.GetAssignedCountMapByTentIdsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_filter_by_category()
    {
        var retreatId = Guid.NewGuid();

        var t1 = NewTent(retreatId, 1, TentCategory.Male,   4, isActive: true);
        var t2 = NewTent(retreatId, 2, TentCategory.Female, 4, isActive: true);
        var t3 = NewTent(retreatId, 3, TentCategory.Male,   4, isActive: false);
        SetTentId(t1, Guid.NewGuid());
        SetTentId(t2, Guid.NewGuid());
        SetTentId(t3, Guid.NewGuid());

        var handler = BuildHandler(out var tentRepo, out var regRepo, tents: new[] { t1, t2, t3 });

        var query = new ListTentsQuery(RetreatId: retreatId, Category: TentCategory.Male, Active: null);
        var res = (await handler.Handle(query, CancellationToken.None)).ToList();

        res.Should().OnlyContain(x => x.Category == TentCategory.Male);

        regRepo.Verify(r => r.GetAssignedCountMapByTentIdsAsync(
            retreatId,
            It.Is<Guid[]>(ids => ids.Length == 2 && ids.Contains(t1.Id) && ids.Contains(t3.Id)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_filter_by_active_flag()
    {
        var retreatId = Guid.NewGuid();

        var a1 = NewTent(retreatId, 10, TentCategory.Female, 6, isActive: true);
        var a2 = NewTent(retreatId, 11, TentCategory.Female, 6, isActive: false);
        var a3 = NewTent(retreatId, 12, TentCategory.Male,   6, isActive: false);
        SetTentId(a1, Guid.NewGuid());
        SetTentId(a2, Guid.NewGuid());
        SetTentId(a3, Guid.NewGuid());

        var handler = BuildHandler(out var tentRepo, out var regRepo, tents: new[] { a1, a2, a3 });

        var query = new ListTentsQuery(RetreatId: retreatId, Category: null, Active: false);
        var res = (await handler.Handle(query, CancellationToken.None)).ToList();

        res.Should().HaveCount(2);
        res.Should().OnlyContain(x => x.IsActive == false);

        regRepo.Verify(r => r.GetAssignedCountMapByTentIdsAsync(
            retreatId,
            It.Is<Guid[]>(ids => ids.Length == 2 && ids.Contains(a2.Id) && ids.Contains(a3.Id)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_map_assigned_count_and_default_missing_to_zero()
    {
        var retreatId = Guid.NewGuid();

        var t1 = NewTent(retreatId, 1, TentCategory.Male,   4);
        var t2 = NewTent(retreatId, 2, TentCategory.Female, 4);
        var t3 = NewTent(retreatId, 3, TentCategory.Male,   4);
        SetTentId(t1, Guid.NewGuid());
        SetTentId(t2, Guid.NewGuid());
        SetTentId(t3, Guid.NewGuid());

        var countMap = new Dictionary<Guid, int>
        {
            [t1.Id] = 2,
            [t3.Id] = 5
        };

        var handler = BuildHandler(out var tentRepo, out var regRepo, tents: new[] { t1, t2, t3 }, countMap: countMap);

        var query = new ListTentsQuery(RetreatId: retreatId, Category: null, Active: null);
        var res = (await handler.Handle(query, CancellationToken.None)).ToList();

        res.Should().ContainSingle(x => x.TentId == t1.Id && x.AssignedCount == 2);
        res.Should().ContainSingle(x => x.TentId == t2.Id && x.AssignedCount == 0);
        res.Should().ContainSingle(x => x.TentId == t3.Id && x.AssignedCount == 5);

        regRepo.Verify(r => r.GetAssignedCountMapByTentIdsAsync(
            retreatId,
            It.Is<Guid[]>(ids => ids.Length == 3 && ids.Contains(t1.Id) && ids.Contains(t2.Id) && ids.Contains(t3.Id)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_order_by_category_then_number()
    {
        var retreatId = Guid.NewGuid();

        var t1 = NewTent(retreatId,  2, TentCategory.Male,   4);
        var t2 = NewTent(retreatId, 10, TentCategory.Female, 4);
        var t3 = NewTent(retreatId,  1, TentCategory.Male,   4);
        var t4 = NewTent(retreatId,  3, TentCategory.Female, 4);
        SetTentId(t1, Guid.NewGuid());
        SetTentId(t2, Guid.NewGuid());
        SetTentId(t3, Guid.NewGuid());
        SetTentId(t4, Guid.NewGuid());

        var handler = BuildHandler(out _, out _, tents: new[] { t1, t2, t3, t4 });

        var query = new ListTentsQuery(RetreatId: retreatId, Category: null, Active: null);
        var res = (await handler.Handle(query, CancellationToken.None)).ToList();

        res.Select(r => r.Category).Should().BeInAscendingOrder();

        var maleNumbers   = res.Where(r => r.Category == TentCategory.Male).Select(r => r.Number).ToList();
        var femaleNumbers = res.Where(r => r.Category == TentCategory.Female).Select(r => r.Number).ToList();

        maleNumbers.Should().BeInAscendingOrder();
        femaleNumbers.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Should_pass_retreatId_and_filtered_ids_to_count_map()
    {
        var retreatId = Guid.NewGuid();

        var t1 = NewTent(retreatId, 1, TentCategory.Male,   4, isActive: true);
        var t2 = NewTent(retreatId, 2, TentCategory.Female, 4, isActive: false);
        var t3 = NewTent(retreatId, 3, TentCategory.Male,   4, isActive: true);
        SetTentId(t1, Guid.NewGuid());
        SetTentId(t2, Guid.NewGuid());
        SetTentId(t3, Guid.NewGuid());

        var tentRepo = new Mock<ITentRepository>();
        var regRepo  = new Mock<IRegistrationRepository>();

        tentRepo.Setup(r => r.ListByRetreatAsync(It.IsAny<Guid>(), null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { t1, t2, t3 });

        regRepo.Setup(r => r.GetAssignedCountMapByTentIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, int>());

        var handler = new ListTentsHandler(tentRepo.Object, regRepo.Object);

        var query = new ListTentsQuery(RetreatId: retreatId, Category: TentCategory.Male, Active: true);
        _ = await handler.Handle(query, CancellationToken.None);

        regRepo.Verify(r => r.GetAssignedCountMapByTentIdsAsync(
                retreatId,
                It.Is<Guid[]>(ids => ids.Length == 2 && ids.Contains(t1.Id) && ids.Contains(t3.Id) && !ids.Contains(t2.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
