using System.Reflection;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Fakes;

// Navigation properties are populated by EF Core when entities are loaded and have
// private setters. Tests use this to build entities as if they came from the database.
public static class PersistedEntities
{
    public static UserCollection CollectionFor(Edition edition, Guid userId)
    {
        var collection = new UserCollection(userId: userId, editionId: edition.Id);

        typeof(UserCollection)
            .GetProperty(nameof(UserCollection.Edition), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(collection, edition);

        return collection;
    }
}
