namespace MangaTracker.Application.Catalog.SearchCatalog;

public sealed record SearchCatalogQuery(
    string Query,
    int Limit);
