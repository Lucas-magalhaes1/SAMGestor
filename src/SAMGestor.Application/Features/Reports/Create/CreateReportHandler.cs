using MediatR;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.Create;

public sealed class CreateReportHandler : IRequestHandler<CreateReportCommand, CreateReportResponse>
{
    private readonly IReportCatalog _catalog;
    private readonly IReportEngine  _engine;

    public CreateReportHandler(IReportCatalog catalog, IReportEngine engine)
    {
        _catalog = catalog;
        _engine  = engine;
    }

    public async Task<CreateReportResponse> Handle(CreateReportCommand request, CancellationToken ct)
    {
        var id = await _catalog.CreateAsync(
            new CreateReportRequest(request.Title, request.TemplateKey, request.RetreatId, request.DefaultParamsJson),
            ct
        );
        
        if (string.IsNullOrWhiteSpace(id)) throw new Exception("Erro ao criar relatório.");
        
        var ctx = await _engine.BuildContextAsync(id, ct);
        return new CreateReportResponse(id, request.Title, DateTime.UtcNow); 
    }
}