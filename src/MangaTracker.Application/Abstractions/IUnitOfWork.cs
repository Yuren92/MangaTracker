namespace MangaTracker.Application.Abstractions;

// Repositories only stage changes; a use case commits them all at once through this.
// All repositories share the same scoped DbContext, so one call persists the whole
// unit of work atomically.
public interface IUnitOfWork
{
    /// <exception cref="Common.Exceptions.UniqueConstraintViolationException">
    /// A unique constraint was violated, usually by a concurrent request.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // Drops every staged change so a use case can retry from a clean state.
    void DiscardChanges();
}
