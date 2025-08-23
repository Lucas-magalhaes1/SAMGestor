using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Retreats.Update;
using SAMGestor.IntegrationTests.Shared;
using Xunit;

namespace SAMGestor.IntegrationTests.Application.Features;

public class RetreatsCrudIntegrationTests(PostgresWebAppFactory factory) : IClassFixture<PostgresWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static object NewCreateBody(string name = "Retiro INT", string edition = "INT1") => new
    {
        name = new { value = name },
        edition,
        theme = "Tema INT",
        startDate = "2035-01-10",
        endDate   = "2035-01-12",
        maleSlots = 10,
        femaleSlots = 10,
        registrationStart = "2035-01-01",
        registrationEnd   = "2035-01-05",
        feeFazer  = new { amount = 100, currency = "BRL" },
        feeServir = new { amount =  50, currency = "BRL" },
        westRegionPct  = new { value = 50 },
        otherRegionPct = new { value = 50 }
    };
    
    [Fact]
    public async Task Full_Crud_Flow_Works()
    {
        
        var post = await _client.PostAsJsonAsync("/api/Retreats", NewCreateBody());
        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var createResp = await post.Content.ReadFromJsonAsync<CreateRetreatResponse>();
        createResp.Should().NotBeNull();
        var id = createResp!.RetreatId;

        
        var get = await _client.GetAsync($"/api/Retreats/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var getJson = await get.Content.ReadAsStringAsync();
        getJson.Should().Contain("Retiro INT");

        
        var updateBody = new
        {
            name = new { value = "Retiro INT Atualizado" },
            edition = "INT1",
            theme = "Tema Alterado",
            startDate = "2035-01-10",
            endDate   = "2035-01-12",
            maleSlots = 12,
            femaleSlots = 8,
            registrationStart = "2035-01-01",
            registrationEnd   = "2035-01-05",
            feeFazer  = new { amount = 120, currency = "BRL" },
            feeServir = new { amount =  60, currency = "BRL" },
            westRegionPct  = new { value = 40 },
            otherRegionPct = new { value = 60 }
        };

        var put = await _client.PutAsJsonAsync($"/api/Retreats/{id}", updateBody);
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var updResp = await put.Content.ReadFromJsonAsync<UpdateRetreatResponse>();
        updResp!.Id.Should().Be(id);

        
        var list = await _client.GetAsync("/api/Retreats?skip=0&take=50");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var listJson = await list.Content.ReadAsStringAsync();
        listJson.Should().Contain("Retiro INT Atualizado");

     
        var del = await _client.DeleteAsync($"/api/Retreats/{id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        
        var get404 = await _client.GetAsync($"/api/Retreats/{id}");
        get404.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_With_Invalid_Percentages_Returns_400()
    {
        var invalidBody = new
        {
            name = new { value = "Retiro Bad" },
            edition = "ERR",
            theme = "Tema",
            startDate = "2035-01-10",
            endDate   = "2035-01-12",
            maleSlots = 1,
            femaleSlots = 1,
            registrationStart = "2035-01-01",
            registrationEnd   = "2035-01-05",
            feeFazer  = new { amount = 100, currency = "BRL" },
            feeServir = new { amount = 50,  currency = "BRL" },
            westRegionPct  = new { value = 70 },   
            otherRegionPct = new { value = 20 }    
        };

        var resp = await _client.PostAsJsonAsync("/api/Retreats", invalidBody);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var msg = await resp.Content.ReadAsStringAsync();
        msg.Should().Contain("must equal 100");
    }
}
