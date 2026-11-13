namespace BlueHeeler.Domain.Members;

/// <summary>A named set of permissions a member holds; see <see cref="Access.RolePermissions"/>.</summary>
public enum Role
{
    /// <summary>Volunteer coordinator: manages members and sees sensitive records.</summary>
    Admin,

    /// <summary>Leads shifts.</summary>
    ShiftLead,

    /// <summary>Signs up for shifts.</summary>
    Walker,
}
