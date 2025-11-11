namespace SAMGestor.Application.Dtos.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);