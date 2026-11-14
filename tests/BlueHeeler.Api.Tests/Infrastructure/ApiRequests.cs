using System.Net.Http.Json;

namespace BlueHeeler.Api.Tests.Infrastructure;

/// <summary>Calls the API the way the SPA does, including the cross-site request header.</summary>
public static class ApiRequests
{
    public const string RequestHeader = "X-PAS-Request";

    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(RequestHeader, "1");
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
}
