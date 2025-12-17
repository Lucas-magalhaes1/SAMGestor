using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Domain.Entities;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public sealed class UsersAdminSecurityEffectsE2EIntegrationTests(UsersWebAppFactory factory)
    : IClassFixture<UsersWebAppFactory>
{
    private readonly UsersWebAppFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Users_AdminSecurity_EfeitosDevemPersistir_RevogaRefreshTokens_EmailSenhaBlockUnblock()
    {
        // 1) CREATE user
        var email = NewEmail();
        var createBody = new
        {
            name = "User Test",
            email,
            phone = "11999999999",
            role = 2 // Consultant
        };

        var create = await _client.PostAsJsonAsync("/api/users", createBody);
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());

        var userId = ReadIdFromCreatedBody(await create.Content.ReadAsStringAsync());

        // 2) Seed de refresh tokens ativos (simula sessões existentes)
        var now = DateTimeOffset.UtcNow;

        await WithDb(async db =>
        {
            var t1 = CreateRefreshTokenBestEffort(userId, now.AddDays(10));
            var t2 = CreateRefreshTokenBestEffort(userId, now.AddDays(20));
            await db.RefreshTokens.AddRangeAsync(t1, t2);
            await db.SaveChangesAsync();
        });

        // sanity: existem 2 tokens ativos
        await WithDb(async db =>
        {
            var active = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
                .CountAsync();

            active.Should().Be(2);
        });

        // 3) FORCE CHANGE PASSWORD -> deve revogar tokens + trocar hash + limpar lockout
        var beforeHash = await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();
            return ReadHash(u!.PasswordHash);
        });

        var fcp = await _client.PostAsJsonAsync(
            $"/api/users/{userId}/force-change-password",
            new { newPassword = "NewP@ss123!" });

        fcp.StatusCode.Should().Be(HttpStatusCode.OK, await fcp.Content.ReadAsStringAsync());

        await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();

            // hash mudou
            ReadHash(u!.PasswordHash).Should().NotBe(beforeHash);

            // lockout/contador resetados (pela MarkLoginSuccess)
            u.FailedAccessCount.Should().Be(0);
            u.LockoutEndAt.Should().BeNull();

            // todos refresh tokens ativos devem ter sido revogados
            var active = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
                .CountAsync();

            active.Should().Be(0, "force-change-password revoga todas as sessões ativas");
        });

        // 4) SEED mais 1 token ativo para testar revogação no BLOCK também
        await WithDb(async db =>
        {
            var t3 = CreateRefreshTokenBestEffort(userId, DateTimeOffset.UtcNow.AddDays(30));
            await db.RefreshTokens.AddAsync(t3);
            await db.SaveChangesAsync();
        });

        // 5) BLOCK -> Enabled=false e revoga tokens ativos
        var block = await _client.PostAsync($"/api/users/{userId}/block", content: null);
        block.StatusCode.Should().Be(HttpStatusCode.OK, await block.Content.ReadAsStringAsync());

        await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();
            u!.Enabled.Should().BeFalse();

            var active = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
                .CountAsync();

            active.Should().Be(0, "block revoga todas as sessões ativas");
        });

        // 6) UNBLOCK -> Enabled=true (não “desrevoga” token)
        var unblock = await _client.PostAsync($"/api/users/{userId}/unblock", content: null);
        unblock.StatusCode.Should().Be(HttpStatusCode.OK, await unblock.Content.ReadAsStringAsync());

        await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();
            u!.Enabled.Should().BeTrue();

            var active = await db.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow)
                .CountAsync();

            active.Should().Be(0, "unblock não deve reativar sessões antigas");
        });

        // 7) FORCE CHANGE EMAIL -> troca email + emailConfirmed=false + cria token confirmação
        var newEmail = NewEmail("new");
        var fce = await _client.PostAsJsonAsync($"/api/users/{userId}/force-change-email", new { newEmail });
        fce.StatusCode.Should().Be(HttpStatusCode.OK, await fce.Content.ReadAsStringAsync());

        await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();

            u!.Email.Value.Should().Be(newEmail.Trim().ToLowerInvariant());
            u.EmailConfirmed.Should().BeFalse("ao trocar email, deve exigir nova confirmação");

            // token de confirmação foi criado
            var hasToken = await db.EmailConfirmationTokens.AnyAsync(t => t.UserId == userId);
            hasToken.Should().BeTrue();
        });
    }

    private async Task<T> WithDb<T>(Func<SAMContext, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        return await work(db);
    }

    private async Task WithDb(Func<SAMContext, Task> work)
        => await WithDb(async db => { await work(db); return 0; });

    private static string NewEmail(string prefix = "t")
        => $"{prefix}-{Guid.NewGuid():N}@t.com";

    private static Guid ReadIdFromCreatedBody(string json)
    {
        // seu create retorna { id: "...", message: "..." }
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("id", out var id))
            return id.GetGuid();

        throw new InvalidOperationException($"Não consegui ler 'id' do body: {json}");
    }

    private static string ReadHash(object passwordHash)
    {
        var p = passwordHash.GetType().GetProperty("Value");
        return p?.GetValue(passwordHash)?.ToString()
               ?? passwordHash.ToString()
               ?? string.Empty;
    }

    private static RefreshToken CreateRefreshTokenBestEffort(Guid userId, DateTimeOffset expiresAt)
    {
        var now = DateTimeOffset.UtcNow;
        var tokenHash = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var t = typeof(RefreshToken);

        // tenta criar por ctor (public ou private)
        foreach (var ctor in t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var ps = ctor.GetParameters();
            var args = new object?[ps.Length];
            var ok = true;

            for (int i = 0; i < ps.Length; i++)
            {
                var pt = ps[i].ParameterType;

                if (pt == typeof(Guid)) args[i] = userId;
                else if (pt == typeof(string)) args[i] = tokenHash;
                else if (pt == typeof(DateTimeOffset)) args[i] = (i == 0 ? now : expiresAt);
                else if (pt == typeof(DateTime)) args[i] = expiresAt.UtcDateTime;
                else if (pt == typeof(int)) args[i] = 0;
                else if (pt == typeof(bool)) args[i] = false;
                else { ok = false; break; }
            }

            if (!ok) continue;

            try
            {
                var obj = (RefreshToken)ctor.Invoke(args);
                SetPropIfExists(obj, "UserId", userId);
                SetPropIfExists(obj, "TokenHash", tokenHash);
                SetPropIfExists(obj, "ExpiresAt", expiresAt);
                SetPropIfExists(obj, "RevokedAt", null);
                SetPropIfExists(obj, "ReplacedByTokenId", null);
                SetPropIfExists(obj, "UserAgent", "IntegrationTest");
                SetPropIfExists(obj, "IpAddress", "127.0.0.1");
                return obj;
            }
            catch
            {
                // tenta próximo ctor
            }
        }

        // fallback: cria “sem ctor” (se tiver) e seta props
        var inst = (RefreshToken)Activator.CreateInstance(t, nonPublic: true)!;
        SetPropIfExists(inst, "Id", Guid.NewGuid());
        SetPropIfExists(inst, "UserId", userId);
        SetPropIfExists(inst, "TokenHash", tokenHash);
        SetPropIfExists(inst, "ExpiresAt", expiresAt);
        SetPropIfExists(inst, "CreatedAt", now);
        SetPropIfExists(inst, "RevokedAt", null);
        SetPropIfExists(inst, "ReplacedByTokenId", null);
        SetPropIfExists(inst, "UserAgent", "IntegrationTest");
        SetPropIfExists(inst, "IpAddress", "127.0.0.1");
        return inst;
    }

    private static void SetPropIfExists(object obj, string propName, object? value)
    {
        var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p is null || !p.CanWrite) return;
        p.SetValue(obj, value);
    }
}
