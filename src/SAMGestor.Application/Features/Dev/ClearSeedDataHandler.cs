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

   
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.families 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );

       
        await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.tents 
          WHERE retreat_id IN (SELECT ""Id"" FROM core.retreats WHERE name LIKE '%[SEED]%')",
            ct
        );

        
        var registrationsDeleted = await _sqlExecutor.ExecuteSqlAsync(
            @"DELETE FROM core.registrations 
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
            Message = "Seed data cleared successfully",
            RetreatsDeleted = retreatsDeleted,
            RegistrationsDeleted = registrationsDeleted,
            FamiliesDeleted = 0,
            TentsDeleted = 0
        };
    }
}