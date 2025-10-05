using MediatR;

namespace SAMGestor.Application.Features.Service.Alerts.GetAll;

public enum ServiceAlertMode
{
    All,
    Preferences,
    Roster
}

public sealed record GetServiceAlertsQuery(
    Guid RetreatId,
    ServiceAlertMode Mode = ServiceAlertMode.All
) : IRequest<GetServiceAlertsResponse>;