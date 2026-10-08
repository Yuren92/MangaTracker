using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Tests.Integration.Infrastructure;

// Imports the tomes straight away, in a scope of its own like the background service
// does, so integration tests can check the result of adding a series without waiting.
// The production queue is covered by its own tests.
public sealed class InlineTomeImportQueue : ITomeImportQueue
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public InlineTomeImportQueue(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task EnqueueAsync(Guid editionId, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<EditionTomeImporter>()
            .ImportMissingTomesAsync(editionId, cancellationToken);
    }

    public bool IsPending(Guid editionId) => false;
}
