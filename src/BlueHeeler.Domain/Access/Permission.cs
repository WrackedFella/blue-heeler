using System.Diagnostics.CodeAnalysis;

namespace BlueHeeler.Domain.Access;

/// <summary>
/// An action the app guards. Endpoints require a permission, never a role, so a new role
/// is a row in <see cref="RolePermissions"/> rather than a change to every endpoint.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Permission is the domain term, not the System.Security permission suffix.")]
public enum Permission
{
    /// <summary>See the member list and member details.</summary>
    ViewMembers,
}
