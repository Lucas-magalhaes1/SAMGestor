namespace SAMGestor.Application.Dtos.Auth;

public sealed record ConfirmEmailRequest(string Token, string NewPassword);