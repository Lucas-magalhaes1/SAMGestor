using System.Net.Http.Headers;

namespace SAMGestor.IntegrationTests.Shared;

public static class HttpClientAuthExtensions
{
    public static void SetBearer(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static void ClearAuth(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }
}