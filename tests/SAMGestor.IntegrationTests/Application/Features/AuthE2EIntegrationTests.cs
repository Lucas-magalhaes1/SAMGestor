using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public sealed class AuthE2EIntegrationTests(AuthWebAppFactory factory)
    : IClassFixture<AuthWebAppFactory>
{
    private readonly AuthWebAppFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Auth_ConfirmEmail_Login_Refresh_Logout_CurrentUser_E2E()
    {
        // Arrange: cria usuário com email NÃO confirmado + token de confirmação válido
        var now = DateTimeOffset.UtcNow;

        var seeded = await WithDb<(Guid Id, string Email, string Raw)>(async db =>
        {
            using var scope = _factory.Services.CreateScope();

            var refreshSvc = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
            var hasher     = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var emailLocal = NewEmail();
            var raw = $"confirm-{Guid.NewGuid():N}";

            // ✅ hash válido (qualquer senha placeholder serve, porque ela será trocada no confirm-email)
            var placeholderHash = new PasswordHash(hasher.Hash("TempP@ss123!"));

            var user = new User(
                new FullName("Auth Confirm User"),
                new EmailAddress(emailLocal),
                "11999990000",
                placeholderHash,
                UserRole.Consultant);

            await db.Users.AddAsync(user);

            var hash = refreshSvc.Hash(raw);
            var token = EmailConfirmationToken.Create(
                user.Id,
                hash,
                expiresAt: now.AddDays(1),
                createdAt: now);

            await db.EmailConfirmationTokens.AddAsync(token);

            await db.SaveChangesAsync();

            return (Id: user.Id, Email: emailLocal, Raw: raw);
        });


        var userId = seeded.Id;
        var email = seeded.Email;
        var rawToken = seeded.Raw;

        // 1) CONFIRM EMAIL -> retorna LoginResponse (access+refresh)
        var confirm = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token = rawToken,
            newPassword = "Str0ngP@ss123!"
        });

        confirm.StatusCode.Should().Be(HttpStatusCode.OK, await confirm.Content.ReadAsStringAsync());

        var confirmJson = await confirm.Content.ReadAsStringAsync();
        var access1 = ReadString(confirmJson, "accessToken");
        var refresh1 = ReadString(confirmJson, "refreshToken");
        ReadBool(confirmJson, "emailConfirmed").Should().BeTrue();

        // confere no DB: user.EmailConfirmed=true e existe token (marcado usado no handler)
        await WithDb(async db =>
        {
            var u = await db.Users.FindAsync(userId);
            u.Should().NotBeNull();
            u!.EmailConfirmed.Should().BeTrue();

            var anyToken = await db.EmailConfirmationTokens.AnyAsync(t => t.UserId == userId);
            anyToken.Should().BeTrue();
            return 0;
        });

        // 2) /api/user com bearer
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/user");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access1);

        var me = await _client.SendAsync(meReq);
        me.StatusCode.Should().Be(HttpStatusCode.OK, await me.Content.ReadAsStringAsync());

        var meJson = await me.Content.ReadAsStringAsync();
        ReadString(meJson, "id").Should().Be(userId.ToString());
        ReadString(meJson, "email").Should().Be(email.ToLowerInvariant());

        // 3) LOGIN
        var login = await _client.PostAsJsonAsync("/api/login", new
        {
            email,
            password = "Str0ngP@ss123!"
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        var loginJson = await login.Content.ReadAsStringAsync();
        var access2 = ReadString(loginJson, "accessToken");
        var refresh2 = ReadString(loginJson, "refreshToken");

        // 4) REFRESH (rotação)
        var refresh = await _client.PostAsJsonAsync("/api/refresh", new
        {
            accessToken = access2,
            refreshToken = refresh2
        });

        refresh.StatusCode.Should().Be(HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        var refreshJson = await refresh.Content.ReadAsStringAsync();
        var access3 = ReadString(refreshJson, "accessToken");
        var refresh3 = ReadString(refreshJson, "refreshToken");

        // 5) LOGOUT revoga refresh3
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access3);
        logoutReq.Content = JsonContent.Create(new { refreshToken = refresh3 });

        var logout = await _client.SendAsync(logoutReq);
        logout.StatusCode.Should().Be(HttpStatusCode.OK, await logout.Content.ReadAsStringAsync());

        // DB: refresh token revogado
        await WithDb(async db =>
        {
            using var scope = _factory.Services.CreateScope();
            var refreshSvc = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

            var hash = refreshSvc.Hash(refresh3);

            var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == hash);
            token.Should().NotBeNull("logout deve revogar o refresh token informado");
            token!.RevokedAt.Should().NotBeNull();
            return 0;
        });
    }

    [Fact]
    public async Task Auth_RequestPasswordReset_ResetPassword_AllowsLogin_And_DisabledUserCannotLogin_E2E()
    {
        var now = DateTimeOffset.UtcNow;

        // Arrange: cria 2 usuários:
        // - enabled para fluxo reset
        // - disabled para testar login bloqueado
        var setup = await WithDb<(Guid EnabledUserId, string EnabledEmail, string DisabledEmail)>(async db =>
        {
            using var scope = _factory.Services.CreateScope();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var enabledEmail = NewEmail("enabled");
            var disabledEmail = NewEmail("disabled");

            var enabledUser = new User(
                new FullName("Enabled User"),
                new EmailAddress(enabledEmail),
                "11988887777",
                new PasswordHash(hasher.Hash("OldP@ss123!")),
                UserRole.Consultant);
            enabledUser.ConfirmEmail(now);

            var disabledUser = new User(
                new FullName("Disabled User"),
                new EmailAddress(disabledEmail),
                "11977776666",
                new PasswordHash(hasher.Hash("AnyP@ss123!")),
                UserRole.Consultant);
            disabledUser.ConfirmEmail(now);
            disabledUser.Disable();

            await db.Users.AddRangeAsync(enabledUser, disabledUser);
            await db.SaveChangesAsync();

            return (EnabledUserId: enabledUser.Id, EnabledEmail: enabledEmail, DisabledEmail: disabledEmail);
        });

        // 1) REQUEST PASSWORD RESET (sempre 200) + cria token/outbox para e-mail existente
        var before = await WithDb<(int Tokens, int Outbox)>(async db =>
        {
            var tokens = await db.PasswordResetTokens.CountAsync();
            var outbox = await db.OutboxMessages.CountAsync();
            return (Tokens: tokens, Outbox: outbox);
        });

        var reqReset = await _client.PostAsJsonAsync("/api/auth/request-password-reset", new
        {
            email = setup.EnabledEmail
        });

        reqReset.StatusCode.Should().Be(HttpStatusCode.OK, await reqReset.Content.ReadAsStringAsync());

        var after = await WithDb<(int Tokens, int Outbox)>(async db =>
        {
            var tokens = await db.PasswordResetTokens.CountAsync();
            var outbox = await db.OutboxMessages.CountAsync();
            return (Tokens: tokens, Outbox: outbox);
        });

        after.Tokens.Should().BeGreaterThan(before.Tokens, "deve criar PasswordResetToken para usuário existente");
        after.Outbox.Should().BeGreaterThan(before.Outbox, "deve enfileirar evento no Outbox");

        // 2) RESET PASSWORD (E2E real) — cria nosso token conhecido e chama endpoint
        var raw = $"reset-{Guid.NewGuid():N}";
        var tokenHash = Sha256B64(raw);

        await WithDb(async db =>
        {
            var t = PasswordResetToken.Create(setup.EnabledUserId, tokenHash, now.AddMinutes(30), now);
            await db.PasswordResetTokens.AddAsync(t);
            await db.SaveChangesAsync();
            return 0;
        });

        var reset = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = raw,
            newPassword = "NewP@ss123!"
        });

        reset.StatusCode.Should().Be(HttpStatusCode.OK, await reset.Content.ReadAsStringAsync());

        // 3) LOGIN com nova senha (enabled) -> OK
        var login = await _client.PostAsJsonAsync("/api/login", new
        {
            email = setup.EnabledEmail,
            password = "NewP@ss123!"
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());

        // 4) LOGIN em usuário desabilitado -> 401
        var disabledLogin = await _client.PostAsJsonAsync("/api/login", new
        {
            email = setup.DisabledEmail,
            password = "AnyP@ss123!"
        });

        disabledLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized, await disabledLogin.Content.ReadAsStringAsync());
    }

    // ===== helpers =====

    private async Task<T> WithDb<T>(Func<SAMContext, Task<T>> work)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        return await work(db);
    }

    private static string NewEmail(string prefix = "t")
        => $"{prefix}-{Guid.NewGuid():N}@t.com";

    private static string Sha256B64(string raw)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static string ReadString(string json, string prop)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(prop, out var p))
            return p.GetString() ?? "";

        throw new InvalidOperationException($"Propriedade '{prop}' não encontrada no JSON: {json}");
    }

    private static bool ReadBool(string json, string prop)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty(prop, out var p))
            return p.GetBoolean();

        throw new InvalidOperationException($"Propriedade '{prop}' não encontrada no JSON: {json}");
    }
}
