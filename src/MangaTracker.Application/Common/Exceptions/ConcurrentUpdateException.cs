namespace MangaTracker.Application.Common.Exceptions;

// Raised when saving would overwrite a row that another request changed after it was
// read (an optimistic concurrency check failed). Like a unique violation, it is a 409.
public sealed class ConcurrentUpdateException : ConflictException
{
    public ConcurrentUpdateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
