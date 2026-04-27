using MangaTracker.Application.Collection.AddMangaToCollection;
using MangaTracker.Application.Collection.AddOwnedVolumeToCollection;
using MangaTracker.Application.Collection.GetMangaCollection;
using MangaTracker.Application.Collection.GetMangaCollectionItem;
using MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;
using MangaTracker.Application.Collection.UpdateCustomTotalVolumes;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AddMangaToCollectionHandler>();
        services.AddScoped<GetMangaCollectionHandler>();
        services.AddScoped<AddOwnedVolumeToCollectionHandler>();
        services.AddScoped<RemoveOwnedVolumeFromCollectionHandler>();
        services.AddScoped<GetMangaCollectionItemHandler>();
        services.AddScoped<UpdateCustomTotalVolumesHandler>();

        return services;
    }
}