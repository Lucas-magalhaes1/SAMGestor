public interface IStorageService
{
    Task<(string key, int sizeBytes)> SaveAsync(Stream file, string key, string contentType, CancellationToken ct);
    string GetPublicUrl(string key); // no local: http://localhost:5000/uploads/{key}
}