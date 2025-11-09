using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IReportExporter
{
    
    Task<(string ContentType, string FileName, byte[] Bytes)> ExportAsync(
        ReportPayload payload,
        string format,                // csv | xlsx | pdf
        string? fileNameBase = null,  
        CancellationToken ct = default);
}