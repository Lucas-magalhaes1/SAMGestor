namespace SAMGestor.Application.Features.Retreats.GetAll;

public record RetreatDto(
    Guid     Id,
    string   Name,
    string   Edition,
    DateOnly StartDate,
    DateOnly EndDate);