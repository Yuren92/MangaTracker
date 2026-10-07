using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.ResetPassword;

public sealed class ResetPasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenHasher = tokenHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<ResetPasswordResult> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            throw new ValidationException("Password reset token is required.");
        }

        PasswordValidator.Validate(command.NewPassword);

        var tokenHash = _tokenHasher.HashToken(command.Token);

        var userToken = await _userTokenRepository.GetActiveTokenAsync(
            tokenHash,
            UserTokenType.PasswordReset,
            cancellationToken);

        if (userToken is null)
        {
            throw new ValidationException("Password reset token is invalid or expired.");
        }

        var user = await _userRepository.GetByIdAsync(
            userToken.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        var newPasswordHash = _passwordHasher.HashPassword(command.NewPassword);

        user.ChangePasswordHash(newPasswordHash);

        await _userTokenRepository.MarkActiveTokensAsUsedAsync(
            user.Id,
            UserTokenType.PasswordReset,
            cancellationToken);

        userToken.MarkAsUsed();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            // A concurrent request used the same link first: it is single-use.
            throw new ValidationException("Password reset token is invalid or expired.");
        }

        return new ResetPasswordResult("Password reset successfully.");
    }
}