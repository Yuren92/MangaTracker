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
    private readonly IUnitOfWork _unitOfWork;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
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

        var verification = _passwordHasher.VerifyPassword(
            command.Password,
            user?.PasswordHash ?? GetUnknownUserPasswordHash());

        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            throw new ValidationException("Invalid email or password.");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new ValidationException("Email is not confirmed.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // The plain password is only available here, at login, so this is when a hash
            // made with older parameters (e.g. Identity 2.x, 10,000 PBKDF2 iterations) is
            // replaced by one with the current ones.
            user.UpgradePasswordHash(_passwordHasher.HashPassword(command.Password));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var accessToken = _jwtTokenGenerator.GenerateToken(user);

        return new LoginUserResult(accessToken);
    }

    private string GetUnknownUserPasswordHash()
    {
        return _unknownUserPasswordHash ??= _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));
    }
}
