using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;
using MangaTracker.Domain.Entities;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.ForgotPassword;

public sealed class ForgotPasswordHandler
{
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private const string GenericMessage =
        "If the email exists, a password reset email has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IAuthLinkBuilder authLinkBuilder,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _authLinkBuilder = authLinkBuilder;
        _emailSender = emailSender;
    }

    public async Task<ForgotPasswordResult> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailValidator.ValidateAndNormalize(command.Email);

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null)
        {
            return new ForgotPasswordResult(GenericMessage);
        }

        await _userTokenRepository.MarkActiveTokensAsUsedAsync(
            user.Id,
            UserTokenType.PasswordReset,
            cancellationToken);

        var resetToken = _tokenGenerator.GenerateSecureToken();
        var resetTokenHash = _tokenHasher.HashToken(resetToken);

        var userToken = new UserToken(
            userId: user.Id,
            tokenHash: resetTokenHash,
            type: UserTokenType.PasswordReset,
            expiresAt: DateTimeOffset.UtcNow.Add(PasswordResetTokenLifetime));

        await _userTokenRepository.AddAsync(userToken, cancellationToken);
        await _userTokenRepository.SaveChangesAsync(cancellationToken);

        var resetPasswordUrl = _authLinkBuilder.BuildPasswordResetUrl(resetToken);

        await _emailSender.SendPasswordResetAsync(
            to: user.Email,
            resetPasswordUrl: resetPasswordUrl,
            cancellationToken: cancellationToken);

        return new ForgotPasswordResult(GenericMessage);
    }
}