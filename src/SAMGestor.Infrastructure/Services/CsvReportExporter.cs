using System.Text;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Infrastructure.Services;

public sealed class CsvReportExporter : IReportExporter
{
    public Task<(string ContentType, string FileName, byte[] Bytes)> ExportAsync(
        ReportPayload payload,
        string format,
        string? fileNameBase = null,
        CancellationToken ct = default)
    {
        fileNameBase ??= SanitizeFileName(payload.report.Title ?? "report");
        var ext = format.ToLowerInvariant();

        return ext switch
        {
            "csv"  => Task.FromResult(ExportCsv(payload, fileNameBase)),
            // “Stubs” para evolução: quando quiser, plugamos libs e ativamos
            "xlsx" => throw new NotSupportedException("XLSX ainda não habilitado. Podemos ativar com ClosedXML."),
            "pdf"  => throw new NotSupportedException("PDF ainda não habilitado. Podemos ativar com QuestPDF."),
            _      => throw new ArgumentException($"Formato '{format}' inválido. Use csv|xlsx|pdf.")
        };
    }

    private static (string ContentType, string FileName, byte[] Bytes) ExportCsv(ReportPayload p, string name)
    {
        var sb = new StringBuilder();

        // Cabeçalho: usa Columns em ordem
        var headers = p.columns.Select(c => Escape(c.Label ?? c.Key));
        sb.AppendLine(string.Join(",", headers));

        // Linhas
        foreach (var row in p.data)
        {
            var values = p.columns.Select(c =>
            {
                row.TryGetValue(c.Key, out var v);
                return Escape(v?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }

        // Opcional: seção de summary ao final
        if (p.summary?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Resumo,Valor");
            foreach (var kv in p.summary)
                sb.AppendLine($"{Escape(kv.Key)},{Escape(kv.Value?.ToString() ?? "")}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"{name}.csv";
        return ("text/csv; charset=utf-8", fileName, bytes);
    }

    private static string Escape(string s)
    {
        if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }

    private static string SanitizeFileName(string s)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return new string(s.Where(ch => !invalid.Contains(ch)).ToArray()).Trim();
    }
}
