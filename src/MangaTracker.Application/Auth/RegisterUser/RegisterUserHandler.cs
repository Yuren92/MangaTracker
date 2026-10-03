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

    public RegisterUserHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IAuthLinkBuilder authLinkBuilder,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _authLinkBuilder = authLinkBuilder;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUserResult> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailValidator.ValidateAndNormalize(command.Email);

        PasswordValidator.Validate(command.Password);

        var existingUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (existingUser is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var passwordHash = _passwordHasher.HashPassword(command.Password);

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
            expiresAt: DateTimeOffset.UtcNow.Add(EmailConfirmationTokenLifetime));

        await _userTokenRepository.AddAsync(userToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var confirmationUrl = _authLinkBuilder.BuildEmailConfirmationUrl(confirmationToken);

        await _emailSender.SendEmailConfirmationAsync(
            to: user.Email,
            confirmationUrl: confirmationUrl,
            cancellationToken: cancellationToken);

        return new RegisterUserResult(
            UserId: user.Id,
            Email: user.Email);
    }
}