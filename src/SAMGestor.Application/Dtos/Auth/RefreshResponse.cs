namespace SAMGestor.Application.Dtos.Auth;

public sealed record RefreshResponse(string AccessToken, string RefreshToken);