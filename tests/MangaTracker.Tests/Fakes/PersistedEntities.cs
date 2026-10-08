using System.Reflection;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Fakes;

// Navigation collections are populated by EF Core when entities are loaded and are not
// writable from outside. Tests use this to build entities as if they came from the database.
public static class PersistedEntities
{
    public static void AddTome(Edition edition, Tome tome)
    {
        var tomes = (List<Tome>)typeof(Edition)
            .GetField("_tomes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(edition)!;

        tomes.Add(tome);
    }
}
