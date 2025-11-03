public sealed class LocalStorageService : IStorageService
{
    private readonly string _basePath;      // ex.: "wwwroot/uploads"
    private readonly string _publicBaseUrl; // ex.: "http://localhost:5000/uploads"

    public LocalStorageService(string basePath, string publicBaseUrl)
    {
        _basePath = basePath;
        _publicBaseUrl = publicBaseUrl.TrimEnd('/');
        Directory.CreateDirectory(_basePath);
    }

    public async Task<(string key, int sizeBytes)> SaveAsync(Stream file, string key, string contentType, CancellationToken ct)
    {
        var fullPath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var fs = File.Create(fullPath);
        var size = 0;
        await file.CopyToAsync(fs, ct);
        size = (int)fs.Length;
        return (key, size);
    }

    public string GetPublicUrl(string key) => $"{_publicBaseUrl}/{key}";
}