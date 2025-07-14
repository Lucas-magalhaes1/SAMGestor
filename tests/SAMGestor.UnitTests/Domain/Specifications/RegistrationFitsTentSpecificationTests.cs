using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class RegistrationFitsTentSpecificationTests
    {
        private static Registration CreateRegistration(Gender gender)
        {
            return new Registration(
                new FullName("Alex Souza"),
                new CPF("12312312300"),
                new EmailAddress("alex@email.com"),
                "11991112222",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-23)),
                gender,
                "Campinas",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                Guid.NewGuid());
        }

        private static Tent CreateTent(int number, TentCategory category)
        {
            return new Tent(
                new TentNumber(number),
                category,
                4,
                Guid.NewGuid()); 
        }

        [Theory]
        [InlineData(Gender.Male,   TentCategory.Male)]
        [InlineData(Gender.Female, TentCategory.Female)]
        public void Should_Return_True_When_Gender_Matches_TentCategory(Gender gender, TentCategory category)
        {
            var registration = CreateRegistration(gender);
            var tent         = CreateTent(1, category);

            var spec = new RegistrationFitsTentSpecification(tent);
            
            Assert.True(spec.IsSatisfiedBy(registration));
        }
        
        [Theory]
        [InlineData(Gender.Male,   TentCategory.Female)]
        [InlineData(Gender.Female, TentCategory.Male)]
        public void Should_Return_False_When_Gender_Mismatches_Category(Gender gender, TentCategory category)
        {
            var registration = CreateRegistration(gender);
            var tent         = CreateTent(1, category);

            var spec = new RegistrationFitsTentSpecification(tent);

            Assert.False(spec.IsSatisfiedBy(registration));
        }
    }
}
