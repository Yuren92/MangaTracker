using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;

namespace MangaTracker.Application.Auth.LoginUser;

public sealed class LoginUserHandler
{
    // Hash of a random value, computed once. Unknown emails are verified against it so
    // they take as long as a wrong password and response times reveal nothing.
    private static string? _unknownUserPasswordHash;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginUserResult> HandleAsync(
        LoginUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailValidator.ValidateAndNormalize(command.Email);

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ValidationException("Password is required.");
        }

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        var isPasswordValid = _passwordHasher.VerifyPassword(
            command.Password,
            user?.PasswordHash ?? GetUnknownUserPasswordHash());

        if (user is null || !isPasswordValid)
        {
            throw new ValidationException("Invalid email or password.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new ValidationException("Email is not confirmed.");
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user);

        return new LoginUserResult(accessToken);
    }

    private string GetUnknownUserPasswordHash()
    {
        return _unknownUserPasswordHash ??= _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));
    }
}
