using System.Net;
using System.Net.Http.Json;
using BlueHeeler.Api.Tests.Infrastructure;

namespace BlueHeeler.Api.Tests;

public sealed class CsrfHeaderTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Post_WithoutHeader_Returns400AndChangesNothing()
    {
        var admin = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(admin, new GoogleUser("admin@example.org", true, "Justin Admin"));

        var response = await admin.PostAsJsonAsync("/api/members", ApiRequests.MemberBody("Sam", "sam@example.org"), Ct);
        var members = await admin.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(members!);
    }

    [Fact]
    public async Task SignOut_WithoutHeader_Returns400AndKeepsSession()
    {
        var admin = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(admin, new GoogleUser("admin@example.org", true, "Justin Admin"));

        var response = await admin.PostAsync(new Uri("/api/auth/sign-out", UriKind.Relative), content: null, Ct);
        var me = await admin.GetAsync(new Uri("/api/me", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutHeader_IsAllowed()
    {
        var response = await _app.CreateBrowser().GetAsync(new Uri("/healthz", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
