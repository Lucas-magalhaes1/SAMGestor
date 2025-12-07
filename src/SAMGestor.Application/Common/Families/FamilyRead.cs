    using SAMGestor.Domain.Enums;

    namespace SAMGestor.Application.Common.Families;

    public static class FamilyRead
    {
        public sealed record MemberView(Guid RegistrationId, string Name, Gender Gender, string City, int Position);
        public sealed record AlertView(string Severity, string Code, string Message, IReadOnlyList<Guid> RegistrationIds);

        public static (int total, int male, int female, int remaining) Metrics(int capacity, IReadOnlyList<MemberView> members)
        {
            var male   = members.Count(m => m.Gender == Gender.Male);
            var total  = members.Count;
            var female = total - male;
            var remaining = Math.Max(0, capacity - total);
            return (total, male, female, remaining);
        }

        public static List<AlertView> Alerts(IReadOnlyList<MemberView> members)
        {
            var alerts = new List<AlertView>();

            
            var groupsBySurname = members
                .Select(m => new { m, Last = ExtractLastName(m.Name) })
                .GroupBy(x => NormalizeSurname(x.Last))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

            foreach (var g in groupsBySurname)
            {
                alerts.Add(new AlertView(
                    "critical",
                    "SAME_SURNAME",
                    $"Sobrenome repetido na família: '{g.Key}'.",
                    g.Select(x => x.m.RegistrationId).ToList()
                ));
            }
            
            var groupsByCity = members
                .GroupBy(m => (m.City ?? string.Empty).Trim().ToLowerInvariant())
                .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1);

            foreach (var g in groupsByCity)
            {
                alerts.Add(new AlertView(
                    "warning",
                    "SAME_CITY",
                    $"Múltiplos membros da mesma cidade: '{g.Key}'.",
                    g.Select(x => x.RegistrationId).ToList()
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
    }
