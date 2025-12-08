using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.IntegrationTests.Shared;
using SAMGestor.IntegrationTests.TestDoubles;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

/// <summary>
/// Testes rápidos de autenticação (mock de eventbus)
/// </summary>
public class AuthIntegrationTests(PostgresWebAppFactory factory) 
    : IClassFixture<PostgresWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private FakeEventBus _eventBus = null!;

    public Task InitializeAsync()
    {
        _eventBus = factory.Services.GetRequiredService<FakeEventBus>();
        _eventBus.Clear();
        TestAuthHandler.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _eventBus.Clear();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_with_seed_admin_returns_token()
    {
        // Arrange
        var body = new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Role.Should().Be("admin");
        result.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        // Arrange
        var body = new
        {
            email = "admin@samgestor.local",
            password = "WrongPassword@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_with_nonexistent_user_returns_401()
    {
        // Arrange
        var body = new
        {
            email = "naoexiste@test.com",
            password = "Senha@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmEmail_with_valid_token_activates_account()
    {
        // Arrange - Cria usuário
        TestAuthHandler.CurrentRole = "admin";
        var createBody = new
        {
            name = "User Confirm",
            email = $"confirm-{Guid.NewGuid():N}@test.com",
            phone = "11999995555",
            role = "Consultant"
        };
        await _client.PostAsJsonAsync("/api/users", createBody);

        // Captura token do evento
        _eventBus.Events.Should().HaveCount(1);
        var eventData = _eventBus.Events[0].Data;
        var token = ExtractTokenFromEvent(eventData);

        // Act - Confirma email
        var confirmBody = new
        {
            token,
            newPassword = "SenhaForte@2025"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", confirmBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.EmailConfirmed.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();

        // Valida banco
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.Value == createBody.email);
        user!.EmailConfirmed.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmEmail_with_invalid_token_returns_400()
    {
        // Arrange
        var body = new
        {
            token = "token-invalido-123",
            newPassword = "Senha@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_with_expired_token_returns_400()
    {
        // Arrange - Cria usuário
        TestAuthHandler.CurrentRole = "admin";
        var createBody = new
        {
            name = "User Expired",
            email = $"expired-{Guid.NewGuid():N}@test.com",
            phone = "11999994444",
            role = "Consultant"
        };
        await _client.PostAsJsonAsync("/api/users", createBody);

        var eventData = _eventBus.Events[0].Data;
        var token = ExtractTokenFromEvent(eventData);

        // Expira token no banco
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var tokenEntity = await db.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash== token);
        tokenEntity!.ForceExpire(); // Expirado
        await db.SaveChangesAsync();

        // Act
        var confirmBody = new
        {
            token,
            newPassword = "Senha@123"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", confirmBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_after_confirmation_works()
    {
        // Arrange - Cria e confirma usuário
        TestAuthHandler.CurrentRole = "admin";
        var email = $"loginafter-{Guid.NewGuid():N}@test.com";
        var password = "SenhaTest@2025";
        
        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Login After",
            email,
            phone = "11999993333",
            role = "Consultant"
        });

        var token = ExtractTokenFromEvent(_eventBus.Events[0].Data);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token, newPassword = password });

        // Act - Login
        var loginBody = new { email, password };
        var response = await _client.PostAsJsonAsync("/api/login", loginBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_token_with_valid_refresh_returns_new_tokens()
    {
        // Arrange - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/login", new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act - Refresh
        var refreshBody = new
        {
            refreshToken = loginResult!.RefreshToken
        };
        var response = await _client.PostAsJsonAsync("/api/refresh", refreshBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(loginResult.RefreshToken); // Rotação
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        // Arrange - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/login", new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Act - Logout
        TestAuthHandler.CurrentRole = "admin";
        var logoutBody = new
        {
            refreshToken = loginResult!.RefreshToken
        };
        var response = await _client.PostAsJsonAsync("/api/logout", logoutBody);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Tentar usar refresh token revogado deve falhar
        var refreshResponse = await _client.PostAsJsonAsync("/api/refresh", new
        {
            refreshToken = loginResult.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Multiple_failed_login_attempts_lock_account()
    {
        // Arrange - Cria e confirma usuário
        TestAuthHandler.CurrentRole = "admin";
        var email = $"lockout-{Guid.NewGuid():N}@test.com";
        var correctPassword = "CorrectPass@2025";
        
        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Lockout",
            email,
            phone = "11999992222",
            role = "Consultant"
        });

        var token = ExtractTokenFromEvent(_eventBus.Events[0].Data);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token, newPassword = correctPassword });

        // Act - Múltiplas tentativas erradas (padrão: 5)
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/login", new
            {
                email,
                password = "WrongPassword@123"
            });
        }

        // Tenta com senha correta
        var response = await _client.PostAsJsonAsync("/api/login", new
        {
            email,
            password = correctPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("bloqueada"); // ou "locked"
    }

    private static string ExtractTokenFromEvent(object eventData)
    {
        // Assume que o evento tem uma propriedade Token ou ConfirmationToken
        var json = System.Text.Json.JsonSerializer.Serialize(eventData);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        
        if (doc.RootElement.TryGetProperty("Token", out var tokenEl) ||
            doc.RootElement.TryGetProperty("token", out tokenEl) ||
            doc.RootElement.TryGetProperty("ConfirmationToken", out tokenEl))
        {
            return tokenEl.GetString()!;
        }
        
        throw new Exception("Token não encontrado no evento");
    }

    private sealed class LoginResponse
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public bool EmailConfirmed { get; set; }
        public UserDto User { get; set; } = null!;
    }

    private sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
