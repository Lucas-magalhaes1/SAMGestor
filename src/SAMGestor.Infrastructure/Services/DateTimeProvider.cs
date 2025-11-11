using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.Infrastructure.Services;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}