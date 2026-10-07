using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Auth.LoginUser;
using MangaTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;
using PasswordHasher = MangaTracker.Infrastructure.Auth.PasswordHasher;
using PasswordVerificationResult = MangaTracker.Application.Abstractions.Auth.PasswordVerificationResult;

namespace MangaTracker.Tests.Application.Auth;

public sealed class PasswordRehashTests
{
    private const string Email = "reader@example.com";
    private const string Password = "Correct-Horse-1";

    [Fact]
    public void Hashes_made_with_the_old_iteration_count_should_ask_for_a_rehash()
    {
        // Identity 2.x hashed with PBKDF2 and 10,000 iterations; current versions use more.
        var legacyHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        var legacyHash = legacyHasher.HashPassword(null!, Password);

        var hasher = new PasswordHasher();

        hasher.VerifyPassword(Password, legacyHash).Should().Be(PasswordVerificationResult.SuccessRehashNeeded);
        hasher.VerifyPassword(Password, hasher.HashPassword(Password)).Should().Be(PasswordVerificationResult.Success);
        hasher.VerifyPassword("Wrong-Password-1", legacyHash).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public async Task Login_should_upgrade_an_outdated_hash_without_signing_out_other_sessions()
    {
        var user = new User(Email, "legacy-hash");
        user.ConfirmEmail();
        var stampBefore = user.SecurityStamp;

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(user);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.VerifyPassword(Password, "legacy-hash").Returns(PasswordVerificationResult.SuccessRehashNeeded);
        passwordHasher.HashPassword(Password).Returns("fresh-hash");

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new LoginUserHandler(userRepository, passwordHasher, Substitute.For<IJwtTokenGenerator>(), unitOfWork);

        await handler.HandleAsync(new LoginUserCommand(Email, Password));

        user.PasswordHash.Should().Be("fresh-hash");
        user.SecurityStamp.Should().Be(stampBefore);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
