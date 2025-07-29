namespace SAMGestor.Application.Features.Retreats.GetAll;

public record ListRetreatsResponse(
    IReadOnlyList<RetreatDto> Items,
    int TotalCount,
    int Skip,
    int Take);