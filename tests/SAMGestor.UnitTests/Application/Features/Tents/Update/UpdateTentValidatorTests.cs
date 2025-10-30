using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Update;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.Update;

public class UpdateTentValidatorTests
{
    private static UpdateTentValidator BuildValidator(bool existsNumber = false)
    {
        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(r => r.ExistsNumberAsync(
                It.IsAny<Guid>(),
                It.IsAny<TentCategory>(),
                It.IsAny<TentNumber>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existsNumber);

        return new UpdateTentValidator(tentRepo.Object);
    }

    [Fact]
    public async Task Valid_payload_should_pass()
    {
        var v = BuildValidator(existsNumber: false);
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "12", TentCategory.Male, 6, true, "ok");
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_number_should_fail()
    {
        var v = BuildValidator();
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "abc", TentCategory.Male, 4, null, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTentCommand.Number));
    }

    [Fact]
    public async Task Invalid_category_should_fail()
    {
        var v = BuildValidator();
        var invalid = (TentCategory)999;
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "1", invalid, 3, null, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTentCommand.Category));
    }

    [Fact]
    public async Task Capacity_must_be_positive()
    {
        var v = BuildValidator();
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "1", TentCategory.Female, 0, null, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTentCommand.Capacity));
    }

    [Fact]
    public async Task Notes_too_long_should_fail()
    {
        var v = BuildValidator();
        var notes = new string('x', 201);
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "1", TentCategory.Male, 3, null, notes);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateTentCommand.Notes));
    }

    [Fact]
    public async Task Duplicate_number_should_fail()
    {
        var v = BuildValidator(existsNumber: true);
        var cmd = new UpdateTentCommand(Guid.NewGuid(), Guid.NewGuid(), "10", TentCategory.Male, 3, null, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.ErrorMessage.Contains("já existe", StringComparison.OrdinalIgnoreCase));
    }
}
