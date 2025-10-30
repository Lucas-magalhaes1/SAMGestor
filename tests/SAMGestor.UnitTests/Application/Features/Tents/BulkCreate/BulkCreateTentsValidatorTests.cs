using FluentAssertions;
using SAMGestor.Application.Features.Tents.BulkCreate;
using SAMGestor.Domain.Enums;

namespace SAMGestor.UnitTests.Application.Features.Tents.BulkCreate;

public class BulkCreateTentsValidatorTests
{
    private readonly BulkCreateTentsValidator _validator = new();

    [Fact]
    public void Valid_payload_should_pass()
    {
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[]
            {
                new BulkCreateTentItemDto("1", TentCategory.Male, 4, null),
                new BulkCreateTentItemDto("2", TentCategory.Female, 3, "ok")
            });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeTrue(res.ToString());
    }

    [Fact]
    public void Empty_items_should_fail()
    {
        var cmd = new BulkCreateTentsCommand(Guid.NewGuid(), Array.Empty<BulkCreateTentItemDto>());
        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.ErrorMessage.Contains("Items não pode ser vazio"));
    }

    [Fact]
    public void Non_numeric_number_should_fail()
    {
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[] { new BulkCreateTentItemDto("X", TentCategory.Male, 4, null) });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.ErrorMessage.Contains("Number deve ser numérico"));
    }

    [Fact]
    public void Capacity_less_or_equal_zero_should_fail()
    {
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[] { new BulkCreateTentItemDto("1", TentCategory.Male, 0, null) });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.EndsWith(".Capacity"));
    }

    [Fact]
    public void Notes_over_200_chars_should_fail()
    {
        var notes = new string('a', 201);
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[] { new BulkCreateTentItemDto("1", TentCategory.Male, 2, notes) });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName.EndsWith(".Notes"));
    }

    [Fact]
    public void Duplicate_exact_pairs_in_payload_should_fail()
    {
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[]
            {
                new BulkCreateTentItemDto("1", TentCategory.Male, 2, null),
                new BulkCreateTentItemDto("1", TentCategory.Male, 3, null) // mesma Category+Number string
            });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.ErrorMessage.Contains("duplicadas no payload"));
    }

    [Fact]
    public void Category_out_of_range_should_fail()
    {
        var invalid = (TentCategory)999;
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[] { new BulkCreateTentItemDto("1", invalid, 2, null) });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validator_allows_01_and_1_but_handler_will_catch()
    {
        var cmd = new BulkCreateTentsCommand(
            Guid.NewGuid(),
            new[]
            {
                new BulkCreateTentItemDto("1",  TentCategory.Male, 2, null),
                new BulkCreateTentItemDto("01", TentCategory.Male, 2, null) // strings diferentes
            });

        var res = _validator.Validate(cmd);
        res.IsValid.Should().BeTrue("o handler trata duplicidade numérica; o validator só olha a string");
    }
}
