using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Auth.ConfirmEmail;

public sealed class ConfirmEmailHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailHandler(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfirmEmailResult> HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            throw new ValidationException("Confirmation token is required.");
        }

        var tokenHash = _tokenHasher.HashToken(command.Token);

        var userToken = await _userTokenRepository.GetActiveTokenAsync(
            tokenHash,
            UserTokenType.EmailConfirmation,
            cancellationToken);

        if (userToken is null)
        {
            throw new ValidationException("Confirmation token is invalid or expired.");
        }

        var user = await _userRepository.GetByIdAsync(
            userToken.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User was not found.");
        }

        user.ConfirmEmail();
        userToken.MarkAsUsed();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailSender.SendWelcomeAsync(
            user.Email,
            cancellationToken);

        return new ConfirmEmailResult("Email confirmed successfully.");
    }
}