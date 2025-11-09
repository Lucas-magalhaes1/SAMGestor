// SAMGestor.Infrastructure/Services/Reports/HttpImageFetcher.cs
using System.Net.Http.Headers;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Infrastructure.Services.Reports
{
    public sealed class HttpImageFetcher : IImageFetcher
    {
        private readonly HttpClient _http;
        public HttpImageFetcher(HttpClient http) => _http = http;

        public async Task<byte[]?> GetImageBytesAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!res.IsSuccessStatusCode) return null;

            if (res.Content.Headers.ContentLength is long len && len > 3_000_000) return null;

            if (res.Content.Headers.ContentType is MediaTypeHeaderValue mt &&
                !mt.MediaType!.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                return null;

            return await res.Content.ReadAsByteArrayAsync(ct);
        }
    }
}