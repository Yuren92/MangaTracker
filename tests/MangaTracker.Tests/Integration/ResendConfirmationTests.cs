using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MangaTracker.Tests.Integration.Infrastructure;

namespace MangaTracker.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ResendConfirmationTests
{
    private readonly MangaTrackerApiFactory _factory;
    private readonly HttpClient _client;

    public ResendConfirmationTests(MangaTrackerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Resend_should_answer_the_same_for_unknown_unconfirmed_and_confirmed_emails()
    {
        var unconfirmed = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, unconfirmed);

        var confirmed = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, confirmed);
        await TestUsers.ConfirmAsync(_client, _factory, confirmed);

        var unknown = TestUsers.NewEmail();

        var bodies = new List<string>();
        foreach (var email in new[] { unconfirmed, confirmed, unknown })
        {
            var response = await _client.PostAsJsonAsync("/api/auth/resend-confirmation-email", new { email });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            bodies.Add(await response.Content.ReadAsStringAsync());
        }

        bodies.Should().AllBe(bodies[0], "the response must not reveal which emails exist or are confirmed");

        // Only the account that still needs it gets a new link.
        _factory.Emails.CountFor(unconfirmed, EmailKind.Confirmation).Should().Be(2);
        _factory.Emails.CountFor(confirmed, EmailKind.Confirmation).Should().Be(1);
        _factory.Emails.CountFor(unknown, EmailKind.Confirmation).Should().Be(0);
    }

    [Fact]
    public async Task A_new_confirmation_link_should_invalidate_the_previous_one()
    {
        var email = TestUsers.NewEmail();
        await TestUsers.RegisterAsync(_client, email);
        var firstToken = _factory.Emails.LatestTokenFor(email, EmailKind.Confirmation);

        await _client.PostAsJsonAsync("/api/auth/resend-confirmation-email", new { email });
        var secondToken = _factory.Emails.LatestTokenFor(email, EmailKind.Confirmation);

        var withOld = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token = firstToken });
        var withNew = await _client.PostAsJsonAsync("/api/auth/confirm-email", new { token = secondToken });

        secondToken.Should().NotBe(firstToken);
        withOld.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        withNew.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
