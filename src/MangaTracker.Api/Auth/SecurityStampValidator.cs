using System.Security.Claims;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MangaTracker.Api.Auth;

// A JWT stays cryptographically valid until it expires. Comparing its security stamp
// with the user's current one lets the server revoke it early: changing or resetting
// the password rotates the stamp, so every token issued before stops working, and
// tokens of deleted users stop working too.
//
// Cost: one indexed primary-key lookup per authenticated request. Fine at this scale;
// with more traffic it could be cached for a short time.
public static class SecurityStampValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var tokenStamp = context.Principal?.FindFirstValue(AuthClaimTypes.SecurityStamp);

        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrEmpty(tokenStamp))
        {
            context.Fail("The access token is missing required claims.");
            return;
        }

        var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
        var currentStamp = await userRepository.GetSecurityStampAsync(userId, context.HttpContext.RequestAborted);

        if (currentStamp is null || !string.Equals(currentStamp, tokenStamp, StringComparison.Ordinal))
        {
            context.Fail("The access token has been revoked.");
        }
    }
}
