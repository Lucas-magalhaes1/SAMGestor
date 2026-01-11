namespace SAMGestor.Application.Features.Users.UploadPhoto;

public sealed record UploadUserPhotoResult(
    Guid UserId,
    string StorageKey,
    string ContentType,
    int SizeBytes,
    DateTime UploadedAt
);