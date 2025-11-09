namespace SAMGestor.Application.Interfaces.Reports
{
    public interface IImageFetcher
    {
        Task<byte[]?> GetImageBytesAsync(string url, CancellationToken ct = default);
    }
}