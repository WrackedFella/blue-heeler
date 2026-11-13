using System.Net;
using BlueHeeler.Api.Tests.Infrastructure;

namespace BlueHeeler.Api.Tests;

public sealed class HealthTests(PostgresFixture postgres) : IDisposable
{
    private readonly AppFactory _app = new(postgres);

    public void Dispose() => _app.Dispose();

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/readyz")]
    public async Task HealthEndpoint_WithDatabase_Returns200(string path)
    {
        var response = await _app.CreateBrowser().GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
