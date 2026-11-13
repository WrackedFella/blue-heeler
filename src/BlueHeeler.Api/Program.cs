using BlueHeeler.Api.Auth;
using BlueHeeler.Api.Hosting;
using BlueHeeler.Api.Members;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppHosting(builder.Configuration);
builder.Services.AddAppAuth(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapMemberEndpoints();
// Client-side routes get the SPA; unknown API paths stay 404 (the literal "api" segment outranks the catch-all).
app.MapFallbackToFile("index.html");
app.MapFallback("/api/{**path}", () => Results.NotFound());

// Fail on bad configuration before touching the database, not after migrating it.
app.Services.GetRequiredService<Microsoft.Extensions.Options.IStartupValidator>().Validate();
await app.MigrateDatabaseAsync();
await app.RunAsync();

/// <summary>Entry point, public so integration tests can host the app.</summary>
public partial class Program;
