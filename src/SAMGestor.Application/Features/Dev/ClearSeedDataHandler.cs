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
        
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.manual_payment_proofs 
          WHERE service_registration_id IN (
              SELECT ""Id"" FROM core.service_registrations 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
          )",
            ct
        );

        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_assignments 
          WHERE service_registration_id IN (
              SELECT ""Id"" FROM core.service_registrations 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
          )",
            ct
        );
        
        var serviceRegsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_registrations 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
        var serviceSpacesDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.service_spaces 
          WHERE ""RetreatId"" IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.tent_assignments 
          WHERE registration_id IN (
              SELECT ""Id"" FROM core.registrations 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
          )",
            ct
        );
        
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.family_members 
          WHERE family_id IN (
              SELECT ""Id"" FROM core.families 
              WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')
          )",
            ct
        );
        
        var familiesDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.families 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
        var tentsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.tents 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );
        
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
