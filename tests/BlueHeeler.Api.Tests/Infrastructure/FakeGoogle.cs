using System.Net;
using System.Net.Http.Json;

namespace BlueHeeler.Api.Tests.Infrastructure;

/// <summary>
/// Stands in for Google's token and userinfo endpoints on the Google handler's backchannel,
/// so the real challenge, state and callback run in-process.
/// </summary>
public sealed class FakeGoogle : HttpMessageHandler
{
    public GoogleUser NextUser { get; set; } = new("admin@example.org", EmailVerified: true, "Justin Admin");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object body = request.RequestUri!.AbsoluteUri switch
        {
            "https://oauth2.googleapis.com/token" => new { access_token = "fake-access-token", token_type = "Bearer", expires_in = 3600 },
            "https://www.googleapis.com/oauth2/v3/userinfo" => new
            {
                sub = "google-" + NextUser.Email,
                email = NextUser.Email,
                email_verified = NextUser.EmailVerified,
                name = NextUser.Name,
            },
            var other => throw new InvalidOperationException($"Unexpected backchannel call to {other}"),
        };

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
    }
}

public sealed record GoogleUser(string Email, bool EmailVerified, string Name);
