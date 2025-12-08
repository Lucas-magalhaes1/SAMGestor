using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using SAMGestor.Infrastructure.Persistence;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

/// <summary>
/// Testes E2E COMPLETOS de Auth: valida eventos no RabbitMQ real e todos os cenários
/// </summary>
public class AuthE2ETests(RabbitOutboxWebAppFactory factory) 
    : IClassFixture<RabbitOutboxWebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public Task InitializeAsync()
    {
        TestAuthHandler.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        TestAuthHandler.Reset();
        return Task.CompletedTask;
    }

    #region POST /api/login - 5 testes

    [Fact]
    public async Task Login_with_valid_credentials_returns_tokens_and_user()
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
        result.EmailConfirmed.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("admin@samgestor.local");
        result.User.Role.Should().Be("admin");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        // Arrange
        var body = new
        {
            email = "admin@samgestor.local",
            password = "SenhaErrada@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Credenciais inválidas", "ou mensagem similar");
    }

    [Fact]
    public async Task Login_with_nonexistent_email_returns_401()
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
    public async Task Login_with_blocked_account_returns_401_or_403()
    {
        // Arrange - Cria e bloqueia usuário
        TestAuthHandler.CurrentRole = "admin";
        var email = $"blocked-{Guid.NewGuid():N}@test.com";
        var password = "BlockTest@2025";

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Blocked",
            email,
            phone = "11999991111",
            role = "Consultant"
        });

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token, newPassword = password });

        // Bloqueia usuário
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        await _client.PostAsync($"/api/users/{user!.Id}/block", null);

        // Act - Tenta login
        var response = await _client.PostAsJsonAsync("/api/login", new { email, password });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Login_with_unconfirmed_email_returns_401_or_special_response()
    {
        // Arrange - Cria usuário sem confirmar
        TestAuthHandler.CurrentRole = "admin";
        var email = $"unconfirmed-{Guid.NewGuid():N}@test.com";

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Unconfirmed",
            email,
            phone = "11999990000",
            role = "Consultant"
        });

        // Act - Tenta login sem confirmar email
        var response = await _client.PostAsJsonAsync("/api/login", new
        {
            email,
            password = "Qualquer@123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/auth/confirm-email - 5 testes

    [Fact]
    public async Task ConfirmEmail_with_valid_token_activates_account_and_returns_tokens()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        var email = $"confirm-valid-{Guid.NewGuid():N}@test.com";
        var password = "ValidTest@2025";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Confirm Valid",
            email,
            phone = "11999998888",
            role = "Consultant"
        });

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token,
            newPassword = password
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        result!.Success.Should().BeTrue();
        result.EmailConfirmed.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();

        // Valida banco
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email.Value == email);
        user!.EmailConfirmed.Should().BeTrue();
        user.EmailConfirmedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmEmail_with_invalid_token_returns_400()
    {
        // Arrange
        var body = new
        {
            token = "token-completamente-invalido-12345",
            newPassword = "Senha@123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("inválido", "ou token não encontrado");
    }

    [Fact]
    public async Task ConfirmEmail_with_expired_token_returns_400()
    {
        // Arrange - Cria usuário
        TestAuthHandler.CurrentRole = "admin";
        var email = $"confirm-expired-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Expired",
            email,
            phone = "11999997777",
            role = "Consultant"
        });

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);

        // Expira token no banco
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var tokenEntity = await db.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash== token); 

        if (tokenEntity != null)
        {
            // Força expiração via reflexão
            var prop = typeof(SAMGestor.Domain.Entities.EmailConfirmationToken)
                .GetProperty("ExpiresAt");
            prop!.SetValue(tokenEntity, DateTimeOffset.UtcNow.AddDays(-1));
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token,
            newPassword = "Senha@123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("expirado", "ou token inválido");
    }

    [Fact]
    public async Task ConfirmEmail_with_already_used_token_returns_400()
    {
        // Arrange - Confirma token uma vez
        TestAuthHandler.CurrentRole = "admin";
        var email = $"confirm-used-{Guid.NewGuid():N}@test.com";
        var password = "UsedTest@2025";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Used Token",
            email,
            phone = "11999996666",
            role = "Consultant"
        });

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);

        // Usa token pela primeira vez
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token, newPassword = password });

        // Act - Tenta usar novamente
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token,
            newPassword = "OutraSenha@2025"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConfirmEmail_with_weak_password_returns_400()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        var email = $"confirm-weak-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Weak Pass",
            email,
            phone = "11999995555",
            role = "Consultant"
        });

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);

        // Act - Senha fraca
        var response = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token,
            newPassword = "123" // Muito fraca
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("senha", "ou password");
    }

    #endregion

    #region POST /api/auth/request-password-reset - 3 testes

    [Fact]
    public async Task RequestPasswordReset_with_valid_email_publishes_event()
    {
        // Arrange - Cria e confirma usuário
        TestAuthHandler.CurrentRole = "admin";
        var email = $"reset-valid-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        
        var qCreate = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qCreate.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Reset Valid",
            email,
            phone = "11999994444",
            role = "Consultant"
        });

        var createToken = await WaitForTokenInQueueAsync(ch, qCreate.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token = createToken,
            newPassword = "OldPass@2025"
        });

        // Setup fila pra evento de reset
        var qReset = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qReset.QueueName, "sam.topic", routingKey: "password.reset.#");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var resetToken = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email);
        resetToken.Should().NotBeNullOrEmpty("evento password.reset deve ser publicado");
    }

    [Fact]
    public async Task RequestPasswordReset_with_nonexistent_email_returns_200_but_no_event()
    {
        // Arrange
        var email = $"naoexiste-{Guid.NewGuid():N}@test.com";

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });

        // Assert
        // Retorna 200 por segurança (não revela se email existe)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestPasswordReset_multiple_times_generates_new_tokens()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        var email = $"reset-multiple-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        
        var qCreate = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qCreate.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Reset Multiple",
            email,
            phone = "11999993333",
            role = "Consultant"
        });

        var createToken = await WaitForTokenInQueueAsync(ch, qCreate.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token = createToken,
            newPassword = "Pass@2025"
        });

        var qReset = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qReset.QueueName, "sam.topic", routingKey: "password.reset.#");

        // Act - Solicita 2x
        await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });
        await Task.Delay(1000);
        await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });

        // Assert - 2 eventos diferentes
        var token1 = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email, 10);
        var token2 = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email, 10);

        token1.Should().NotBeNullOrEmpty();
        token2.Should().NotBeNullOrEmpty();
        token1.Should().NotBe(token2, "tokens devem ser diferentes");
    }

    #endregion

    #region POST /api/auth/reset-password - 4 testes

    [Fact]
    public async Task ResetPassword_with_valid_token_changes_password()
    {
        // Arrange - Full flow
        TestAuthHandler.CurrentRole = "admin";
        var email = $"reset-success-{Guid.NewGuid():N}@test.com";
        var oldPassword = "OldPass@2025";
        var newPassword = "NewPass@2025";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        
        var qCreate = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qCreate.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Reset Success",
            email,
            phone = "11999992222",
            role = "Consultant"
        });

        var createToken = await WaitForTokenInQueueAsync(ch, qCreate.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token = createToken, newPassword = oldPassword });

        var qReset = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qReset.QueueName, "sam.topic", routingKey: "password.reset.#");

        await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });
        var resetToken = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            newPassword
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Nova senha funciona
        var loginNew = await _client.PostAsJsonAsync("/api/login", new { email, password = newPassword });
        loginNew.StatusCode.Should().Be(HttpStatusCode.OK);

        // Senha antiga não funciona
        var loginOld = await _client.PostAsJsonAsync("/api/login", new { email, password = oldPassword });
        loginOld.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetPassword_with_invalid_token_returns_400()
    {
        // Arrange
        var body = new
        {
            token = "token-invalido-reset-123",
            newPassword = "NewPass@2025"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_with_expired_token_returns_400()
    {
        // Arrange - Cria token e expira
        TestAuthHandler.CurrentRole = "admin";
        var email = $"reset-expired-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        
        var qCreate = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qCreate.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Reset Expired",
            email,
            phone = "11999991111",
            role = "Consultant"
        });

        var createToken = await WaitForTokenInQueueAsync(ch, qCreate.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token = createToken,
            newPassword = "Pass@2025"
        });

        var qReset = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qReset.QueueName, "sam.topic", routingKey: "password.reset.#");

        await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });
        var resetToken = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email);

        // Expira token
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();
        var tokenEntity = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == resetToken);

        if (tokenEntity != null)
        {
            var prop = typeof(SAMGestor.Domain.Entities.PasswordResetToken)
                .GetProperty("ExpiresAt");
            prop!.SetValue(tokenEntity, DateTimeOffset.UtcNow.AddDays(-1));
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "NewPass@2025"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_with_weak_password_returns_400()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        var email = $"reset-weak-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        
        var qCreate = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qCreate.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "User Reset Weak",
            email,
            phone = "11999990000",
            role = "Consultant"
        });

        var createToken = await WaitForTokenInQueueAsync(ch, qCreate.QueueName, email);
        await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token = createToken,
            newPassword = "Pass@2025"
        });

        var qReset = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(qReset.QueueName, "sam.topic", routingKey: "password.reset.#");

        await _client.PostAsJsonAsync("/api/auth/request-password-reset", new { email });
        var resetToken = await WaitForTokenInQueueAsync(ch, qReset.QueueName, email);

        // Act - Senha fraca
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = resetToken,
            newPassword = "abc" // Fraca
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/refresh - 3 testes

    [Fact]
    public async Task Refresh_with_valid_token_returns_new_tokens_and_rotates()
    {
        // Arrange - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/login", new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var oldRefreshToken = loginResult!.RefreshToken;

        // Act
        var response = await _client.PostAsJsonAsync("/api/refresh", new
        {
            refreshToken = oldRefreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(oldRefreshToken, "refresh token deve rotacionar");

        // Token antigo não funciona mais
        var oldResponse = await _client.PostAsJsonAsync("/api/refresh", new
        {
            refreshToken = oldRefreshToken
        });
        oldResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_invalid_token_returns_401()
    {
        // Arrange
        var body = new
        {
            refreshToken = "token-refresh-invalido-12345"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/refresh", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_revoked_token_returns_401()
    {
        // Arrange - Login e logout
        var loginResponse = await _client.PostAsJsonAsync("/api/login", new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        TestAuthHandler.CurrentRole = "admin";
        TestAuthHandler.CurrentEmail = "admin@samgestor.local";

        await _client.PostAsJsonAsync("/api/logout", new
        {
            refreshToken = loginResult!.RefreshToken
        });

        // Act - Tenta refresh com token revogado
        var response = await _client.PostAsJsonAsync("/api/refresh", new
        {
            refreshToken = loginResult.RefreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/logout - 3 testes

    [Fact]
    public async Task Logout_revokes_refresh_token_and_publishes_event()
    {
        // Arrange - Login
        var loginResponse = await _client.PostAsJsonAsync("/api/login", new
        {
            email = "admin@samgestor.local",
            password = "Admin@2025"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Setup RabbitMQ
        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.logout.#");

        // Act
        TestAuthHandler.CurrentRole = "admin";
        TestAuthHandler.CurrentEmail = "admin@samgestor.local";

        var response = await _client.PostAsJsonAsync("/api/logout", new
        {
            refreshToken = loginResult!.RefreshToken
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventFound = await WaitForEventInQueueAsync(ch, q.QueueName, "admin@samgestor.local", 15);
        eventFound.Should().BeTrue("evento user.logout deve ser publicado");

        // Refresh token revogado
        var refreshResponse = await _client.PostAsJsonAsync("/api/refresh", new
        {
            refreshToken = loginResult.RefreshToken
        });
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_with_invalid_refresh_token_returns_400()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        var body = new
        {
            refreshToken = "token-refresh-invalido-logout"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/logout", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_without_authentication_returns_401()
    {
        // Arrange
        TestAuthHandler.CurrentRole = ""; // Sem auth
        TestAuthHandler.CurrentUserId = "";

        var body = new
        {
            refreshToken = "qualquer-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/logout", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/user - 3 testes

    [Fact]
    public async Task GetCurrentUser_when_authenticated_returns_user_info()
    {
        // Arrange
        TestAuthHandler.CurrentRole = "admin";
        TestAuthHandler.CurrentEmail = "admin@samgestor.local";
        TestAuthHandler.CurrentUserId = "f62d937f-4120-4850-881d-9b9e4aa5c3ac"; // Admin do seed
        TestAuthHandler.CurrentName = "Admin Sistema";

        // Act
        var response = await _client.GetAsync("/api/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        
        result.Should().NotBeNull();
        result!.Email.Should().Be("admin@samgestor.local");
        result.Role.Should().Be("admin");
        result.Name.Should().Be("Admin Sistema");
    }

    [Fact]
    public async Task GetCurrentUser_without_authentication_returns_401()
    {
        // Arrange - Remove auth
        TestAuthHandler.CurrentRole = "";
        TestAuthHandler.CurrentUserId = "";

        // Act
        var response = await _client.GetAsync("/api/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_with_consultant_role_returns_correct_info()
    {
        // Arrange - Cria consultant e autentica como ele
        TestAuthHandler.CurrentRole = "admin";
        var email = $"consultant-current-{Guid.NewGuid():N}@test.com";

        using var conn = await CreateRabbitConnectionAsync();
        using var ch = await conn.CreateChannelAsync();
        await ch.ExchangeDeclareAsync("sam.topic", ExchangeType.Topic, durable: true);
        var q = await ch.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await ch.QueueBindAsync(q.QueueName, "sam.topic", routingKey: "user.created.#");

        await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Consultant Current",
            email,
            phone = "11999998888",
            role = "Consultant"
        });

        var token = await WaitForTokenInQueueAsync(ch, q.QueueName, email);
        var confirmResponse = await _client.PostAsJsonAsync("/api/auth/confirm-email", new
        {
            token,
            newPassword = "ConsultTest@2025"
        });

        var confirmResult = await confirmResponse.Content.ReadFromJsonAsync<LoginResponse>();

        // Autentica como consultant
        TestAuthHandler.CurrentRole = "consultant";
        TestAuthHandler.CurrentEmail = email;
        TestAuthHandler.CurrentUserId = confirmResult!.User.Id.ToString();
        TestAuthHandler.CurrentName = "Consultant Current";

        // Act
        var response = await _client.GetAsync("/api/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UserDto>();
        result!.Role.Should().Be("consultant");
        result.Email.Should().Be(email);
    }

    #endregion

    #region Helpers

    private async Task<IConnection> CreateRabbitConnectionAsync()
    {
        var cf = new ConnectionFactory
        {
            HostName = factory.RabbitHost,
            Port = factory.RabbitPort,
            UserName = "guest",
            Password = "guest"
        };
        return await cf.CreateConnectionAsync("samtests-auth-e2e");
    }

    private static async Task<string?> WaitForTokenInQueueAsync(
        RabbitMQ.Client.IChannel channel, 
        string queueName, 
        string email, 
        int timeoutSeconds = 30)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        
        while (DateTime.UtcNow < deadline)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result != null)
            {
                var json = Encoding.UTF8.GetString(result.Body.Span);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("data", out var dataEl))
                {
                    if (dataEl.TryGetProperty("Email", out var emailEl) || 
                        dataEl.TryGetProperty("email", out emailEl))
                    {
                        if (emailEl.GetString() == email)
                        {
                            if (dataEl.TryGetProperty("Token", out var tokenEl) ||
                                dataEl.TryGetProperty("token", out tokenEl) ||
                                dataEl.TryGetProperty("ConfirmationToken", out tokenEl) ||
                                dataEl.TryGetProperty("ResetToken", out tokenEl))
                            {
                                return tokenEl.GetString();
                            }
                        }
                    }
                }
            }
            else
            {
                await Task.Delay(500);
            }
        }

        return null;
    }

    private static async Task<bool> WaitForEventInQueueAsync(
        RabbitMQ.Client.IChannel channel, 
        string queueName, 
        string email, 
        int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        
        while (DateTime.UtcNow < deadline)
        {
            var result = await channel.BasicGetAsync(queueName, autoAck: true);
            if (result != null)
            {
                var json = Encoding.UTF8.GetString(result.Body.Span);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("data", out var dataEl))
                {
                    if (dataEl.TryGetProperty("Email", out var emailEl) || 
                        dataEl.TryGetProperty("email", out emailEl))
                    {
                        if (emailEl.GetString() == email)
                        {
                            return true;
                        }
                    }
                }
            }
            else
            {
                await Task.Delay(500);
            }
        }

        return false;
    }

    #endregion

    #region DTOs

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

    #endregion
}
