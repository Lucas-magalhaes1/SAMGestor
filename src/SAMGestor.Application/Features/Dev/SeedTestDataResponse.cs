namespace SAMGestor.Application.Features.Dev.Seed;

public sealed record SeedTestDataResponse(
    Guid Seed1RetreatId,  // Contemplação
    int Seed1Registrations,
    Guid Seed2RetreatId,  // Famílias/Barracas
    int Seed2Registrations
);