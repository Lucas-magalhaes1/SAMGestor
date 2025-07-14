using System;
using Moq;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class RegistrationPeriodOpenSpecificationTests
    {
        private static Retreat CreateRetreat(DateOnly start, DateOnly end)
        {
            var zero = new Money(0);               

            return new Retreat(
                new FullName("Retiro Jovens"),      
                "10ª",                              
                "Tema",                             
                start,                              
                end,                                
                50,                                 
                50,                                 
                start,                              
                end,                                
                zero,                               
                zero,                               
                new Percentage(70),                 
                new Percentage(30));                
        }

        private static Registration CreateRegistration(Guid retreatId)
        {
            return new Registration(
                new FullName("Maria Lima"),
                new CPF("98765432100"),
                new EmailAddress("maria@email.com"),
                "11998887777",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-22)),
                SAMGestor.Domain.Enums.Gender.Female,
                "São Paulo",
                SAMGestor.Domain.Enums.RegistrationStatus.NotSelected,
                SAMGestor.Domain.Enums.ParticipationCategory.Guest,
                "Oeste",
                retreatId);
        }
        
        [Fact]
        public void Should_Return_True_When_Date_Is_Inside_Registration_Window()
        {
            
            var today      = new DateTime(2025,  7, 15);
            var retreatId  = Guid.NewGuid();

            var retreat    = CreateRetreat(DateOnly.FromDateTime(today.AddDays(-1)),
                                           DateOnly.FromDateTime(today.AddDays( 5)));

            var reg        = CreateRegistration(retreatId);

            var repoMock   = new Mock<IRetreatRepository>();
            repoMock.Setup(r => r.GetById(retreatId)).Returns(retreat);

            var spec = new RegistrationPeriodOpenSpecification(repoMock.Object,
                                                               () => today); 

            
            var result = spec.IsSatisfiedBy(reg);

            
            Assert.True(result);
        }

        [Fact]
        public void Should_Return_False_When_Date_Is_Outside_Registration_Window()
        {
            var today      = new DateTime(2025,  7, 15);
            var retreatId  = Guid.NewGuid();

            var retreat    = CreateRetreat(DateOnly.FromDateTime(today.AddDays(-10)),
                                           DateOnly.FromDateTime(today.AddDays(-5))); 

            var reg        = CreateRegistration(retreatId);

            var repoMock   = new Mock<IRetreatRepository>();
            repoMock.Setup(r => r.GetById(retreatId)).Returns(retreat);

            var spec = new RegistrationPeriodOpenSpecification(repoMock.Object,
                                                               () => today);
            var result = spec.IsSatisfiedBy(reg);
            
            Assert.False(result);
        }
    }
}
