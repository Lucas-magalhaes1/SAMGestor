using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.Logout;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IRefreshTokenService _refreshService;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IDateTimeProvider _time;
    private readonly IUnitOfWork _uow;

    public LogoutHandler(
        IRefreshTokenService refreshService,
        IRefreshTokenRepository refreshRepo,
        IDateTimeProvider time,
        IUnitOfWork uow)
    {
        _refreshService = refreshService;
        _refreshRepo = refreshRepo;
        _time = time;
        _uow = uow;
    }

    public async Task<Unit> Handle(LogoutCommand req, CancellationToken ct)
    {
        var now = _time.UtcNow;
        
        var token = await _refreshService.ValidateAsync(req.RefreshToken, req.UserId, now, ct);
        
        token.Revoke(now);
        await _refreshRepo.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}