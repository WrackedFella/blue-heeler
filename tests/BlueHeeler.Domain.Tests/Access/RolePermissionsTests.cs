using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Access;

public sealed class RolePermissionsTests
{
    [Fact]
    public void For_Admin_GrantsViewMembers()
    {
        Assert.Contains(Permission.ViewMembers, RolePermissions.For(Role.Admin));
    }

    [Theory]
    [InlineData(Role.ShiftLead)]
    [InlineData(Role.Walker)]
    public void For_NonAdminRole_DoesNotGrantViewMembers(Role role)
    {
        Assert.DoesNotContain(Permission.ViewMembers, RolePermissions.For(role));
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
