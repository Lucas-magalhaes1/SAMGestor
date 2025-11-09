using MediatR;
using SAMGestor.Application.Interfaces.Reports;

namespace SAMGestor.Application.Features.Reports.Delete;

public sealed class DeleteReportHandler : IRequestHandler<DeleteReportCommand, bool>
{
    private readonly IReportCatalog _catalog;
    public DeleteReportHandler(IReportCatalog catalog) => _catalog = catalog;

    public async Task<bool> Handle(DeleteReportCommand request, CancellationToken ct)
    {
        var (ok, _) = await _catalog.DeleteAsync(request.Id, ct);
        return ok;
    }
}