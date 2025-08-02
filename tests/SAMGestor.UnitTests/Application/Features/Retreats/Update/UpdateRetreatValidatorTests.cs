using System;
using FluentAssertions;
using FluentValidation.TestHelper;
using SAMGestor.Application.Features.Retreats.Update;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Retreats.Update
{
    public class UpdateRetreatValidatorTests
    {
        private readonly UpdateRetreatValidator _validator = new();

        private static UpdateRetreatCommand ValidCmd(Guid id) =>
            new(
                id,
                new FullName("Retiro Validação"),
                "2035",
                "Tema",
                new DateOnly(2035,1,1),
                new DateOnly(2035,1,3),
                5,5,
                new DateOnly(2034,12,1),
                new DateOnly(2034,12,2),
                new Money(100,"BRL"),
                new Money(50,"BRL"),
                new Percentage(50),
                new Percentage(50));

        [Fact]
        public void Should_Have_Error_When_Id_Empty()
        {
            var cmd = ValidCmd(Guid.Empty);

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.Id);
        }

        [Fact]
        public void Should_Have_Error_When_Percentages_Invalid()
        {
            var cmd = ValidCmd(Guid.NewGuid()) with
            {
                WestRegionPct  = new Percentage(70),
                OtherRegionPct = new Percentage(20)
            };

            var result = _validator.TestValidate(cmd);

            result.Errors.Should()
                .Contain(e => e.ErrorMessage ==
                              "WestRegionPct + OtherRegionPct must equal 100.");
        }
    }
}