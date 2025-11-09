using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IReportTemplateRegistry
{
    IReadOnlyList<ReportTemplateDto> ListTemplates();
    IReadOnlyList<ReportTemplateSchemaDto> ListSchemas();
}