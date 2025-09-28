namespace SAMGestor.Domain.Exceptions;

public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message) : base(message) { }
    public UniqueConstraintViolationException(string message, Exception inner) : base(message, inner) { }
}