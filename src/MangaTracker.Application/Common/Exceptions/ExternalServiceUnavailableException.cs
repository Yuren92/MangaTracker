namespace MangaTracker.Application.Common.Exceptions;

// An external dependency (Comic Vine) failed or timed out after the configured retries.
// It is not the client's fault nor a bug in the API, so it maps to 503 rather than 500.
public sealed class ExternalServiceUnavailableException : AppException
{
    public ExternalServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
