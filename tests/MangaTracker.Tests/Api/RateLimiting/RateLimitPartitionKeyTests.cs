using System.Net;
using System.Security.Claims;
using FluentAssertions;
using MangaTracker.Api.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace MangaTracker.Tests.Api.RateLimiting;

public sealed class RateLimitPartitionKeyTests
{
    [Fact]
    public void For_should_partition_authenticated_requests_by_user_id()
    {
        var userId = Guid.NewGuid().ToString();
        var context = CreateContext("203.0.113.10");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            authenticationType: "Bearer"));

        RateLimitPartitionKey.For(context).Should().Be($"user:{userId}");
    }

    [Fact]
    public void For_should_give_two_users_behind_the_same_ip_independent_partitions()
    {
        var first = CreateAuthenticatedContext("203.0.113.10", Guid.NewGuid());
        var second = CreateAuthenticatedContext("203.0.113.10", Guid.NewGuid());

        RateLimitPartitionKey.For(first).Should().NotBe(RateLimitPartitionKey.For(second));
    }

    [Fact]
    public void For_should_partition_anonymous_requests_by_ip()
    {
        var context = CreateContext("203.0.113.10");

        RateLimitPartitionKey.For(context).Should().Be("ip:203.0.113.10");
    }

    [Fact]
    public void For_should_not_trust_a_user_id_claim_on_an_unauthenticated_identity()
    {
        var context = CreateContext("203.0.113.10");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())]));

        RateLimitPartitionKey.For(context).Should().Be("ip:203.0.113.10");
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string ipAddress, Guid userId)
    {
        var context = CreateContext(ipAddress);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "Bearer"));

        return context;
    }

    private static DefaultHttpContext CreateContext(string ipAddress)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

        return context;
    }
}
