// SAMGestor.Infrastructure/Services/Reports/DocumentReportExporter.cs
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Infrastructure.Services.Reports;

public sealed class DocumentReportExporter : IReportExporter
{
    private readonly IImageFetcher _images;
    public DocumentReportExporter(IImageFetcher images) => _images = images;

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
            "csv" => Task.FromResult(ExportCsv(payload, fileNameBase)),
            "pdf" => Task.FromResult(ExportPdf(payload, fileNameBase)),
            "xlsx" => throw new NotSupportedException("XLSX ainda não habilitado."),
            _ => throw new ArgumentException($"Formato '{format}' inválido. Use csv|pdf.")
        };
    }

    // ---------- CSV ----------
    private static (string ContentType, string FileName, byte[] Bytes) ExportCsv(ReportPayload p, string name)
    {
        var sb = new StringBuilder();
        var headers = p.columns.Select(c => Escape(c.Label ?? c.Key));
        sb.AppendLine(string.Join(",", headers));

        foreach (var row in p.data)
        {
            var values = p.columns.Select(c =>
            {
                row.TryGetValue(c.Key, out var v);
                return Escape(v?.ToString() ?? "");
            });
            sb.AppendLine(string.Join(",", values));
        }

        if (p.summary?.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Resumo,Valor");
            foreach (var kv in p.summary)
                sb.AppendLine($"{Escape(kv.Key)},{Escape(kv.Value?.ToString() ?? "")}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return ("text/csv; charset=utf-8", $"{name}.csv", bytes);
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

    // ---------- PDF ----------
    [Obsolete("Obsolete")]
    private (string ContentType, string FileName, byte[] Bytes) ExportPdf(ReportPayload p, string name)
    {
        var title = string.IsNullOrWhiteSpace(p.report.Title) ? "Relatório" : p.report.Title;

        var pdfBytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(t => t.Span(title).FontSize(16).SemiBold());
                    col.Item().Text(t => t.Span($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Darken2));
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        // índice fixo + foto fixa + demais relativas
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24); // índice
                            foreach (var cd in p.columns)
                            {
                                if (cd.Key.Equals("photoUrl", StringComparison.OrdinalIgnoreCase))
                                    c.ConstantColumn(56); // coluna de imagem com largura fixa
                                else
                                    c.RelativeColumn();
                            }
                        });

                        // cabeçalho
                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text(t => t.Span("#"));
                            foreach (var cd in p.columns)
                                h.Cell().Element(HeaderCell).Text(t => t.Span(cd.Label ?? cd.Key));

                            static IContainer HeaderCell(IContainer x) =>
                                x.Padding(6).Background(Colors.Grey.Lighten2).DefaultTextStyle(s => s.SemiBold());
                        });

                        // linhas
                        var idx = 0;
                        foreach (var row in p.data)
                        {
                            var stripe = (idx % 2 == 0) ? Colors.White : Colors.Grey.Lighten5;

                            table.Cell().Element(x => x.Padding(5).Background(stripe))
                                .Text(t => t.Span((idx + 1).ToString()));

                            foreach (var cd in p.columns)
                            {
                                row.TryGetValue(cd.Key, out var val);
                                var cell = table.Cell().Element(x => x.Padding(5).Background(stripe));

                                if (cd.Key.Equals("photoUrl", StringComparison.OrdinalIgnoreCase))
                                {
                                    // célula fixa (60×50) com box interno 48×48 para a imagem
                                    var hasUrl = val is string u && !string.IsNullOrWhiteSpace(u);
                                    byte[]? img = null;

                                    if (hasUrl)
                                    {
                                        try { img = _images.GetImageBytesAsync((string)val!).GetAwaiter().GetResult(); }
                                        catch { img = null; }
                                    }

                                    cell.Width(56).Height(44).AlignCenter().AlignMiddle().Column(cc =>
                                    {
                                        cc.Item().Width(48).Height(48).Element(e =>
                                        {
                                            if (img is { Length: > 0 })
                                                e.Image(img).FitArea();   // nunca extrapola o quadrado
                                            else
                                                e.Border(0); // vazio (sem texto para não estourar largura)
                                        });
                                    });

                                    continue;
                                }

                                // texto com quebra de palavras para nunca estourar largura
                                cell.Text(t => t.Span(val?.ToString() ?? "").WrapAnywhere());
                            }

                            idx++;
                        }
                    });

                    col.Item().PaddingVertical(8);

                    if (p.summary?.Count > 0)
                    {
                        col.Item().Text(t => t.Span("Resumo").FontSize(12).SemiBold());

                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); });

                            table.Header(h =>
                            {
                                h.Cell().Element(H).Text(t => t.Span("Chave"));
                                h.Cell().Element(H).Text(t => t.Span("Valor"));
                                static IContainer H(IContainer x) =>
                                    x.Padding(6).Background(Colors.Grey.Lighten2).DefaultTextStyle(s => s.SemiBold());
                            });

                            foreach (var kv in p.summary)
                            {
                                table.Cell().Element(x => x.Padding(5)).Text(t => t.Span(kv.Key));
                                table.Cell().Element(x => x.Padding(5)).Text(t => t.Span(kv.Value?.ToString() ?? ""));
                            }
                        });
                    }
                });

                page.Footer()
                    .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken2))
                    .AlignRight()
                    .Text(t =>
                    {
                        t.Span("Página ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
            });
        }).GeneratePdf();

        return ("application/pdf", $"{name}.pdf", pdfBytes);
    }
}
