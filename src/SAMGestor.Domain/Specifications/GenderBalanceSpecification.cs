using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Specifications;

/// <summary>
/// Garante que a diferença entre homens e mulheres na família
/// seja, no máximo, 1 (≈ 50 % de cada gênero).
/// </summary>
public sealed class GenderBalanceSpecification : ISpecification<Family>
{
    public bool IsSatisfiedBy(Family family)
    {
        var maleCount   = family.Members.Count(m => m.Gender == Enums.Gender.Male);
        var femaleCount = family.Members.Count - maleCount;
        return Math.Abs(maleCount - femaleCount) <= 1;
    }
}