using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
namespace SAMGestor.Domain.Entities;

/// <summary>
/// Metadados de uma geração (histórico). 
/// Não vamos salvar arquivo agora; StoragePath/FileName são opcionais.
/// </summary>
public class ReportInstance : Entity<Guid>
{
    public Guid ReportId { get; private set; }
    public ReportFormat Format { get; private set; }
    public DateTime GeneratedAt { get; private set; } = DateTime.UtcNow;
    public string? FileName     { get; private set; }
    public string? StoragePath  { get; private set; }
    public long?   FileSizeBytes{ get; private set; }

    private ReportInstance() { }

    public ReportInstance(Guid reportId, ReportFormat format, string? fileName = null, string? storagePath = null, long? fileSizeBytes = null)
    {
        Id            = Guid.NewGuid();
        ReportId      = reportId;
        Format        = format;
        GeneratedAt   = DateTime.UtcNow;
        FileName      = string.IsNullOrWhiteSpace(fileName) ? null : fileName.Trim();
        StoragePath   = string.IsNullOrWhiteSpace(storagePath) ? null : storagePath.Trim();
        FileSizeBytes = fileSizeBytes;
    }
}