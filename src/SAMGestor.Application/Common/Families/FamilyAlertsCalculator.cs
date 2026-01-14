using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Common.Families;

public static class FamilyAlertsCalculator
{
    public sealed record AlertDto(
        string Severity,
        string Code,
        string Message,
        IReadOnlyList<Guid> RegistrationIds
    );
    
    public static List<AlertDto> Calculate(
        IEnumerable<Registration> members, 
        int? expectedCapacity = null)
    {
        var alerts = new List<AlertDto>();
        var membersList = members.ToList();

        alerts.AddRange(CheckSameSurname(membersList));
        alerts.AddRange(CheckSameCity(membersList));
        alerts.AddRange(CheckPossibleRelatives(membersList));
        alerts.AddRange(CheckGenderBalance(membersList));

        if (membersList.Count % 2 != 0)
        {
            alerts.Add(new AlertDto(
                "info",
                "ODD_MEMBER_COUNT",
                $"Família possui número ímpar de membros ({membersList.Count}). Recomendado número par.",
                Array.Empty<Guid>()
            ));
        }
        
        if (expectedCapacity.HasValue && membersList.Count != expectedCapacity.Value)
        {
            alerts.Add(new AlertDto(
                "info",
                "DIFFERENT_FAMILY_SIZE",
                $"Esta família tem {membersList.Count} membros, mas a capacidade definida é {expectedCapacity.Value}.",
                Array.Empty<Guid>()
            ));
        }

        return alerts;
    }
    
    public static List<AlertDto> CalculateWithGodparents(
        IEnumerable<Registration> members,
        IEnumerable<Guid> padrinhoIds,
        IEnumerable<Guid> madrinhaIds,
        int? expectedCapacity = null)
    {
        var alerts = Calculate(members, expectedCapacity);
        var membersList = members.ToList();
        var padrinhos = padrinhoIds.ToList();
        var madrinhas = madrinhaIds.ToList();
        
        if (padrinhos.Count < 2 || madrinhas.Count < 2)
        {
            if (padrinhos.Count == 0 && madrinhas.Count == 0)
            {
                alerts.Add(new AlertDto(
                    "critical",
                    "MISSING_GODPARENTS",
                    "Família não possui padrinhos nem madrinhas definidos. Recomendado: 2 padrinhos e 2 madrinhas.",
                    Array.Empty<Guid>()
                ));
            }
            else if (padrinhos.Count < 2 && madrinhas.Count < 2)
            {
                alerts.Add(new AlertDto(
                    "critical",
                    "MISSING_GODPARENTS",
                    $"Família precisa ter 2 padrinhos e 2 madrinhas. Atual: {padrinhos.Count} padrinho(s), {madrinhas.Count} madrinha(s).",
                    Array.Empty<Guid>()
                ));
            }
            else if (padrinhos.Count < 2)
            {
                alerts.Add(new AlertDto(
                    "warning",
                    "MISSING_GODPARENTS",
                    $"Família tem apenas {padrinhos.Count} padrinho(s). Recomendado: 2.",
                    Array.Empty<Guid>()
                ));
            }
            else if (madrinhas.Count < 2)
            {
                alerts.Add(new AlertDto(
                    "warning",
                    "MISSING_GODPARENTS",
                    $"Família tem apenas {madrinhas.Count} madrinha(s). Recomendado: 2.",
                    Array.Empty<Guid>()
                ));
            }
        }

        return alerts;
    }

    public static List<AlertDto> CheckFamilySizeComparison(
        int currentFamilySize,
        List<int> otherFamilySizes,
        IReadOnlyList<Guid> currentMemberIds)
    {
        var alerts = new List<AlertDto>();

        if (otherFamilySizes.Count == 0)
            return alerts;

        var avgSize = otherFamilySizes.Average();
        var minSize = otherFamilySizes.Min();
        var maxSize = otherFamilySizes.Max();

        var diffFromAvg = Math.Abs(currentFamilySize - avgSize);

        if (currentFamilySize < minSize)
        {
            alerts.Add(new AlertDto(
                "warning",
                "SMALLEST_FAMILY",
                $"Esta família tem {currentFamilySize} membros, sendo a menor do retiro (outras têm entre {minSize}-{maxSize}).",
                Array.Empty<Guid>()
            ));
        }
        else if (currentFamilySize > maxSize)
        {
            alerts.Add(new AlertDto(
                "warning",
                "LARGEST_FAMILY",
                $"Esta família tem {currentFamilySize} membros, sendo a maior do retiro (outras têm entre {minSize}-{maxSize}).",
                Array.Empty<Guid>()
            ));
        }
        else if (diffFromAvg > 2)
        {
            alerts.Add(new AlertDto(
                "info",
                "SIZE_DIFFERS_FROM_AVERAGE",
                $"Esta família tem {currentFamilySize} membros, média do retiro é {avgSize:F1}.",
                Array.Empty<Guid>()
            ));
        }

        return alerts;
    }

    private static List<AlertDto> CheckSameSurname(List<Registration> members)
    {
        var alerts = new List<AlertDto>();

        var surnames = members
            .Where(r => r.Name != null)
            .Select(r => new { Reg = r, Surname = NormalizeSurname(ExtractLastName((string)r.Name)) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Surname))
            .GroupBy(x => x.Surname)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var g in surnames)
        {
            alerts.Add(new AlertDto(
                "critical",
                "SAME_SURNAME",
                $"Sobrenome repetido na família: '{g.Key}'.",
                g.Select(x => x.Reg.Id).ToList()
            ));
        }

        return alerts;
    }

    private static List<AlertDto> CheckSameCity(List<Registration> members)
    {
        var alerts = new List<AlertDto>();

        var cities = members
            .Select(r => new { Reg = r, City = NormalizeCity(r.City) })
            .Where(x => !string.IsNullOrWhiteSpace(x.City))
            .GroupBy(x => x.City)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var g in cities)
        {
            alerts.Add(new AlertDto(
                "warning",
                "SAME_CITY",
                $"Múltiplos membros da mesma cidade: '{g.Key}'.",
                g.Select(x => x.Reg.Id).ToList()
            ));
        }

        return alerts;
    }

    private static List<AlertDto> CheckPossibleRelatives(List<Registration> members)
    {
        var alerts = new List<AlertDto>();

        var withRelativesFlag = members
            .Where(r => r.HasRelativeOrFriendSubmitted == true && !string.IsNullOrWhiteSpace(r.SubmitterNames))
            .ToList();

        if (withRelativesFlag.Count > 0)
        {
            var matchedPairs = new HashSet<Guid>();

            foreach (var person in withRelativesFlag)
            {
                var submitterNames = person.SubmitterNames!
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => NormalizeName(n))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList();

                foreach (var otherPerson in members)
                {
                    if (person.Id == otherPerson.Id) continue;

                    var otherName = NormalizeName((string)otherPerson.Name);

                    if (submitterNames.Any(submitter => 
                        otherName.Contains(submitter) || submitter.Contains(otherName)))
                    {
                        matchedPairs.Add(person.Id);
                        matchedPairs.Add(otherPerson.Id);
                    }
                }
            }

            if (matchedPairs.Count > 0)
            {
                alerts.Add(new AlertDto(
                    "warning",
                    "DECLARED_RELATIVES",
                    "Possíveis parentes/amigos na mesma família (nomes declarados coincidem).",
                    matchedPairs.ToList()
                ));
            }
        }
        
        var withParents = members
            .Where(r => !string.IsNullOrWhiteSpace(r.FatherName) || !string.IsNullOrWhiteSpace(r.MotherName))
            .ToList();

        if (withParents.Count > 1)
        {
            var potentialRelatives = new HashSet<Guid>();

            for (int i = 0; i < withParents.Count; i++)
            {
                for (int j = i + 1; j < withParents.Count; j++)
                {
                    var r1 = withParents[i];
                    var r2 = withParents[j];

                    var father1 = r1.FatherName?.Trim().ToLowerInvariant();
                    var father2 = r2.FatherName?.Trim().ToLowerInvariant();
                    var mother1 = r1.MotherName?.Trim().ToLowerInvariant();
                    var mother2 = r2.MotherName?.Trim().ToLowerInvariant();

                    if ((!string.IsNullOrWhiteSpace(father1) && father1 == father2) ||
                        (!string.IsNullOrWhiteSpace(mother1) && mother1 == mother2))
                    {
                        potentialRelatives.Add(r1.Id);
                        potentialRelatives.Add(r2.Id);
                    }
                }
            }

            if (potentialRelatives.Count > 0)
            {
                alerts.Add(new AlertDto(
                    "critical",
                    "SAME_PARENT_NAMES",
                    "Possíveis parentes na mesma família (mesmo nome de pai/mãe).",
                    potentialRelatives.ToList()
                ));
            }
        }

        return alerts;
    }

    private static List<AlertDto> CheckGenderBalance(List<Registration> members)
    {
        var alerts = new List<AlertDto>();

        var maleCount = members.Count(r => r.Gender == Gender.Male);
        var femaleCount = members.Count - maleCount;
        var malePercent = members.Count > 0 ? (maleCount * 100.0m / members.Count) : 0;

        if (Math.Abs(malePercent - 50) > 0.01m)
        {
            alerts.Add(new AlertDto(
                "warning",
                "GENDER_IMBALANCE",
                $"Composição de gênero: {maleCount} homens ({malePercent:F1}%) e {femaleCount} mulheres ({100 - malePercent:F1}%). Ideal: 50%/50%.",
                Array.Empty<Guid>()
            ));
        }

        return alerts;
    }

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    private static string NormalizeSurname(string surname)
    {
        if (string.IsNullOrWhiteSpace(surname)) return string.Empty;
        var s = surname.Trim().ToLowerInvariant();
        if (s is "de" or "da" or "do" or "dos" or "das") return string.Empty;
        return s;
    }

    private static string NormalizeCity(string? city)
    {
        return (city ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        return name.Trim().ToLowerInvariant();
    }
}
