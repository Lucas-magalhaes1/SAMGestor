using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.BulkCreate;

public sealed record BulkCreateTentsCommand(
    Guid RetreatId,
    IReadOnlyList<BulkCreateTentItemDto> Items
) : IRequest<BulkCreateTentsResponse>;

public sealed record BulkCreateTentItemDto(
    string Number,          
    TentCategory Category,  
    int Capacity,           
    string? Notes           
);