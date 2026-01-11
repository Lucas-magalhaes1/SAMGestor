using MediatR;

namespace SAMGestor.Application.Features.Users.UploadPhoto;

public sealed record UploadUserPhotoCommand(
    Guid UserId,          
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes
) : IRequest<UploadUserPhotoResult>;