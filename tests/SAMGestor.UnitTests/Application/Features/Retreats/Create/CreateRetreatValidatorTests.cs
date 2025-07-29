using FluentValidation.TestHelper;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Application.Features.Retreats.Create
{
    public class CreateRetreatValidatorTests
    {
        private readonly CreateRetreatValidator _validator = new();

        private CreateRetreatCommand ValidCommand() =>
            new CreateRetreatCommand(
                new FullName("Valid User"),
                "2025",
                "Valid Theme",
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                1, 1,
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                new Money(10, "BRL"),
                new Money(5, "BRL"),
                new Percentage(50),
                new Percentage(50)
            );

        [Fact]
        public void Should_Have_Error_When_Name_Is_Null()
        {
            var cmd = ValidCommand();
            cmd = cmd with { Name = null! };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Name is required.");
        }

        [Fact]
        public void Should_Have_Error_When_Percentages_Do_Not_Sum_100()
        {
            var cmd = ValidCommand();
            cmd = cmd with
            {
                WestRegionPct  = new Percentage(70),
                OtherRegionPct = new Percentage(20)
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x)
                  .WithErrorMessage("WestRegionPct + OtherRegionPct must equal 100.");
        }

        [Fact]
        public void Should_Have_Error_When_EndDate_Before_StartDate()
        {
            var cmd = ValidCommand();
            cmd = cmd with
            {
                StartDate = new DateOnly(2025, 5, 10),
                EndDate   = new DateOnly(2025, 5, 5)
            };

            var result = _validator.TestValidate(cmd);
            result.ShouldHaveValidationErrorFor(x => x.EndDate)
                  .WithErrorMessage("EndDate must be after StartDate.");
        }
    }
}
