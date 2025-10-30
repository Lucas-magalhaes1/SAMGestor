using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Create;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.Create;

public class CreateTentValidatorTests
{
    private static CreateTentValidator BuildValidator(bool existsNumber = false)
    {
        var repo = new Mock<ITentRepository>(MockBehavior.Strict);

        repo.Setup(r => r.ExistsNumberAsync(
                It.IsAny<Guid>(),
                It.IsAny<TentCategory>(),
                It.IsAny<TentNumber>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existsNumber);

        return new CreateTentValidator(repo.Object);
    }

    [Fact]
    public async Task Valid_payload_should_pass()
    {
        var v = BuildValidator(existsNumber: false);
        var cmd = new CreateTentCommand(Guid.NewGuid(), "7", TentCategory.Male, 3, "ok");
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public async Task Empty_retreat_should_fail()
    {
        var v = BuildValidator();
        var cmd = new CreateTentCommand(Guid.Empty, "7", TentCategory.Male, 3, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTentCommand.RetreatId));
    }

    [Theory]
    [InlineData(""), InlineData("abc"), InlineData("  ")]
    public async Task Non_numeric_number_should_fail(string number)
    {
        var v = BuildValidator();
        var cmd = new CreateTentCommand(Guid.NewGuid(), number, TentCategory.Male, 3, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTentCommand.Number));
    }

    [Fact]
    public async Task Invalid_category_should_fail()
    {
        var v = BuildValidator();
        var invalid = (TentCategory)999; // fora do enum
        var cmd = new CreateTentCommand(Guid.NewGuid(), "1", invalid, 3, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateTentCommand.Category));
    }

    [Theory]
    [InlineData(0), InlineData(-1)]
    public async Task Capacity_must_be_positive(int capacity)
    {
        var v = BuildValidator();
        var cmd = new CreateTentCommand(Guid.NewGuid(), "1", TentCategory.Female, capacity, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTentCommand.Capacity));
    }

    [Fact]
    public async Task Notes_over_200_should_fail()
    {
        var v = BuildValidator();
        var longNotes = new string('x', 201);
        var cmd = new CreateTentCommand(Guid.NewGuid(), "1", TentCategory.Male, 2, longNotes);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTentCommand.Notes));
    }

    [Fact]
    public async Task Duplicate_number_should_fail_uniqueness_rule()
    {
        var v = BuildValidator(existsNumber: true);
        var cmd = new CreateTentCommand(Guid.NewGuid(), "15", TentCategory.Female, 4, null);
        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.ErrorMessage.Contains("Já existe barraca"));
    }
}