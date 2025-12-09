using MediatR;

namespace SAMGestor.Application.Features.Dev.Seed;

public record SeedTestDataCommand : IRequest<SeedTestDataResponse>;
    
