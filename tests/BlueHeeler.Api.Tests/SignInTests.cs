using System.Net;
using System.Net.Http.Json;
using BlueHeeler.Api.Tests.Infrastructure;

namespace BlueHeeler.Api.Tests;

public sealed class SignInTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task GoogleSignIn_ConfiguredFirstAdmin_SeesSelfAsActiveAdmin()
    {
        var browser = _app.CreateBrowser();

        var landing = await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));
        var members = await browser.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Equal("/members", landing);
        var self = Assert.Single(members!);
        Assert.Equal(("Justin Admin", "admin@example.org", "Admin", "Active", (int?)null), (self.Name, self.Email, self.Role, self.Status, self.Tier));
    }

    [Fact]
    public async Task GoogleSignIn_FirstAdminEmailInOtherCase_BootstrapsNormalisedEmail()
    {
        var browser = _app.CreateBrowser();

        await _app.SignInWithGoogleAsync(browser, new GoogleUser("Admin@Example.ORG", true, "Justin Admin"));
        var members = await browser.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Equal("admin@example.org", Assert.Single(members!).Email);
    }

    [Fact]
    public async Task GoogleSignIn_SecondSignInOfFirstAdmin_DoesNotDuplicate()
    {
        await _app.SignInWithGoogleAsync(_app.CreateBrowser(), new GoogleUser("admin@example.org", true, "Justin Admin"));
        var browser = _app.CreateBrowser();

        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));
        var members = await browser.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Single(members!);
    }

    [Fact]
    public async Task GoogleSignIn_UnknownEmail_RedirectsWithErrorAndNoCookie()
    {
        var browser = _app.CreateBrowser();

        var landing = await _app.SignInWithGoogleAsync(browser, new GoogleUser("stranger@example.org", true, "Stranger"));
        var me = await browser.GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        Assert.Equal("/sign-in?error=not-a-member", landing);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task GoogleSignIn_UnverifiedEmail_RedirectsWithErrorAndNoCookie()
    {
        var browser = _app.CreateBrowser();

        var landing = await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", false, "Impostor"));
        var me = await browser.GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        Assert.Equal("/sign-in?error=email-not-verified", landing);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task GoogleSignIn_InactiveMember_RedirectsWithError()
    {
        await _app.AddMemberAsync("Sam", "sam@example.org", Domain.Members.Role.Walker, Domain.Members.WalkerTier.Tier1, Domain.Members.MemberStatus.Inactive);
        var browser = _app.CreateBrowser();

        var landing = await _app.SignInWithGoogleAsync(browser, new GoogleUser("sam@example.org", true, "Sam"));

        Assert.Equal("/sign-in?error=inactive", landing);
    }

    [Fact]
    public async Task Me_SignedInAdmin_ReturnsIdentityAndPermissions()
    {
        var browser = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));

        var me = await browser.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal("Justin Admin", me!.Name);
        Assert.Equal("Admin", me.Role);
        Assert.Equal(["ViewMembers"], me.Permissions);
    }

    [Fact]
    public async Task SignOut_SignedIn_EndsSession()
    {
        var browser = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));

        var signOut = await browser.PostAsync(new Uri("/api/auth/sign-out", UriKind.Relative), content: null, Ct);
        var me = await browser.GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    [Fact]
    public async Task GoogleSignIn_Callback_SetsHardenedSessionCookie()
    {
        var browser = _app.CreateBrowser();
        _app.Google.NextUser = new GoogleUser("admin@example.org", true, "Justin Admin");
        var challenge = await browser.GetAsync(new Uri("/api/auth/google", UriKind.Relative), Ct);
        var state = System.Web.HttpUtility.ParseQueryString(challenge.Headers.Location!.Query)["state"];

        var callback = await browser.GetAsync(new Uri($"/signin-google?code=c&state={Uri.EscapeDataString(state!)}", UriKind.Relative), Ct);

        var session = Assert.Single(callback.Headers.GetValues("Set-Cookie"), c => c.StartsWith("__Host-pas-session=", StringComparison.Ordinal));
        var attributes = session.Split(';', StringSplitOptions.TrimEntries).Skip(1).Select(a => a.ToLowerInvariant()).ToArray();
        Assert.Contains("path=/", attributes);
        Assert.Contains("secure", attributes);
        Assert.Contains("httponly", attributes);
        Assert.Contains("samesite=lax", attributes);
        Assert.DoesNotContain(attributes, a => a.StartsWith("domain=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GoogleChallenge_BehindTlsTerminatingProxy_UsesHttpsRedirectUri()
    {
        var plainHttp = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/google");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var challenge = await plainHttp.SendAsync(request, Ct);

        var redirectUri = System.Web.HttpUtility.ParseQueryString(challenge.Headers.Location!.Query)["redirect_uri"];
        Assert.Equal("https://localhost/signin-google", redirectUri);
    }

    [Fact]
    public async Task GoogleSignIn_FirstAdminAfterConcurrentBootstrap_SignsInWithoutDuplicate()
    {
        var first = _app.CreateBrowser();
        var second = _app.CreateBrowser();
        var user = new GoogleUser("admin@example.org", true, "Justin Admin");

        var landings = await Task.WhenAll(_app.SignInWithGoogleAsync(first, user), _app.SignInWithGoogleAsync(second, user));
        var members = await second.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.All(landings, l => Assert.Equal("/members", l));
        Assert.Single(members!);
    }
}
