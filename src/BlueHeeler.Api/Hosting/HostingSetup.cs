using BlueHeeler.Api.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace BlueHeeler.Api.Hosting;

internal static class HostingSetup
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddAppHosting(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Default is required.");
        }

        // GSS encryption is never used (Neon and the test database both use TLS), and probing for it
        // logs an error on chiseled images, which ship without Kerberos libraries.
        var npgsql = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable };
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(npgsql.ConnectionString).UseSnakeCaseNamingConvention());
        services.AddDataProtection().SetApplicationName("blue-heeler").PersistKeysToDbContext<AppDbContext>();
        services.AddSingleton(TimeProvider.System);
        services.AddProblemDetails();
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>(tags: [ReadyTag]);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Container Apps' ingress is the only way in and its addresses are not fixed, so trust any proxy.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }

    /// <summary>Applies pending migrations. EF Core holds a database lock while migrating, so overlapping starts are safe.</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync(app.Lifetime.ApplicationStopping);
    }
}
