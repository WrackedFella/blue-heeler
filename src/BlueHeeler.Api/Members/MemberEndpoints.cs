using BlueHeeler.Api.Auth;
using BlueHeeler.Api.Data;
using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BlueHeeler.Api.Members;

internal static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var members = app.MapGroup("/api/members");

        members.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
                (await db.Members.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct)).Select(MemberResponse.From))
            .RequirePermission(Permission.ViewMembers);

        members.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
                await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, ct) is { } member
                    ? Results.Ok(MemberResponse.From(member))
                    : Results.NotFound())
            .WithName(GetMemberRoute)
            .RequirePermission(Permission.ViewMembers);

        members.MapPost("/", AddAsync).RequirePermission(Permission.ManageMembers);
        members.MapPut("/{id:guid}", EditAsync).RequirePermission(Permission.ManageMembers);

        return app;
    }

    private const string GetMemberRoute = "GetMember";

    private static Task<IResult> AddAsync(MemberRequest request, AppDbContext db, TimeProvider time, CancellationToken ct) =>
        WithValidDetails(request, async details =>
        {
            var now = time.GetUtcNow();
            var member = Member.Add(Guid.CreateVersion7(now), details, now);
            db.Members.Add(member);
            return await TrySaveAsync(db, ct)
                ? Results.CreatedAtRoute(GetMemberRoute, new { id = member.Id }, MemberResponse.From(member))
                : EmailInUse();
        });

    private static Task<IResult> EditAsync(Guid id, MemberRequest request, AppDbContext db, CancellationToken ct) =>
        WithValidDetails(request, async details =>
        {
            var member = await db.Members.SingleOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
            {
                return Results.NotFound();
            }

            var activeAdmins = await db.Members.CountAsync(m => m.Role == Role.Admin && m.Status == MemberStatus.Active, ct);
            if (RosterPolicy.CheckEdit(member, details, activeAdmins) is EditRefusal.LastActiveAdmin)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Last active admin",
                    detail: "This is the only active admin. Make someone else an admin first.");
            }

            member.Apply(details);
            return await TrySaveAsync(db, ct) ? Results.Ok(MemberResponse.From(member)) : EmailInUse();
        });

    /// <summary>Runs <paramref name="handle"/> on valid input, or answers 400 with the field errors.</summary>
    private static Task<IResult> WithValidDetails(MemberRequest request, Func<MemberDetails, Task<IResult>> handle) =>
        request.ToDetails() switch
        {
            MemberDetailsResult.Valid(var details) => handle(details),
            MemberDetailsResult.Invalid(var errors) => Task.FromResult(Results.ValidationProblem(errors.ToDictionary())),
            var other => throw new InvalidOperationException($"Unhandled result {other.GetType().Name}."),
        };

    /// <returns><see langword="false"/> when the email already belongs to another member.</returns>
    private static async Task<bool> TrySaveAsync(AppDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static IResult EmailInUse() =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Email in use",
            detail: "Another member already has this email address.");
}
