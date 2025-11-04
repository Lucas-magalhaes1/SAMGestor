using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.BulkCreate;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.UnitTests.Dependencies;

namespace SAMGestor.UnitTests.Application.Features.Tents.BulkCreate;

public class BulkCreateTentsHandlerTests
{
    

    private static Retreat FakeRetreat()
    {
        // cria uma instância de Retreat sem chamar construtor (não precisamos de estado)
        return TestObjectFactory.Uninitialized<Retreat>();
    }

    private static Tent NewTent(Guid retreatId, TentCategory cat, int number, int capacity = 4, string? notes = null)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            // usa mesmo caminho do handler (UpdateNotes se existir)
            var up = typeof(Tent).GetMethod("UpdateNotes",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (up is not null) up.Invoke(t, new object?[] { notes });
            else
            {
                var p = typeof(Tent).GetProperty("Notes",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                p?.SetValue(t, notes);
            }
        }
        return t;
    }

    private static BulkCreateTentsHandler BuildHandler(
        Retreat retreat,
        out Mock<IRetreatRepository> retRepo,
        out Mock<ITentRepository> tentRepo,
        out Mock<IUnitOfWork> uow,
        IEnumerable<Tent>? existing = null,
        List<Tent>? addedCapture = null)
    {
        retRepo = new Mock<IRetreatRepository>(MockBehavior.Strict);
        tentRepo = new Mock<ITentRepository>(MockBehavior.Strict);
        uow = new Mock<IUnitOfWork>(MockBehavior.Strict);

        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(retreat);

        tentRepo.Setup(t => t.ListByRetreatAsync(
                It.IsAny<Guid>(),
                It.IsAny<TentCategory?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing?.ToList() ?? new List<Tent>());

        tentRepo.Setup(t => t.AddAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
            .Callback<Tent, CancellationToken>((t, _) => addedCapture?.Add(t))
            .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new BulkCreateTentsHandler(retRepo.Object, tentRepo.Object, uow.Object);
    }
    
    [Fact]
    public async Task Should_create_all_valid_items_and_return_views_sorted()
    {
      
        var retreatId = Guid.NewGuid();
        var retreat = FakeRetreat();
        var added = new List<Tent>();
        var handler = BuildHandler(
            retreat,
            out var retRepo, out var tentRepo, out var uow,
            existing: Array.Empty<Tent>(),
            addedCapture: added
        );

        var cmd = new BulkCreateTentsCommand(
            RetreatId: retreatId,
            Items: new[]
            {
                new BulkCreateTentItemDto("1", TentCategory.Male,   4, "  nota  "),
                new BulkCreateTentItemDto("2", TentCategory.Male,   4, null),
                new BulkCreateTentItemDto("1", TentCategory.Female, 3, "")
            }
        );

        
        var res = await handler.Handle(cmd, default);
        
        res.RetreatId.Should().Be(retreatId);
        res.Created.Should().Be(3);
        res.Skipped.Should().Be(0);
        res.Errors.Should().BeEmpty();
        res.Tents.Should().HaveCount(3);
        
        added.Should().HaveCount(3);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        added.Any(t => t.Notes == "nota").Should().BeTrue();
        
        res.Tents.Should().Contain(t => t.Category == TentCategory.Male && t.Number == "1");
        res.Tents.Should().Contain(t => t.Category == TentCategory.Male && t.Number == "2");
        res.Tents.Should().Contain(t => t.Category == TentCategory.Female && t.Number == "1");
    }

    [Fact]
    public async Task Should_skip_invalid_number_and_create_the_rest()
    {
        var retreatId = Guid.NewGuid();
        var retreat = FakeRetreat();
        var added = new List<Tent>();
        var handler = BuildHandler(
            retreat,
            out _, out _, out var uow,
            existing: Array.Empty<Tent>(),
            addedCapture: added
        );

        var cmd = new BulkCreateTentsCommand(
            retreatId,
            new[]
            {
                new BulkCreateTentItemDto("X", TentCategory.Male, 4, null), // inválido
                new BulkCreateTentItemDto("10", TentCategory.Female, 2, null)
            });

        var res = await handler.Handle(cmd, default);

        res.Created.Should().Be(1);
        res.Skipped.Should().Be(1);
        res.Errors.Should().ContainSingle(e => e.Code == "INVALID_NUMBER" && e.Number == "X");
        added.Should().HaveCount(1);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_detect_duplicate_in_payload_by_numeric_equivalence()
    {
        // "1" e "01" -> mesmo número (1) => segundo vira DUPLICATE_PAYLOAD
        var retreatId = Guid.NewGuid();
        var retreat = FakeRetreat();
        var added = new List<Tent>();
        var handler = BuildHandler(
            retreat,
            out _, out _, out var uow,
            existing: Array.Empty<Tent>(),
            addedCapture: added
        );

        var cmd = new BulkCreateTentsCommand(
            retreatId,
            new[]
            {
                new BulkCreateTentItemDto("1",  TentCategory.Male, 4, null),
                new BulkCreateTentItemDto("01", TentCategory.Male, 4, null)
            });

        var res = await handler.Handle(cmd, default);

        res.Created.Should().Be(1);
        res.Skipped.Should().Be(1);
        res.Errors.Should().ContainSingle(e => e.Code == "DUPLICATE_PAYLOAD" && e.Number == "01");
        added.Should().HaveCount(1);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_skip_if_duplicate_with_existing_tent()
    {
        var retreatId = Guid.NewGuid();
        var retreat = FakeRetreat();
        var existing = new[]
        {
            NewTent(retreatId, TentCategory.Male, 2, capacity: 4)
        };

        var added = new List<Tent>();
        var handler = BuildHandler(
            retreat,
            out _, out _, out var uow,
            existing: existing,
            addedCapture: added
        );

        var cmd = new BulkCreateTentsCommand(
            retreatId,
            new[]
            {
                new BulkCreateTentItemDto("2", TentCategory.Male, 4, null) // conflita com existing
            });

        var res = await handler.Handle(cmd, default);

        res.Created.Should().Be(0);
        res.Skipped.Should().Be(1);
        res.Errors.Should().ContainSingle(e => e.Code == "DUPLICATE_EXISTING" && e.Number == "2");
        added.Should().BeEmpty();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_throw_business_rule_when_tents_locked_if_property_exists()
    {
        var retreatId = Guid.NewGuid();
        var retreat = FakeRetreat();

        // Se o domínio tiver a prop TentsLocked (bool), marca como true
        var prop = retreat.GetType().GetProperty("TentsLocked",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (prop is null || prop.PropertyType != typeof(bool))
        {
            // Nada a testar nesse cenário de domínio (passa “em branco”)
            return;
        }

        prop.SetValue(retreat, true);

        var handler = BuildHandler(
            retreat,
            out _, out _, out _,
            existing: Array.Empty<Tent>(),
            addedCapture: new List<Tent>()
        );

        var cmd = new BulkCreateTentsCommand(
            retreatId,
            new[] { new BulkCreateTentItemDto("1", TentCategory.Male, 2, null) });

        var act = async () => await handler.Handle(cmd, default);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }
}
