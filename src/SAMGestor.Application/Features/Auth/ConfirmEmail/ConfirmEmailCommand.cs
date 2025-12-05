    using MediatR;
    using SAMGestor.Application.Dtos.Auth;

    namespace SAMGestor.Application.Features.Auth.ConfirmEmail;

    public sealed record ConfirmEmailCommand(string Token, string NewPassword) : IRequest<LoginResponse>;