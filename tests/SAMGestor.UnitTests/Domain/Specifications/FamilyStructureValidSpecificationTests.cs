using System;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class FamilyStructureValidSpecificationTests
    {
        private static Family CreateFamily(int godfathers, int godmothers)
        {
            return new Family(
                new FullName("Família Luz"),
                godfathers,
                godmothers,
                Guid.NewGuid(),   
                10);              
        }

        [Fact]
        public void Should_Return_True_When_Has_At_Least_Two_Godparents_Of_Each_Type()
        {
            
            var family = CreateFamily(2, 2);
            var spec   = new FamilyStructureValidSpecification();

          
            var ok = spec.IsSatisfiedBy(family);

            
            Assert.True(ok);
        }

        [Fact]
        public void Should_Return_False_When_Godfather_Or_Godmother_Count_Is_Less_Than_Two()
        {
            
            var familyFewGodfathers = CreateFamily(1, 2);
            var spec = new FamilyStructureValidSpecification();
            
            Assert.False(spec.IsSatisfiedBy(familyFewGodfathers));
            
            var familyFewGodmothers = CreateFamily(2, 1);
            
            Assert.False(spec.IsSatisfiedBy(familyFewGodmothers));
        }
    }
}