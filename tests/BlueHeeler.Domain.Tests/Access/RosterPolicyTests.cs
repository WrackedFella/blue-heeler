using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Access;

public sealed class RosterPolicyTests
{
    private static readonly EmailAddress Email = EmailAddress.TryCreate("justin@example.org")!;

    private static Member Admin(MemberStatus status = MemberStatus.Active) =>
        new(Guid.NewGuid(), "Justin", Email, Role.Admin, null, status, DateTimeOffset.UnixEpoch);

    private static MemberDetails As(Role role, MemberStatus status) => new("Justin", Email, role, null, status);

    [Theory]
    [InlineData(Role.Walker, MemberStatus.Active)]
    [InlineData(Role.ShiftLead, MemberStatus.Active)]
    [InlineData(Role.Admin, MemberStatus.Inactive)]
    public void CheckEdit_LastActiveAdminLosesAdmin_Refuses(Role role, MemberStatus status)
    {
        Assert.Equal(EditRefusal.LastActiveAdmin, RosterPolicy.CheckEdit(Admin(), As(role, status), activeAdminCount: 1));
    }

    [Fact]
    public void CheckEdit_DemoteAdmin_OtherActiveAdminExists_Allows()
    {
        Assert.Null(RosterPolicy.CheckEdit(Admin(), As(Role.Walker, MemberStatus.Active), activeAdminCount: 2));
    }

    [Fact]
    public void CheckEdit_LastActiveAdminStaysActiveAdmin_Allows()
    {
        Assert.Null(RosterPolicy.CheckEdit(Admin(), As(Role.Admin, MemberStatus.Active), activeAdminCount: 1));
    }

    [Fact]
    public void CheckEdit_InactiveAdminChanged_Allows()
    {
        Assert.Null(RosterPolicy.CheckEdit(Admin(MemberStatus.Inactive), As(Role.Walker, MemberStatus.Inactive), activeAdminCount: 1));
    }

    [Fact]
    public void CheckEdit_NonAdminChanged_Allows()
    {
        var walker = new Member(Guid.NewGuid(), "Sam", Email, Role.Walker, WalkerTier.Tier1, MemberStatus.Active, DateTimeOffset.UnixEpoch);

        Assert.Null(RosterPolicy.CheckEdit(walker, As(Role.Walker, MemberStatus.Inactive), activeAdminCount: 1));
    }

    [Fact]
    public void CheckEdit_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => RosterPolicy.CheckEdit(null!, As(Role.Admin, MemberStatus.Active), 1));
        Assert.Throws<ArgumentNullException>(() => RosterPolicy.CheckEdit(Admin(), null!, 1));
    }
}
