namespace MangaTracker.Application.Common.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }

    protected ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
