using System.Reflection;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Domain.Specifications;

public class TeamMemberRoleRequiredSpecificationTests
{
    private static TeamMember CreateMember(TeamMemberRole role)
    {
        var team = new Team(
            new FullName("Equipe Música"),
            "Equipe do louvor",
            0,
            10,
            Guid.NewGuid());

        // Obtém o ctor não-público (Team, Guid, TeamMemberRole)
        var ctor = typeof(TeamMember).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[] { typeof(Team), typeof(Guid), typeof(TeamMemberRole) },
            modifiers: null)!;

        return (TeamMember)ctor.Invoke(new object[] { team, Guid.NewGuid(), role });
    }

    [Fact]
    public void Should_Return_True_For_Valid_Role()
    {
        var member = CreateMember(TeamMemberRole.Coordinator);
        var spec   = new TeamMemberRoleRequiredSpecification();

        Assert.True(spec.IsSatisfiedBy(member));
    }

    [Fact]
    public void Should_Return_False_For_Invalid_Role()
    {
        var invalidRole = (TeamMemberRole)999;           
        var member      = CreateMember(invalidRole);

        var spec = new TeamMemberRoleRequiredSpecification();

        Assert.False(spec.IsSatisfiedBy(member));
    }
}