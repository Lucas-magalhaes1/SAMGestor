using System;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class FeitoNotContemplableSpecificationTests
    {
        private static Registration CreateRegistration(bool completed)
        {
            var reg = new Registration(
                new FullName("Juliana Souza"),
                new CPF("32165498700"),
                new EmailAddress("ju@mail.com"),
                "11997776655",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-26)),
                Gender.Female,
                "Sorocaba",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                Guid.NewGuid());

            if (completed) reg.CompleteRetreat();  
            return reg;
        }

        [Fact]
        public void Should_Return_True_When_Person_Has_Not_Completed_Retreat_Before()
        {
            
            var reg  = CreateRegistration(completed: false);
            var spec = new FeitoNotContemplableSpecification();

           
            Assert.True(spec.IsSatisfiedBy(reg));
        }

        [Fact]
        public void Should_Return_False_When_Person_Has_Completed_Retreat_Before()
        {
            var reg  = CreateRegistration(completed: true);
            var spec = new FeitoNotContemplableSpecification();

            Assert.False(spec.IsSatisfiedBy(reg));
        }
    }
}