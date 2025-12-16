using System.Text.Json.Serialization;

namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryResultDto(
    List<Guid> Male,
    List<Guid> Female,
    int MaleCapacity,
    int FemaleCapacity,
    List<Guid> MalePriority,
    List<Guid> FemalePriority
);