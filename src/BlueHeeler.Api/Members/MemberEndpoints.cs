using BlueHeeler.Api.Auth;
using BlueHeeler.Api.Data;
using BlueHeeler.Domain.Access;
using Microsoft.EntityFrameworkCore;

namespace BlueHeeler.Api.Members;

internal static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/members", async (AppDbContext db, CancellationToken ct) =>
                (await db.Members.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct)).Select(MemberResponse.From))
            .RequirePermission(Permission.ViewMembers);

        return app;
    }
}
