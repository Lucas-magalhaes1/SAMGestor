using System;
using Moq;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class CpfNotBlockedSpecificationTests
    {
        private static Registration CreateRegistration(string rawCpf)
        {
            var retreatId = Guid.NewGuid();
            return new Registration(
                new FullName("João Souza"),
                new CPF(rawCpf),
                new EmailAddress("joao@email.com"),
                "11995550101",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
                Gender.Male,
                "Campinas",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                retreatId);
        }

        [Fact]
        public void Should_Return_True_When_Cpf_Is_Not_Blocked()
        {
            var cpf = new CPF("11122233344");
            var reg = CreateRegistration("11122233344");

            var repo = new Mock<IRegistrationRepository>();
            repo.Setup(r => r.IsCpfBlocked(cpf)).Returns(false);

            var spec = new CpfNotBlockedSpecification(repo.Object);
            
            var result = spec.IsSatisfiedBy(reg);
            
            Assert.True(result);
        }

        [Fact]
        public void Should_Return_False_When_Cpf_Is_Blocked()
        {
            
            var cpf = new CPF("11122233344");
            var reg = CreateRegistration("11122233344");

            var repo = new Mock<IRegistrationRepository>();
            repo.Setup(r => r.IsCpfBlocked(cpf)).Returns(true); 

            var spec = new CpfNotBlockedSpecification(repo.Object);
            
            var result = spec.IsSatisfiedBy(reg);
            
            Assert.False(result);
        }
    }
}
