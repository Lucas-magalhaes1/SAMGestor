namespace SAMGestor.Domain.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}