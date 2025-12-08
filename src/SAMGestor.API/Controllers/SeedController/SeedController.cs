using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.API.Controllers.SeedController;

/// <summary> DEV APENAS </summary>
[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public SeedController(
        IUserRepository users,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _users = users;
        _hasher = hasher;
        _clock = clock;
        _uow = uow;
    }
    
    /// <summary> Cria usuários iniciais para testes </summary>

    [HttpPost("initial-users")]
    public async Task<IActionResult> CreateInitialUsers(CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var created = new List<object>();

        // Admin
        if (await _users.GetByEmailAsync("admin@samgestor.local", ct) == null)
        {
            var admin = new User(
                new FullName("Admin Sistema"),
                new EmailAddress("admin@samgestor.local"),
                "11999999001",
                new PasswordHash(_hasher.Hash("Admin@2025")),
                UserRole.Administrator
            );
            admin.ConfirmEmail(now);
            await _users.AddAsync(admin, ct);
            created.Add(new { email = "admin@samgestor.local", password = "Admin@2025", role = "Administrator" });
        }

        // Manager
        if (await _users.GetByEmailAsync("manager@samgestor.local", ct) == null)
        {
            var manager = new User(
                new FullName("Manager Teste"),
                new EmailAddress("manager@samgestor.local"),
                "11999999002",
                new PasswordHash(_hasher.Hash("Manager@2025")),
                UserRole.Manager
            );
            manager.ConfirmEmail(now);
            await _users.AddAsync(manager, ct);
            created.Add(new { email = "manager@samgestor.local", password = "Manager@2025", role = "Manager" });
        }

        // Consultant
        if (await _users.GetByEmailAsync("consultant@samgestor.local", ct) == null)
        {
            var consultant = new User(
                new FullName("Consultor Teste"),
                new EmailAddress("consultant@samgestor.local"),
                "11999999003",
                new PasswordHash(_hasher.Hash("Consultant@2025")),
                UserRole.Consultant
            );
            consultant.ConfirmEmail(now);
            await _users.AddAsync(consultant, ct);
            created.Add(new { email = "consultant@samgestor.local", password = "Consultant@2025", role = "Consultant" });
        }

        await _uow.SaveChangesAsync(ct);

        return Ok(new { message = $"{created.Count} usuários criados", users = created });
    }
    
    /// <summary> Remove usuários de teste </summary>
    [HttpDelete("initial-users")]
    public async Task<IActionResult> DeleteInitialUsers(CancellationToken ct)
    {
        var testEmails = new[]
        {
            "admin@samgestor.local",
            "manager@samgestor.local",
            "consultant@samgestor.local"
        };

        var deleted = 0;
        foreach (var email in testEmails)
        {
            var user = await _users.GetByEmailAsync(email, ct);
            if (user != null)
            {
                await _users.DeleteAsync(user, ct);
                deleted++;
            }
        }

        await _uow.SaveChangesAsync(ct);

        return Ok(new { message = $"{deleted} usuários removidos" });
    }


}
