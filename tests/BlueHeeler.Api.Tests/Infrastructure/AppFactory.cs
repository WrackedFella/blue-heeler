using System.Web;
using BlueHeeler.Api.Data;
using BlueHeeler.Domain.Members;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueHeeler.Api.Tests.Infrastructure;

/// <summary>The real app on a fresh database, with Google faked at the network edge.</summary>
public sealed class AppFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public const string FirstAdminEmail = "admin@example.org";

    private readonly string _database = "t_" + Guid.NewGuid().ToString("N");

    public FakeGoogle Google { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", postgres.ConnectionStringFor(_database));
        builder.UseSetting("Authentication:Google:ClientId", "test-client-id");
        builder.UseSetting("Authentication:Google:ClientSecret", "test-client-secret");
        builder.UseSetting("Bootstrap:FirstAdminEmail", FirstAdminEmail);
        builder.ConfigureServices(services =>
            services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme, o => o.Backchannel = new HttpClient(Google)));
    }

    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
    });

    /// <summary>Runs the Google sign-in round trip and returns where the app sent the browser afterwards.</summary>
    public async Task<string> SignInWithGoogleAsync(HttpClient browser, GoogleUser user)
    {
        Google.NextUser = user;

        var challenge = await browser.GetAsync(new Uri("/api/auth/google", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, challenge.StatusCode);
        var state = HttpUtility.ParseQueryString(challenge.Headers.Location!.Query)["state"];

        var callback = await browser.GetAsync(
            new Uri($"/signin-google?code=fake-code&state={Uri.EscapeDataString(state!)}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Redirect, callback.StatusCode);
        return callback.Headers.Location!.OriginalString;
    }

    public async Task AddMemberAsync(string name, string email, Role role, WalkerTier? tier, MemberStatus status = MemberStatus.Active)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Members.Add(new Member(Guid.NewGuid(), name, EmailAddress.TryCreate(email)!, role, tier, status, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async Task ExecuteSqlAsync(FormattableString sql)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlAsync(sql, TestContext.Current.CancellationToken);
    }
}
