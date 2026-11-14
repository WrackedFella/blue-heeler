using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueHeeler.Api.Tests.Infrastructure;
using static BlueHeeler.Api.Tests.Infrastructure.ApiRequests;

namespace BlueHeeler.Api.Tests;

public sealed class MemberManagementTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _app.Dispose();

    private async Task<HttpClient> AdminAsync()
    {
        var browser = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(browser, new GoogleUser("admin@example.org", true, "Justin Admin"));
        return browser;
    }

    private static async Task<MemberResponse> ReadMemberAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<MemberResponse>(Ct))!;

    private static async Task<MemberResponse> AddSamAsync(HttpClient admin)
    {
        var response = await admin.AddMemberAsync(MemberBody("Sam", "sam@example.org"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadMemberAsync(response);
    }

    [Fact]
    public async Task AddMember_ThenSignIn_SignsInAsActiveWalkerTier1()
    {
        var admin = await AdminAsync();
        await AddSamAsync(admin);
        var sam = _app.CreateBrowser();

        var landing = await _app.SignInWithGoogleAsync(sam, new GoogleUser("sam@example.org", true, "Sam G"));
        var me = await sam.GetFromJsonAsync<MeResponse>("/api/me", Ct);
        var listed = (await admin.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct))!.Single(m => m.Email == "sam@example.org");

        Assert.Equal("/members", landing);
        Assert.Equal(("Sam", "Walker"), (me!.Name, me.Role));
        Assert.Equal(("Active", (int?)1), (listed.Status, listed.Tier));
    }

    [Fact]
    public async Task AddMember_Created_ReturnsLocationOfTheMember()
    {
        var admin = await AdminAsync();

        var response = await admin.AddMemberAsync(MemberBody("  Sam  ", "Sam@Example.org", tier: null));
        var created = await ReadMemberAsync(response);
        var fetched = await admin.GetFromJsonAsync<MemberResponse>(response.Headers.Location!, Ct);

        Assert.Equal(new MemberResponse(created.Id, "Sam", "sam@example.org", "Walker", null, "Active"), created);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task EditMember_SetInactive_SignInRefused()
    {
        var admin = await AdminAsync();
        var sam = await AddSamAsync(admin);

        var edit = await admin.EditMemberAsync(sam.Id, MemberBody("Sam", "sam@example.org", status: "Inactive"));
        var landing = await _app.SignInWithGoogleAsync(_app.CreateBrowser(), new GoogleUser("sam@example.org", true, "Sam"));

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal("/sign-in?error=inactive", landing);
    }

    [Theory]
    [InlineData("ShiftLead", 1)]
    [InlineData("Walker", 2)]
    public async Task EditMember_ChangeRoleOrTier_ListShowsChange(string role, int tier)
    {
        var admin = await AdminAsync();
        var sam = await AddSamAsync(admin);

        var edit = await admin.EditMemberAsync(sam.Id, MemberBody("Sam", "sam@example.org", role, tier));
        var listed = (await admin.GetFromJsonAsync<MemberResponse[]>("/api/members", Ct))!.Single(m => m.Id == sam.Id);

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal((role, (int?)tier), (listed.Role, listed.Tier));
        Assert.Equal(listed, await ReadMemberAsync(edit));
    }

    [Fact]
    public async Task AddMember_AsWalker_Returns403()
    {
        var admin = await AdminAsync();
        await AddSamAsync(admin);
        var sam = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(sam, new GoogleUser("sam@example.org", true, "Sam"));

        var response = await sam.AddMemberAsync(MemberBody("Alex", "alex@example.org"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EditMember_AsWalker_Returns403()
    {
        var admin = await AdminAsync();
        var samMember = await AddSamAsync(admin);
        var sam = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(sam, new GoogleUser("sam@example.org", true, "Sam"));

        var response = await sam.EditMemberAsync(samMember.Id, MemberBody("Sam", "sam@example.org", "Admin", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_SignedOut_Returns401()
    {
        var response = await _app.CreateBrowser().AddMemberAsync(MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_InvalidInput_Returns400WithFieldErrors()
    {
        var admin = await AdminAsync();

        var response = await admin.AddMemberAsync(MemberBody("", "nope", role: null, tier: 7));
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            ["email", "name", "role", "tier"],
            problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order());
    }

    [Fact]
    public async Task AddMember_UnknownRoleName_Returns400()
    {
        var admin = await AdminAsync();

        var response = await admin.AddMemberAsync(MemberBody("Sam", "sam@example.org", role: "Overlord"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_EmailInUse_Returns409()
    {
        var admin = await AdminAsync();
        await AddSamAsync(admin);

        var response = await admin.AddMemberAsync(MemberBody("Sam Again", "SAM@example.org"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task EditMember_EmailOfAnotherMember_Returns409()
    {
        var admin = await AdminAsync();
        var sam = await AddSamAsync(admin);

        var response = await admin.EditMemberAsync(sam.Id, MemberBody("Sam", "admin@example.org"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task EditMember_LastActiveAdminDemoted_Returns409AndKeepsAdmin()
    {
        var admin = await AdminAsync();
        var self = (await admin.GetFromJsonAsync<MeResponse>("/api/me", Ct))!;

        var response = await admin.EditMemberAsync(self.Id, MemberBody("Justin Admin", "admin@example.org", "Walker", null));
        var me = await admin.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Admin", me!.Role);
    }

    [Fact]
    public async Task EditMember_AdminDemotedWhileAnotherActiveAdminExists_Succeeds()
    {
        var admin = await AdminAsync();
        await admin.AddMemberAsync(MemberBody("Pat", "pat@example.org", "Admin", null));
        var self = (await admin.GetFromJsonAsync<MeResponse>("/api/me", Ct))!;

        var response = await admin.EditMemberAsync(self.Id, MemberBody("Justin Admin", "admin@example.org", "Walker", 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EditMember_UnknownId_Returns404()
    {
        var admin = await AdminAsync();

        var response = await admin.EditMemberAsync(Guid.NewGuid(), MemberBody("Sam", "sam@example.org"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMember_UnknownId_Returns404()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync(new Uri($"/api/members/{Guid.NewGuid()}", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData("0")]
    public async Task AddMember_NumericRole_Returns400(object role)
    {
        var admin = await AdminAsync();

        var response = await admin.AddMemberAsync(new { name = "Sam", email = "sam@example.org", role, tier = 1, status = "Active" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMember_AsWalker_Returns403()
    {
        var admin = await AdminAsync();
        var samMember = await AddSamAsync(admin);
        var sam = _app.CreateBrowser();
        await _app.SignInWithGoogleAsync(sam, new GoogleUser("sam@example.org", true, "Sam"));

        var response = await sam.GetAsync(new Uri($"/api/members/{samMember.Id}", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EditMember_LastActiveAdminChangesEmail_Returns409AndKeepsEmail()
    {
        var admin = await AdminAsync();
        var self = (await admin.GetFromJsonAsync<MeResponse>("/api/me", Ct))!;

        var response = await admin.EditMemberAsync(self.Id, MemberBody("Justin Admin", "admin@exmaple.org", "Admin", null));
        var me = await admin.GetFromJsonAsync<MeResponse>("/api/me", Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("admin@example.org", me!.Email);
    }
}
