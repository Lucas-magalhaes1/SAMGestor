using System;
using Moq;
using Xunit;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications
{
    public class TeamCapacitySpecificationTests
    {
        private static Team CreateTeam(int memberLimit)
        {
            return new Team(
                new FullName("Equipe Apoio"),
                "Equipe de apoio logístico",
                0,               
                memberLimit,
                Guid.NewGuid()); 
        }

        [Fact]
        public void Should_Return_True_When_Occupancy_Is_Below_Limit()
        {
            var team = CreateTeam(10);

            var repoMock = new Mock<ITeamRepository>();
            repoMock.Setup(r => r.GetOccupancy(team.Id)).Returns(7);

            var spec = new TeamCapacitySpecification(repoMock.Object);
            
            var ok = spec.IsSatisfiedBy(team);
            
            Assert.True(ok);
        }

        [Fact]
        public void Should_Return_False_When_Occupancy_Reaches_Or_Exceeds_Limit()
        {
            var team = CreateTeam(10);

            var repoMock = new Mock<ITeamRepository>();
            repoMock.Setup(r => r.GetOccupancy(team.Id)).Returns(10);

            var spec = new TeamCapacitySpecification(repoMock.Object);
            
            var ok = spec.IsSatisfiedBy(team);
            
            Assert.False(ok);
        }
    }
}