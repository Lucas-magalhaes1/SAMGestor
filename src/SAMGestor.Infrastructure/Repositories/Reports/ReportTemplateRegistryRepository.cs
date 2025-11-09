using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

public sealed class ReportTemplateRegistryRepository : IReportTemplateRegistry
{
    private readonly IReadOnlyList<ReportTemplateDto> _templates;
    private readonly IReadOnlyList<ReportTemplateSchemaDto> _schemas;

    public ReportTemplateRegistryRepository(IEnumerable<IReportTemplate> templates)
    {
        var uniq = templates
            .GroupBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        _templates = uniq
            .Select(t => new ReportTemplateDto(t.Key, t.DefaultTitle))
            .OrderBy(t => t.DefaultTitle)
            .ToList();

        _schemas = uniq
            .OfType<IDescribedReportTemplate>()            
            .Select(t => t.Describe())
            .OrderBy(s => s.DefaultTitle)
            .ToList();
    }

    public IReadOnlyList<ReportTemplateDto> ListTemplates() => _templates;
    public IReadOnlyList<ReportTemplateSchemaDto> ListSchemas() => _schemas;
}