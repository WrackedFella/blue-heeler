using BlueHeeler.Domain.Access;
using Microsoft.AspNetCore.Authorization;

namespace BlueHeeler.Api.Auth;

/// <summary>One authorization policy per <see cref="Permission"/>, so endpoints name the action they guard.</summary>
internal static class Permissions
{
    public static string PolicyName(Permission permission) => $"permission:{permission}";

    public static void AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Enum.GetValues<Permission>())
        {
            options.AddPolicy(PolicyName(permission), policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AppClaims.Permission, permission.ToString()));
        }
    }

    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, Permission permission)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(PolicyName(permission));
}
