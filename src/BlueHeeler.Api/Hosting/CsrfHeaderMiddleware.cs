namespace BlueHeeler.Api.Hosting;

/// <summary>
/// Refuses state-changing API calls that lack <see cref="HeaderName"/>. A cross-site page cannot set a
/// custom header without a CORS preflight, which this API never grants, so the header proves the call
/// came from the app's own pages.
/// </summary>
internal sealed class CsrfHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-PAS-Request";

    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var isUnsafe = !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsOptions(request.Method));
        if (isUnsafe && request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) && !request.Headers.ContainsKey(HeaderName))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Missing request header",
                detail: $"State-changing API calls must send the {HeaderName} header.").ExecuteAsync(context);
        }

        return next(context);
    }
}
