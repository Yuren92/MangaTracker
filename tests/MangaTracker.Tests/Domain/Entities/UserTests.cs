using AwesomeAssertions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Domain.Entities;

public sealed class UserTests
{
    [Fact]
    public void New_users_should_get_a_security_stamp()
    {
        var user = new User("reader@example.com", "hash");

        user.SecurityStamp.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Changing_the_password_should_rotate_the_security_stamp()
    {
        var user = new User("reader@example.com", "hash");
        var previousStamp = user.SecurityStamp;

        user.ChangePasswordHash("new-hash");

        user.SecurityStamp.Should().NotBe(previousStamp);
    }
}
