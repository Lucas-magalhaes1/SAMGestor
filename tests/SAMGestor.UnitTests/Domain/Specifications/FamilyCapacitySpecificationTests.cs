using System;
using System.Collections.Generic;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class FamilyCapacitySpecificationTests
    {
        
        private static long _seedCpf = 10000000000;
        private static CPF NextCpf() => new((_seedCpf++).ToString());
        
        private static Registration Reg(string name)
        {
            return new Registration(
                new FullName(name),
                NextCpf(),
                new EmailAddress($"{name.Split(' ')[0]}@mail.com"),
                "11998887766",
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
                Gender.Male,
                "Campinas",
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                Guid.NewGuid());
        }

        
        private static Family CreateFamily(int limit, params Registration[] members)
        {
            var fam = new Family(
                new FullName("Família Teste"),
                2, 2, Guid.NewGuid(), limit);

            
            var field = typeof(Family).GetField("_members",
                         System.Reflection.BindingFlags.NonPublic |
                         System.Reflection.BindingFlags.Instance)!;

            field.SetValue(fam, new List<Registration>(members));
            return fam;
        }

        [Fact]
        public void Should_Return_True_When_Member_Count_Is_Below_Limit()
        {
           
            var family = CreateFamily(5, Reg("Paulo Silva"), Reg("Marcia Silva"), Reg("João Souza"));
            var spec   = new FamilyCapacitySpecification();

            Assert.True(spec.IsSatisfiedBy(family));
        }

        [Fact]
        public void Should_Return_False_When_Member_Count_Reaches_Limit()
        {
            var family = CreateFamily(4,
                Reg("Ana Silva"), Reg("Bruno Lima"), Reg("Clara Souza"), Reg("Diego Costa"));


            var spec = new FamilyCapacitySpecification();
            
            Assert.False(spec.IsSatisfiedBy(family));
        }
    }
}
