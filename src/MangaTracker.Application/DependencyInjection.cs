using MangaTracker.Application.Auth.ChangePassword;
using MangaTracker.Application.Auth.ConfirmEmail;
using MangaTracker.Application.Auth.ForgotPassword;
using MangaTracker.Application.Auth.GetCurrentUser;
using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Application.Auth.RegisterUser;
using MangaTracker.Application.Auth.ResendConfirmationEmail;
using MangaTracker.Application.Auth.ResetPassword;
using MangaTracker.Application.Collection.AddMangaToCollection;
using MangaTracker.Application.Collection.AddOwnedVolumeToCollection;
using MangaTracker.Application.Collection.GetMangaCollection;
using MangaTracker.Application.Collection.GetMangaCollectionItem;
using MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;
using MangaTracker.Application.Collection.UpdateCustomTotalVolumes;
using MangaTracker.Application.Mal.GetMalMangaDetail;
using MangaTracker.Application.Mal.SearchMalManga;
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
        services.AddScoped<SearchMalMangaHandler>();
        services.AddScoped<GetMalMangaDetailHandler>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<ResendConfirmationEmailHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();

        return services;
    }
}