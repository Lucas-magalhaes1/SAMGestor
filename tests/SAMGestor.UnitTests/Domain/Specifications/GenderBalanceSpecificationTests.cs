using System.Reflection;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Domain.Specifications;

public class GenderBalanceSpecificationTests
{
    private static long _cpfSeed = 20000000000;
    private static CPF NextCpf() => new((_cpfSeed++).ToString());

    private static Registration Reg(string name, Gender gender)
    {
        return new Registration(
            new FullName(name),
            NextCpf(),
            new EmailAddress($"{name.Split(' ')[0]}@mail.com"),
            "11990000000",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-24)),
            gender,
            "Campinas",
            RegistrationStatus.NotSelected,
            ParticipationCategory.Guest,
            "Oeste",
            Guid.NewGuid());
    }

    private static Family CreateFamilyWithMembers(IEnumerable<Registration> members)
    {
        var fam = new Family(
            new FullName("Família Teste"),
            2, 2, Guid.NewGuid(), 10);

        var field = typeof(Family).GetField("_members",
                     BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(fam, new List<Registration>(members));

        return fam;
    }
    
    [Fact]
    public void Should_Return_True_When_Gender_Difference_Is_At_Most_One()
    {
        // 3 homens, 2 mulheres → diferença = 1  
        var members = new[]
        {
            Reg("A Silva", Gender.Male),
            Reg("B Lima",  Gender.Male),
            Reg("C Costa", Gender.Male),
            Reg("D Souza", Gender.Female),
            Reg("E Souza", Gender.Female)
        };

        var family = CreateFamilyWithMembers(members);
        var spec   = new GenderBalanceSpecification();

        Assert.True(spec.IsSatisfiedBy(family));
    }

    [Fact]
    public void Should_Return_False_When_Gender_Difference_Exceeds_One()
    {
        // 5 homens, 1 mulher → diferença = 4  
        var members = new[]
        {
            Reg("H1 Silva", Gender.Male),
            Reg("H2 Silva", Gender.Male),
            Reg("H3 Silva", Gender.Male),
            Reg("H4 Silva", Gender.Male),
            Reg("H5 Silva", Gender.Male),
            Reg("M1 Souza", Gender.Female)
        };

        var family = CreateFamilyWithMembers(members);
        var spec   = new GenderBalanceSpecification();

        Assert.False(spec.IsSatisfiedBy(family));
    }
}
