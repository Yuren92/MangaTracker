using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Application.Auth.RegisterUser;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Auth.GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginUserHandler _loginUserHandler;
    private readonly GetCurrentUserHandler _getCurrentUserHandler;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        RegisterUserHandler registerUserHandler,
        LoginUserHandler loginUserHandler,
        GetCurrentUserHandler getCurrentUserHandler,
        ICurrentUserService currentUserService)
    {
        _registerUserHandler = registerUserHandler;
        _loginUserHandler = loginUserHandler;
        _getCurrentUserHandler = getCurrentUserHandler;
        _currentUserService = currentUserService;
    }

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

        return CreatedAtAction(
            nameof(Register),
            new { id = result.UserId },
            result);
    }

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
}

public sealed record RegisterUserRequest(
    string Email,
    string Password
);

public sealed record LoginUserRequest(
    string Email,
    string Password
);