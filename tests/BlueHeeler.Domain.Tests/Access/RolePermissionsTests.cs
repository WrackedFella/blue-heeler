using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Access;

public sealed class RolePermissionsTests
{
    [Theory]
    [InlineData(Permission.ViewMembers)]
    [InlineData(Permission.ManageMembers)]
    public void For_Admin_Grants(Permission permission)
    {
        Assert.Contains(permission, RolePermissions.For(Role.Admin));
    }

    [Theory]
    [InlineData(Role.ShiftLead, Permission.ViewMembers)]
    [InlineData(Role.Walker, Permission.ViewMembers)]
    [InlineData(Role.ShiftLead, Permission.ManageMembers)]
    [InlineData(Role.Walker, Permission.ManageMembers)]
    public void For_NonAdminRole_DoesNotGrant(Role role, Permission permission)
    {
        Assert.DoesNotContain(permission, RolePermissions.For(role));
    }

    [Fact]
    public void For_EveryRole_IsDefined()
    {
        foreach (var role in Enum.GetValues<Role>())
        {
            var exception = Record.Exception(() => RolePermissions.For(role));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void For_UndefinedRole_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RolePermissions.For((Role)999));
    }
}
