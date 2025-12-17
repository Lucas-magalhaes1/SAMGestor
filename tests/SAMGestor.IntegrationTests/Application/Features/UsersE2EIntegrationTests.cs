using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class UsersControllerE2EIntegrationTests(UsersWebAppFactory factory)
    : IClassFixture<UsersWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string NewEmail(string prefix = "u")
        => $"{prefix}-{Guid.NewGuid():N}@t.com";

    private static Guid ReadIdFromCreatedBody(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("id", out var idEl))
            throw new Exception($"Resposta não contém 'id'. Body: {json}");

        return idEl.ValueKind switch
        {
            JsonValueKind.String => Guid.Parse(idEl.GetString()!),
            _ => Guid.Parse(idEl.ToString())
        };
    }

    [Fact]
    public async Task Users_Lifecycle_Create_Get_Credentials_Update_ForceChange_Block_Unblock_List_ChangeRole_Delete()
    {
        // 1) CREATE
        var email = NewEmail();
        var createBody = new
        {
            name = "User Test",
            email,
            phone = "11999999999",
            role = 2
        };

        var create = await _client.PostAsJsonAsync("/api/users", createBody);
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());

        var createdJson = await create.Content.ReadAsStringAsync();
        var userId = ReadIdFromCreatedBody(createdJson);

        // 2) GET BY ID
        var get = await _client.GetAsync($"/api/users/{userId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK, await get.Content.ReadAsStringAsync());

        // 3) GET CREDENTIALS
        var creds = await _client.GetAsync($"/api/users/{userId}/credentials");
        creds.StatusCode.Should().Be(HttpStatusCode.OK, await creds.Content.ReadAsStringAsync());

        // 4) UPDATE
        var updateBody = new
        {
            id = userId,
            name = "User Updated",
            phone = "11888887777"
        };

        var update = await _client.PutAsJsonAsync($"/api/users/{userId}", updateBody);
        update.StatusCode.Should().Be(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync());

        // 5) FORCE CHANGE EMAIL
        var newEmail = NewEmail("new");
        var fce = await _client.PostAsJsonAsync($"/api/users/{userId}/force-change-email", new { newEmail });
        fce.StatusCode.Should().Be(HttpStatusCode.OK, await fce.Content.ReadAsStringAsync());

        // 6) FORCE CHANGE PASSWORD
        var fcp = await _client.PostAsJsonAsync($"/api/users/{userId}/force-change-password", new { newPassword = "NewP@ss123!" });
        fcp.StatusCode.Should().Be(HttpStatusCode.OK, await fcp.Content.ReadAsStringAsync());

        // 7) BLOCK
        var block = await _client.PostAsync($"/api/users/{userId}/block", content: null);
        block.StatusCode.Should().Be(HttpStatusCode.OK, await block.Content.ReadAsStringAsync());

        // 8) UNBLOCK
        var unblock = await _client.PostAsync($"/api/users/{userId}/unblock", content: null);
        unblock.StatusCode.Should().Be(HttpStatusCode.OK, await unblock.Content.ReadAsStringAsync());

        // 9) LIST
        var list = await _client.GetAsync("/api/users?skip=0&take=20");
        list.StatusCode.Should().Be(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());

        // 10) CHANGE ROLE (501)
        var changeRole = await _client.PostAsJsonAsync($"/api/users/{userId}/roles", new { role = "Admin" });
        changeRole.StatusCode.Should().Be(HttpStatusCode.NotImplemented, await changeRole.Content.ReadAsStringAsync());

        // 11) DELETE
        var del = await _client.DeleteAsync($"/api/users/{userId}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent, await del.Content.ReadAsStringAsync());

        // 12) GET AFTER DELETE -> 404
        var get2 = await _client.GetAsync($"/api/users/{userId}");
        get2.StatusCode.Should().Be(HttpStatusCode.NotFound, await get2.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Users_Update_IdMismatch_deve_retornar_400()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var res = await _client.PutAsJsonAsync($"/api/users/{id1}", new
        {
            id = id2,
            name = "X",
            phone = "11"
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Users_GetById_nao_existente_deve_retornar_404()
    {
        var id = Guid.NewGuid();
        var res = await _client.GetAsync($"/api/users/{id}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound, await res.Content.ReadAsStringAsync());
    }
}
