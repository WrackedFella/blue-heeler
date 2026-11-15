using System.Net;
using System.Net.Http.Json;
using BlueHeeler.Api.Tests.Infrastructure;

namespace BlueHeeler.Api.Tests;

public sealed class AntiforgeryTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Post_WithoutToken_Returns400AndChangesNothing()
    {
        var admin = await SignedInAdminAsync();

        var response = await admin.SendWithTokenAsync(null, HttpMethod.Post, "/api/members", ApiRequests.MemberBody("Sam", "sam@example.org"));
        var members = await admin.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(members!);
    }

    [Fact]
    public async Task SignOut_WithoutToken_Returns400AndKeepsSession()
    {
        var admin = await SignedInAdminAsync();

        var response = await admin.SendWithTokenAsync(null, HttpMethod.Post, "/api/auth/sign-out");
        var me = await admin.GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Post_WithTokenFromAnotherBrowser_Returns400()
    {
        // An attacker can fetch a token for their own browser, but it doesn't match the victim's token cookie.
        var admin = await SignedInAdminAsync();
        var foreignToken = await _app.CreateBrowser().GetAntiforgeryTokenAsync();

        var response = await admin.SendWithTokenAsync(foreignToken, HttpMethod.Post, "/api/members", ApiRequests.MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithTokenIssuedBeforeSignIn_Returns400()
    {
        // Tokens are bound to the member, so one issued to the anonymous visitor stops working at sign-in.
        var browser = _app.CreateBrowser();
        var anonymousToken = await browser.GetAntiforgeryTokenAsync();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));

        var response = await browser.SendWithTokenAsync(anonymousToken, HttpMethod.Post, "/api/members", ApiRequests.MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithValidToken_Succeeds()
    {
        var admin = await SignedInAdminAsync();

        var response = await admin.AddMemberAsync(ApiRequests.MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_SignedOut_Returns401BeforeCheckingToken()
    {
        var response = await _app.CreateBrowser().SendWithTokenAsync(null, HttpMethod.Post, "/api/members", ApiRequests.MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutToken_IsAllowed()
    {
        var admin = await SignedInAdminAsync();

        var response = await admin.GetAsync(new Uri("/api/members", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_IsNotCached()
    {
        var response = await _app.CreateBrowser().GetAsync(new Uri("/api/antiforgery/token", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    private async Task<HttpClient> SignedInAdminAsync()
    {
        var admin = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(admin, new GoogleUser("admin@example.org", true, "Justin Admin"));
        return admin;
    }
}
