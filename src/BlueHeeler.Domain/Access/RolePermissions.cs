using System.Collections.Frozen;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Access;

/// <summary>The permissions each role grants.</summary>
public static class RolePermissions
{
    private static readonly FrozenDictionary<Role, FrozenSet<Permission>> Table = new Dictionary<Role, FrozenSet<Permission>>
    {
        [Role.Admin] = FrozenSet.ToFrozenSet([Permission.ViewMembers]),
        [Role.ShiftLead] = FrozenSet<Permission>.Empty,
        [Role.Walker] = FrozenSet<Permission>.Empty,
    }.ToFrozenDictionary();

    /// <summary>The permissions <paramref name="role"/> grants.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="role"/> has no entry, which means the table is missing a new role.</exception>
    public static IReadOnlySet<Permission> For(Role role) =>
        Table.TryGetValue(role, out var permissions)
            ? permissions
            : throw new ArgumentOutOfRangeException(nameof(role), role, "Every role needs a row in RolePermissions.");
}
