using Moq;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Enums;

 
namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class NoRelativesTogetherSpecificationTests
    {
        private static long _cpfSeed = 10000000000;    

        private static Registration Reg(string fullName, string city, string phone)
        {
            var cpfDigits = (_cpfSeed++).ToString();   // "10000000000", "10000000001", ...
    
            return new Registration(
                new FullName(fullName),
                new CPF(cpfDigits),
                new EmailAddress($"{fullName.Split(' ')[0]}@mail.com"),
                phone,
                DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Gender.Female,
                city,
                RegistrationStatus.NotSelected,
                ParticipationCategory.Guest,
                "Oeste",
                Guid.NewGuid());
        }

        
        private static Family CreateFamily(params Registration[] members)
        {
            var fam = new Family(
                new FullName("Família Teste"),
                2, 2, Guid.NewGuid(), 10);
            
            var field = typeof(Family).GetField("_members", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field!.SetValue(fam, members.ToList());

            return fam;
        }

        
        [Fact]
        public void Should_Return_True_When_No_Relatives_In_Family()
        {
            var r1 = Reg("Ana Souza",   "Campinas", "1199001000");
            var r2 = Reg("Bruno Lima",  "Campinas", "1199002000");

            var family = CreateFamily(r1, r2);

            var relMock = new Mock<IRelationshipService>();
            relMock.Setup(r => r.AreSpouses(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);
            relMock.Setup(r => r.AreDirectRelatives(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(false);

            var spec = new NoRelativesTogetherSpecification(relMock.Object);
            
            Assert.True(spec.IsSatisfiedBy(family));
        }

        [Fact]
        public void Should_Return_False_When_Relatives_Found_In_Family()
        {
            var r1 = Reg("Carlos Silva", "Campinas", "1199003000");
            var r2 = Reg("Mariana Silva", "Campinas", "1199004000");

            var family = CreateFamily(r1, r2);

            var relMock = new Mock<IRelationshipService>();
            
            relMock.Setup(r => r.AreDirectRelatives(r1.Id, r2.Id)).Returns(true);

            var spec = new NoRelativesTogetherSpecification(relMock.Object);

            Assert.False(spec.IsSatisfiedBy(family));
        }
    }
}
