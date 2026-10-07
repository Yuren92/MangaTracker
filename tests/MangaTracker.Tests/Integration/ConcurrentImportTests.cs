using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using MangaTracker.Infrastructure.Persistence;
using MangaTracker.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ConcurrentImportTests
{
    private readonly MangaTrackerApiFactory _factory;

    public ConcurrentImportTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Concurrent_imports_of_the_same_new_volume_should_all_succeed_without_duplicates()
    {
        // Several users import a volume nobody has imported yet, at the same time. All
        // of them read "no edition", and only one INSERT can win the unique indexes.
        var users = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => TestUsers.CreateSignedInAsync(_factory)));

        var responses = await Task.WhenAll(users.Select(user =>
            user.Client.PostAsJsonAsync(
                "/api/collections/import-comic-vine-volume",
                new { apiDetailUrl = FakeComicVineClient.SlowVolumeUrl })));

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MangaTrackerDbContext>();

        var editions = await dbContext.Editions
            .Where(edition => edition.ComicVineApiDetailUrl == FakeComicVineClient.SlowVolumeUrl)
            .Select(edition => edition.Id)
            .ToListAsync();

        editions.Should().ContainSingle();
        (await dbContext.Tomes.CountAsync(tome => tome.EditionId == editions[0])).Should().Be(3);
        (await dbContext.UserCollections.CountAsync(collection => collection.EditionId == editions[0])).Should().Be(4);
    }
}
