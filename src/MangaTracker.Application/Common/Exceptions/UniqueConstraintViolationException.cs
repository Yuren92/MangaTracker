namespace MangaTracker.Application.Common.Exceptions;

// Raised when saving would break a unique constraint, typically because a concurrent
// request created the same row first. It is a 409 Conflict, not a server error.
public sealed class UniqueConstraintViolationException : ConflictException
{
    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
