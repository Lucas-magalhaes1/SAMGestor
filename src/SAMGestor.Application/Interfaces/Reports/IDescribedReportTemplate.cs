using SAMGestor.Application.Dtos.Reports;

namespace SAMGestor.Application.Interfaces.Reports;

public interface IDescribedReportTemplate : IReportTemplate
{
    ReportTemplateSchemaDto Describe();
}