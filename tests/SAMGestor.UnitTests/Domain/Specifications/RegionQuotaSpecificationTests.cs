using System;
using System.Collections.Generic;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class RegionQuotaSpecificationTests
    {
        private static long _cpfSeed = 90000000000;
        private static CPF NextCpf() => new((_cpfSeed++).ToString());

        private static Registration Reg(string region)
        {
            return new Registration(
                new FullName("Teste Usuário"),
                NextCpf(),
                new EmailAddress("t@mail.com"),
                "11990000000",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-21)),
                Gender.Female,
                region switch { "Oeste" => "Sorocaba", _ => "Campinas" },
                RegistrationStatus.Selected,
                ParticipationCategory.Guest,
                region,
                Guid.NewGuid());
        }

        private static IEnumerable<RegionConfig> DefaultConfigs(Guid retreatId)
        {
            return new[]
            {
                new RegionConfig("Oeste",  new Percentage(70), retreatId),
                new RegionConfig("Outra",  new Percentage(30), retreatId)
            };
        }

        // -------- caso OK: 7/10 Oeste (70 %), 3/10 Outra (30 %)
        [Fact]
        public void Should_Return_True_When_Region_Targets_Are_Met()
        {
            var retreatId = Guid.NewGuid();
            var regs = new List<Registration>();

            regs.AddRange(Enumerable.Repeat(Reg("Oeste"), 7));
            regs.AddRange(Enumerable.Repeat(Reg("Outra"), 3));

            var spec = new RegionQuotaSpecification(DefaultConfigs(retreatId));

            Assert.True(spec.IsSatisfiedBy(regs));
        }

        // -------- caso FAIL: Oeste 5/10 (50 %) – abaixo de 70 %
        [Fact]
        public void Should_Return_False_When_Region_Targets_Are_Not_Met()
        {
            var retreatId = Guid.NewGuid();
            var regs = new List<Registration>();

            regs.AddRange(Enumerable.Repeat(Reg("Oeste"), 5));
            regs.AddRange(Enumerable.Repeat(Reg("Outra"), 5));

            var spec = new RegionQuotaSpecification(DefaultConfigs(retreatId));

            Assert.False(spec.IsSatisfiedBy(regs));
        }
    }
}
