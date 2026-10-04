using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Application.Common.Security;
using MangaTracker.Domain.Entities;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.RegisterUser;

public sealed class RegisterUserHandler
{
    private static readonly TimeSpan EmailConfirmationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IAuthLinkBuilder authLinkBuilder,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _authLinkBuilder = authLinkBuilder;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailValidator.ValidateAndNormalize(command.Email);
        PasswordValidator.Validate(command.Password);

        // Hashed up front on every path: hashing is the slow step, so skipping it for
        // existing accounts would make them answer faster and give them away.
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        var existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            await NotifyExistingAccountAsync(existingUser.Email, cancellationToken);
            return GenericResult;
        }

        var user = new User(
            email: normalizedEmail,
            passwordHash: passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

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
        catch (UniqueConstraintViolationException)
        {
            // A concurrent registration created the account first: same outcome as above.
            _unitOfWork.DiscardChanges();
            await NotifyExistingAccountAsync(normalizedEmail, cancellationToken);
            return GenericResult;
        }

        var confirmationUrl = _authLinkBuilder.BuildEmailConfirmationUrl(confirmationToken);

        await _emailSender.SendEmailConfirmationAsync(
            to: user.Email,
            confirmationUrl: confirmationUrl,
            cancellationToken: cancellationToken);

        return GenericResult;
    }

    // The response never says whether the email was already registered, so the
    // endpoint cannot be used to find out who has an account. The owner of an existing
    // account learns about the attempt by email instead.
    private static readonly RegisterUserResult GenericResult = new(
        "If this email can be used, we have sent you a message with the next steps.");

    private Task NotifyExistingAccountAsync(string email, CancellationToken cancellationToken)
    {
        return _emailSender.SendRegistrationAttemptForExistingAccountAsync(
            to: email,
            forgotPasswordUrl: _authLinkBuilder.BuildForgotPasswordUrl(),
            cancellationToken: cancellationToken);
    }
}
