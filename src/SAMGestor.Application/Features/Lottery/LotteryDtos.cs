namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryResultDto(
    IReadOnlyList<Guid> Male,
    IReadOnlyList<Guid> Female,
    int MaleCap,
    int FemaleCap);