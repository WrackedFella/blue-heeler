using System.Net.Http.Json;

namespace BlueHeeler.Api.Tests.Infrastructure;

/// <summary>Calls the API the way the SPA does, including the antiforgery token.</summary>
public static class ApiRequests
{
    public const string TokenHeader = "X-XSRF-TOKEN";

    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client)
    {
        var response = await client.GetFromJsonAsync<TokenResponse>("/api/antiforgery/token", TestContext.Current.CancellationToken);
        return response!.Token;
    }

    public static async Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string path, object? body = null) =>
        await client.SendWithTokenAsync(await client.GetAntiforgeryTokenAsync(), method, path, body);

    public static Task<HttpResponseMessage> SendWithTokenAsync(this HttpClient client, string? token, HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Add(TokenHeader, token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public static Task<HttpResponseMessage> AddMemberAsync(this HttpClient client, object body) =>
        client.SendAsync(HttpMethod.Post, "/api/members", body);

    public static Task<HttpResponseMessage> EditMemberAsync(this HttpClient client, Guid id, object body) =>
        client.SendAsync(HttpMethod.Put, $"/api/members/{id}", body);

    public static object MemberBody(string name, string email, string? role = "Walker", int? tier = 1, string? status = "Active") =>
        new { name, email, role, tier, status };

    private sealed record TokenResponse(string Token);
}
