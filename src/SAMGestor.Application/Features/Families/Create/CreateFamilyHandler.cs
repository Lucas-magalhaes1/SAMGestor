using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Families.Create;

public sealed class CreateFamilyHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository fmRepo,
    IRegistrationRepository regRepo,
    IUnitOfWork uow
) : IRequestHandler<CreateFamilyCommand, CreateFamilyResult>
{
    private const int FixedCapacity = 4;

    public async Task<CreateFamilyResult> Handle(CreateFamilyCommand cmd, CancellationToken ct)
    {
        // 1) Retiro & lock
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        // 2) Validar lista de membros
        if (cmd.MemberIds is null || cmd.MemberIds.Count != FixedCapacity)
            throw new BusinessRuleException($"É necessário informar exatamente {FixedCapacity} membros.");

        if (cmd.MemberIds.Distinct().Count() != cmd.MemberIds.Count)
            throw new BusinessRuleException("Lista de membros contém IDs duplicados.");

        // 3) Carregar regs e validar estado
        var regsMap = await regRepo.GetMapByIdsAsync(cmd.MemberIds, ct);
        var notFound = cmd.MemberIds.Where(id => !regsMap.ContainsKey(id)).ToList();
        if (notFound.Count > 0)
            throw new NotFoundException("Registration", notFound.First());

        foreach (var r in regsMap.Values)
        {
            if (r.RetreatId != cmd.RetreatId)
                throw new BusinessRuleException("Todos os membros devem pertencer ao mesmo retiro.");

            if (!r.Enabled)
                throw new BusinessRuleException($"Participante '{r.Name}' está desabilitado.");

            if (r.Status is not RegistrationStatus.Confirmed and not RegistrationStatus.PaymentConfirmed)
                throw new BusinessRuleException($"Participante '{r.Name}' não está confirmado/pago.");
        }

        // 4) Ninguém pode já estar alocado
        var existingLinks = await fmRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var alreadyAssigned = existingLinks.Select(x => x.RegistrationId).ToHashSet();
        var conflict = cmd.MemberIds.Where(id => alreadyAssigned.Contains(id)).ToList();
        if (conflict.Count > 0)
            throw new BusinessRuleException("Um ou mais membros já estão alocados em outra família.");

        // 5) Composição 2M+2F
        var males   = regsMap.Values.Count(r => r.Gender == Gender.Male);
        var females = regsMap.Values.Count - males;
        if (males != 2 || females != 2)
            throw new BusinessRuleException("Composição inválida: a família deve ter 2 homens e 2 mulheres.");

        // 6) Alerts (warnings/erros)
        var warnings = new List<CreateFamilyWarningDto>();

        // SAME_SURNAME (tratado como ERRO no MVP)
        var surnames = regsMap.Values
            .Select(r => r.Name.Last.Trim().ToLowerInvariant())
            .GroupBy(s => s)
            .Where(g => g.Count() > 1)
            .ToList();

        if (surnames.Count > 0)
        {
            var ids = regsMap.Values
                .Where(r => surnames.Select(s => s.Key).Contains(r.Name.Last.Trim().ToLowerInvariant()))
                .Select(r => r.Id)
                .ToList();

            // No MVP seguimos tratando como erro crítico
            throw new BusinessRuleException($"Sobrenome repetido na mesma família: '{surnames.First().Key}'.");
        }

        // SAME_CITY (warning – pode ser ignorado com IgnoreWarnings)
        var cities = regsMap.Values
            .Select(r => (r.Id, City: (r.City ?? string.Empty).Trim().ToLowerInvariant()))
            .GroupBy(x => x.City)
            .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        foreach (var g in cities)
        {
            warnings.Add(new CreateFamilyWarningDto(
                Severity: "warning",
                Code: "SAME_CITY",
                Message: $"Múltiplos membros da mesma cidade: '{g.Key}'.",
                RegistrationIds: g.Select(x => x.Id).ToList()
            ));
        }

        // 7) Se houver warnings e IgnoreWarnings=false => retornar 422-like (Created=false)
        if (warnings.Count > 0 && !cmd.IgnoreWarnings)
        {
            return new CreateFamilyResult(
                Created: false,
                FamilyId: null,
                Version: retreat.FamiliesVersion,
                Warnings: warnings
            );
        }

        // 8) Gerar nome, se necessário (Família N)
        var name = (cmd.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = await GenerateNextFamilyNameAsync(cmd.RetreatId, ct);
        }

        // 9) Persistir Family + FamilyMembers
        var family = new Family(new (name), cmd.RetreatId, FixedCapacity);
        await familyRepo.AddAsync(family, ct);

        var ordered = regsMap.Values
            .OrderBy(r => r.Gender)               // apenas para fixar uma ordem estável (M/F não importa)
            .ThenBy(r => r.Name.Value)
            .ToList();

        var links = ordered.Select((r, idx) =>
            new FamilyMember(cmd.RetreatId, family.Id, r.Id, position: idx)
        ).ToList();

        await fmRepo.AddRangeAsync(links, ct);

        // 10) bump versão + commit
        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        return new CreateFamilyResult(
            Created: true,
            FamilyId: family.Id,
            Version: retreat.FamiliesVersion,
            Warnings: warnings
        );
    }

    private async Task<string> GenerateNextFamilyNameAsync(Guid retreatId, CancellationToken ct)
    {
        var families = await familyRepo.ListByRetreatAsync(retreatId, ct);
        // pega todos os números já usados no padrão "Família N"
        var used = new HashSet<int>();
        foreach (var f in families)
        {
            var value = ((string)f.Name).Trim();
            if (value.StartsWith("Família ", StringComparison.OrdinalIgnoreCase))
            {
                var tail = value["Família ".Length..].Trim();
                if (int.TryParse(tail, out var n) && n > 0) used.Add(n);
            }
        }

        // menor número positivo livre
        var i = 1;
        while (used.Contains(i)) i++;
        return $"Família {i}";
    }
}
