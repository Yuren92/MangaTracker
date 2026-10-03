using FluentAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Application.Auth.RegisterUser;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using NSubstitute;

namespace MangaTracker.Tests.Application.Auth;

// Response bodies are covered by the integration tests. These check the timing side:
// the expensive password hashing must run whether or not the account exists.
public sealed class AccountEnumerationTests
{
    private const string Email = "reader@example.com";
    private const string Password = "Correct-Horse-1";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    public AccountEnumerationTests()
    {
        _passwordHasher.HashPassword(Arg.Any<string>()).Returns("hash");
    }

    [Fact]
    public async Task Login_with_an_unknown_email_should_still_verify_a_password_hash()
    {
        _userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = new LoginUserHandler(_userRepository, _passwordHasher, Substitute.For<IJwtTokenGenerator>());

        var act = () => handler.HandleAsync(new LoginUserCommand(Email, Password));

        await act.Should().ThrowAsync<ValidationException>().WithMessage("Invalid email or password.");
        _passwordHasher.Received(1).VerifyPassword(Password, Arg.Any<string>());
    }

    [Fact]
    public async Task Registering_an_existing_email_should_hash_the_password_and_notify_instead_of_creating()
    {
        _userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(new User(Email, "existing-hash"));
        var emailSender = Substitute.For<IEmailSender>();

        var handler = new RegisterUserHandler(
            _userRepository,
            Substitute.For<IUserTokenRepository>(),
            _passwordHasher,
            Substitute.For<ITokenGenerator>(),
            Substitute.For<ITokenHasher>(),
            Substitute.For<IAuthLinkBuilder>(),
            emailSender,
            Substitute.For<IUnitOfWork>(),
            TimeProvider.System);

        var result = await handler.HandleAsync(new RegisterUserCommand(Email, Password));

        result.Message.Should().StartWith("If this email can be used");
        _passwordHasher.Received(1).HashPassword(Password);
        await _userRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await emailSender.Received(1).SendRegistrationAttemptForExistingAccountAsync(Email, Arg.Any<string?>()!, Arg.Any<CancellationToken>());
        await emailSender.DidNotReceiveWithAnyArgs().SendEmailConfirmationAsync(default!, default!, default);
    }
}
