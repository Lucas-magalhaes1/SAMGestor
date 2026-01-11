using MediatR;

namespace SAMGestor.Application.Features.Users.RemovePhoto;

public sealed record RemoveUserPhotoCommand(
    Guid UserId  
) : IRequest<Unit>;