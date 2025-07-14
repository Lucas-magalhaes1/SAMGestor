using System;
using System.Collections.Generic;
using Moq;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class SingleRetreatPerCpfSpecificationTests
    {
        private static Registration CreateRegistration(Guid retreatId, bool completed)
        {
            var reg = new Registration(
                new FullName("Carlos Souza"),
                new CPF("65432198700"),
                new EmailAddress("carlos@mail.com"),
                "11991112222",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-28)),
                Gender.Male,
                "Campinas",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                retreatId);

            if (completed) reg.CompleteRetreat();
            return reg;
        }

        [Fact]
        public void Should_Return_True_When_Cpf_Has_No_Completed_Retreats()
        {
            var cpf = new CPF("65432198700");
            var current = CreateRegistration(Guid.NewGuid(), false);

            var repoMock = new Mock<IRegistrationRepository>();
            repoMock.Setup(r => r.GetAllByCpf(cpf))
                    .Returns(new List<Registration>());  

            var spec = new SingleRetreatPerCpfSpecification(repoMock.Object);
            
            Assert.True(spec.IsSatisfiedBy(current));
        }

        [Fact]
        public void Should_Return_False_When_Cpf_Already_Has_Completed_Retreat()
        {
            var cpf = new CPF("65432198700");
            var current   = CreateRegistration(Guid.NewGuid(), false);
            var completed = CreateRegistration(Guid.NewGuid(), true);

            var repoMock = new Mock<IRegistrationRepository>();
            repoMock.Setup(r => r.GetAllByCpf(cpf))
                    .Returns(new List<Registration> { completed });

            var spec = new SingleRetreatPerCpfSpecification(repoMock.Object);

            Assert.False(spec.IsSatisfiedBy(current));
        }
    }
}
