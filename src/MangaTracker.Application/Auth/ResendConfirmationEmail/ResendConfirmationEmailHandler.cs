using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;
using MangaTracker.Domain.Entities;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailHandler
{
    private static readonly TimeSpan EmailConfirmationTokenLifetime = TimeSpan.FromHours(24);

    private const string GenericMessage =
        "If the email exists and is not confirmed, a confirmation email has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public ResendConfirmationEmailHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IAuthLinkBuilder authLinkBuilder,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _authLinkBuilder = authLinkBuilder;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ResendConfirmationEmailResult> HandleAsync(
        ResendConfirmationEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailValidator.ValidateAndNormalize(command.Email);

        var user = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (user is null || user.IsEmailConfirmed)
        {
            return new ResendConfirmationEmailResult(GenericMessage);
        }

        await _userTokenRepository.MarkActiveTokensAsUsedAsync(
            user.Id,
            UserTokenType.EmailConfirmation,
            cancellationToken);

        var confirmationToken = _tokenGenerator.GenerateSecureToken();
        var confirmationTokenHash = _tokenHasher.HashToken(confirmationToken);

        var userToken = new UserToken(
            userId: user.Id,
            tokenHash: confirmationTokenHash,
            type: UserTokenType.EmailConfirmation,
            expiresAt: _timeProvider.GetUtcNow().Add(EmailConfirmationTokenLifetime));

        await _userTokenRepository.AddAsync(userToken, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUpdateException)
        {
            // A concurrent request replaced the previous link first and is sending a
            // fresh one; this request has nothing left to do.
            return new ResendConfirmationEmailResult(GenericMessage);
        }

        var confirmationUrl = _authLinkBuilder.BuildEmailConfirmationUrl(confirmationToken);

        await _emailSender.SendEmailConfirmationAsync(
            to: user.Email,
            confirmationUrl: confirmationUrl,
            cancellationToken: cancellationToken);

        return new ResendConfirmationEmailResult(GenericMessage);
    }
}