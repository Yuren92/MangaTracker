using MangaTracker.Application.Collection.AddMangaToCollection;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AddMangaToCollectionHandler>();

        return services;
    }
}