using MediatR;
using SAMGestor.Application.Interfaces;

namespace SAMGestor.Application.Features.Dev.Seed;

public class ClearSeedDataHandler : IRequestHandler<ClearSeedDataCommand, ClearSeedDataResult>
{
    private readonly IRawSqlExecutor _sqlExecutor;

    public ClearSeedDataHandler(IRawSqlExecutor sqlExecutor)
    {
        _sqlExecutor = sqlExecutor;
    }

    public async Task<ClearSeedDataResult> Handle(ClearSeedDataCommand cmd, CancellationToken ct)
    {
        // ✅ ORDEM CORRETA COM NOMES EXATOS DO BANCO

        // 1️⃣ Deletar service_assignments (FK: service_space_id, service_registration_id - snake_case)
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_assignments 
              WHERE service_registration_id IN (
                  SELECT ""Id"" FROM core.service_registrations 
                  WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
              )",
            ct
        );

        // 2️⃣ Deletar service_registrations (FK: retreat_id - snake_case)
        var serviceRegsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_registrations 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );

        // 3️⃣ Deletar service_spaces (FK: RetreatId - PascalCase! ← ÚNICO)
        var serviceSpacesDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_spaces 
              WHERE ""RetreatId"" IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );

        // 4️⃣ Deletar tent_assignments (FK: registration_id - snake_case)
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.tent_assignments 
              WHERE registration_id IN (
                  SELECT ""Id"" FROM core.registrations 
                  WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
              )",
            ct
        );

        // 5️⃣ Deletar family_members (FK: family_id - snake_case)
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.family_members 
              WHERE family_id IN (
                  SELECT ""Id"" FROM core.families 
                  WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
              )",
            ct
        );

        // 6️⃣ Deletar families (FK: retreat_id - snake_case)
        var familiesDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.families 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );

        // 7️⃣ Deletar tents (FK: retreat_id - snake_case)
        var tentsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.tents 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.manual_payment_proofs 
      WHERE registration_id IN (
          SELECT ""Id"" FROM core.registrations 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
      )",
            ct
        );

        // 8️⃣ Deletar registrations (FK: retreat_id - snake_case)
        var registrationsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.registrations 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
        var reportsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.reports 
      WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        

        // 9️⃣ Deletar retreats (raiz)
        var retreatsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.retreats WHERE name LIKE '%[SEED]%'",
            ct
        );

        return new ClearSeedDataResult
        {
            Success = true,
            Message = "Seed data cleared successfully (FAZER + SERVIR)",
            RetreatsDeleted = retreatsDeleted,
            RegistrationsDeleted = registrationsDeleted,
            ServiceRegistrationsDeleted = serviceRegsDeleted,
            ServiceSpacesDeleted = serviceSpacesDeleted,
            FamiliesDeleted = familiesDeleted,
            TentsDeleted = tentsDeleted,
            ReportsDeleted = reportsDeleted
        };
    }
}
