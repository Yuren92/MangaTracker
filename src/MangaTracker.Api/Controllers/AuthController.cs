using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Auth.ChangePassword;
using MangaTracker.Application.Auth.ConfirmEmail;
using MangaTracker.Application.Auth.ForgotPassword;
using MangaTracker.Application.Auth.GetCurrentUser;
using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Application.Auth.RegisterUser;
using MangaTracker.Application.Auth.ResendConfirmationEmail;
using MangaTracker.Application.Auth.ResetPassword;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MangaTracker.Api.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;
    private readonly GetCurrentUserHandler _getCurrentUserHandler;
    private readonly ConfirmEmailHandler _confirmEmailHandler;
    private readonly ICurrentUserService _currentUserService;
    private readonly ResendConfirmationEmailHandler _resendConfirmationEmailHandler;
    private readonly ForgotPasswordHandler _forgotPasswordHandler;
    private readonly ResetPasswordHandler _resetPasswordHandler;
    private readonly ChangePasswordHandler _changePasswordHandler;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginUserHandler loginUserHandler,
        GetCurrentUserHandler getCurrentUserHandler,
        ICurrentUserService currentUserService,
        ConfirmEmailHandler confirmEmailHandler,
        ResendConfirmationEmailHandler resendConfirmationEmailHandler,
        ForgotPasswordHandler forgotPasswordHandler,
        ResetPasswordHandler resetPasswordHandler,
        ChangePasswordHandler changePasswordHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _currentUserService = currentUserService;
        _resendConfirmationEmailHandler = resendConfirmationEmailHandler;
        _confirmEmailHandler = confirmEmailHandler;
        _forgotPasswordHandler = forgotPasswordHandler;
        _resetPasswordHandler = resetPasswordHandler;
        _changePasswordHandler = changePasswordHandler;
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            Email: request.Email,
            Password: request.Password);

        var result = await _registerUserHandler.HandleAsync(
            command,
            cancellationToken);

        // 202 for new and existing emails alike, so the status code reveals nothing either.
        return Accepted(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginUserResult>> Login(
        LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginUserCommand(
            Email: request.Email,
            Password: request.Password);

        var result = await _loginUserHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<GetCurrentUserResult>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var result = await _getCurrentUserHandler.HandleAsync(
            _currentUserService.UserId,
            cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("confirm-email")]
    public async Task<ActionResult<ConfirmEmailResult>> ConfirmEmail(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(
            Token: request.Token);

        var result = await _confirmEmailHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("resend-confirmation-email")]
    public async Task<ActionResult<ResendConfirmationEmailResult>> ResendConfirmationEmail(
        ResendConfirmationEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResendConfirmationEmailCommand(
            Email: request.Email);

        var result = await _resendConfirmationEmailHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(
            Email: request.Email);

        var result = await _forgotPasswordHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting(RateLimitPolicies.AuthSensitive)]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ResetPasswordResult>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(
            Token: request.Token,
            NewPassword: request.NewPassword);

        var result = await _resetPasswordHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ChangePasswordResult>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(
            UserId: _currentUserService.UserId,
            CurrentPassword: request.CurrentPassword,
            NewPassword: request.NewPassword);

        var result = await _changePasswordHandler.HandleAsync(
            command,
            cancellationToken);

        return Ok(result);
    }
}

public sealed record RegisterUserRequest(
    string Email,
    string Password
);

public sealed record LoginUserRequest(
    string Email,
    string Password
);

public sealed record ConfirmEmailRequest(
    string Token
);

public sealed record ResendConfirmationEmailRequest(
    string Email
);

public sealed record ForgotPasswordRequest(
    string Email
);

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
