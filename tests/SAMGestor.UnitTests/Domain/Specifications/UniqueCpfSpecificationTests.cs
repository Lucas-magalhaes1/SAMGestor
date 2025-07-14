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
    public class UniqueCpfSpecificationTests
    {
        // ---------- helper local ---------
        private static Registration CreateRegistration(string rawCpf, Guid retreatId)
        {
            return new Registration(
                new FullName("Ana Silva"),
                new CPF(rawCpf),
                new EmailAddress("ana@email.com"),
                "1199000000",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Gender.Female,
                "São Paulo",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                retreatId);
        }

        // ---------- testes ---------------

        [Fact]
        public void Should_Return_True_When_Cpf_Is_Unique_In_Retreat()
        {
            // arrange
            var retreatId = Guid.NewGuid();
            var cpf       = new CPF("12345678901");

            var incoming = CreateRegistration("12345678901", retreatId);

            var repoMock = new Mock<IRegistrationRepository>();
            repoMock.Setup(r => r.GetByCpfAndRetreat(cpf, retreatId))
                    .Returns((Registration?)null);

            var spec = new UniqueCpfSpecification(repoMock.Object);

            // act
            var result = spec.IsSatisfiedBy(incoming);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Should_Return_False_When_Cpf_Already_Exists_In_Same_Retreat()
        {
            // arrange
            var retreatId = Guid.NewGuid();
            var cpf       = new CPF("12345678901");

            var existing = CreateRegistration("12345678901", retreatId);
            var incoming = CreateRegistration("12345678901", retreatId);

            var repoMock = new Mock<IRegistrationRepository>();
            repoMock.Setup(r => r.GetByCpfAndRetreat(cpf, retreatId))
                    .Returns(existing); // já há uma inscrição com esse CPF

            var spec = new UniqueCpfSpecification(repoMock.Object);

            // act
            var result = spec.IsSatisfiedBy(incoming);

            // assert
            Assert.False(result);
        }
    }
}
