using Moq;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class TentCapacitySpecificationTests
    {
        private static Tent CreateTent(int number, int capacity)
        {
            return new Tent(
                new TentNumber(number),
                TentCategory.Male,
                capacity,
                Guid.NewGuid());         
        }

        [Fact]
        public void Should_Return_True_When_Occupancy_Is_Below_Capacity()
        {
            
            var tent = CreateTent(1, 4);     

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(r => r.GetOccupancy(tent.Id)).Returns(3); 

            var spec = new TentCapacitySpecification(tentRepo.Object);
            
            var ok = spec.IsSatisfiedBy(tent);
            
            Assert.True(ok);
        }

        [Fact]
        public void Should_Return_False_When_Occupancy_Is_At_Or_Above_Capacity()
        {
            var tent = CreateTent(1, 4);

            var tentRepo = new Mock<ITentRepository>();
            tentRepo.Setup(r => r.GetOccupancy(tent.Id)).Returns(4); 

            var spec = new TentCapacitySpecification(tentRepo.Object);

            Assert.False(spec.IsSatisfiedBy(tent));
        }
    }
}