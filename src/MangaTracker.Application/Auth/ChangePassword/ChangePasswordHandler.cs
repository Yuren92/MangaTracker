using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.ChangePassword;

public sealed class ChangePasswordHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ChangePasswordResult> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            throw new ValidationException("Current password is required.");
        }

        PasswordValidator.Validate(command.NewPassword);

        if (command.CurrentPassword == command.NewPassword)
        {
            throw new ValidationException("New password must be different from current password.");
        }

        var user = await _userRepository.GetByIdAsync(
            command.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Current user was not found.");
        }

        var isCurrentPasswordValid = _passwordHasher.VerifyPassword(
            command.CurrentPassword,
            user.PasswordHash);

        if (!isCurrentPasswordValid)
        {
            throw new ValidationException("Current password is invalid.");
        }

        var newPasswordHash = _passwordHasher.HashPassword(command.NewPassword);

        user.ChangePasswordHash(newPasswordHash);

        await _userTokenRepository.MarkActiveTokensAsUsedAsync(
            user.Id,
            UserTokenType.PasswordReset,
            cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new ChangePasswordResult("Password changed successfully.");
    }
}