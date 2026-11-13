using System.Net;
using System.Net.Http.Json;
using BlueHeeler.Api.Tests.Infrastructure;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Api.Tests;

public sealed class MembersEndpointTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _app.Dispose();

    private async Task<HttpClient> SignedInAdminAsync()
    {
        var browser = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));
        return browser;
    }

    [Fact]
    public async Task GetMembers_SignedOut_Returns401()
    {
        var response = await _app.CreateBrowser().GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_WalkerSignedIn_Returns403()
    {
        await _app.AddMemberAsync("Sam", "sam@example.org", Role.Walker, WalkerTier.Tier1);
        var browser = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("sam@example.org", true, "Sam"));

        var response = await browser.GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_MemberDemotedAfterSignIn_Returns403()
    {
        var browser = await SignedInAdminAsync();
        await _app.ExecuteSqlAsync($"UPDATE members SET role = 'Walker'");

        var response = await browser.GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_MemberDeactivatedAfterSignIn_Returns401()
    {
        var browser = await SignedInAdminAsync();
        await _app.ExecuteSqlAsync($"UPDATE members SET status = 'Inactive'");

        var response = await browser.GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_MemberDeletedAfterSignIn_Returns401()
    {
        var browser = await SignedInAdminAsync();
        await _app.ExecuteSqlAsync($"DELETE FROM members");

        var response = await browser.GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_SeveralMembers_ReturnsAllOrderedByName()
    {
        var browser = await SignedInAdminAsync();
        await _app.AddMemberAsync("Sam", "sam@example.org", Role.Walker, WalkerTier.Tier2);
        await _app.AddMemberAsync("Alex", "alex@example.org", Role.ShiftLead, WalkerTier.Tier3, MemberStatus.Inactive);

        var members = await browser.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Equal(["Alex", "Justin Admin", "Sam"], members!.Select(m => m.Name));
        var alex = members![0];
        Assert.Equal(("alex@example.org", "ShiftLead", "Inactive", (int?)3), (alex.Email, alex.Role, alex.Status, alex.Tier));
    }

    [Fact]
    public async Task UnknownApiPath_Returns404()
    {
        var response = await _app.CreateBrowser().GetAsync(new Uri("/api/nope", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
