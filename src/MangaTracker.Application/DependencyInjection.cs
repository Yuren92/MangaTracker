using MangaTracker.Application.Auth.ChangePassword;
using MangaTracker.Application.Auth.ConfirmEmail;
using MangaTracker.Application.Auth.ForgotPassword;
using MangaTracker.Application.Auth.GetCurrentUser;
using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Application.Auth.RegisterUser;
using MangaTracker.Application.Auth.ResendConfirmationEmail;
using MangaTracker.Application.Auth.ResetPassword;
using MangaTracker.Application.Collections.DeleteUserCollection;
using MangaTracker.Application.Collections.GetPendingTomes;
using MangaTracker.Application.Collections.GetUserCollectionDetail;
using MangaTracker.Application.Collections.GetUserCollections;
using MangaTracker.Application.Collections.ImportComicVineVolume;
using MangaTracker.Application.Collections.MarkAllTomesAsOwned;
using MangaTracker.Application.Collections.MarkTomeAsOwned;
using MangaTracker.Application.Collections.UnmarkTomeAsOwned;
using Microsoft.Extensions.DependencyInjection;

namespace MangaTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<ConfirmEmailHandler>();
        services.AddScoped<ResendConfirmationEmailHandler>();
        services.AddScoped<ForgotPasswordHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<GetUserCollectionsHandler>();
        services.AddScoped<GetUserCollectionDetailHandler>();
        services.AddScoped<MarkTomeAsOwnedHandler>();
        services.AddScoped<UnmarkTomeAsOwnedHandler>();
        services.AddScoped<GetPendingTomesHandler>();
        services.AddScoped<ImportComicVineVolumeHandler>();
        services.AddScoped<MarkAllTomesAsOwnedHandler>();
        services.AddScoped<DeleteUserCollectionHandler>();


        return services;
    }
}