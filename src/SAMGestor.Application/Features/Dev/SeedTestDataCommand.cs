using MediatR;

namespace SAMGestor.Application.Features.Dev.Seed;

public sealed record SeedTestDataCommand : IRequest<SeedTestDataResponse>;