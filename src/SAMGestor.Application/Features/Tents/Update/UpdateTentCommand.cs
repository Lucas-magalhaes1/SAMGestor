using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.Update;

public sealed record UpdateTentCommand(
    Guid RetreatId,
    Guid TentId,
    string Number,            
    TentCategory Category,    
    int Capacity,             
    bool? IsActive = null,   
    string? Notes = null     
) : IRequest<UpdateTentResponse>;