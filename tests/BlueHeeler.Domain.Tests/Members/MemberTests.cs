using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Members;

public sealed class MemberTests
{
    private static readonly EmailAddress Email = EmailAddress.TryCreate("sam@example.org")!;

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Member(Guid.NewGuid(), name, Email, Role.Walker, WalkerTier.Tier1, MemberStatus.Active, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Member(Guid.Empty, "Sam", Email, Role.Walker, WalkerTier.Tier1, MemberStatus.Active, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Constructor_Name_IsTrimmed()
    {
        var member = new Member(Guid.NewGuid(), "  Sam  ", Email, Role.Walker, null, MemberStatus.Active, DateTimeOffset.UnixEpoch);

        Assert.Equal("Sam", member.Name);
    }

    [Fact]
    public void Constructor_NullEmail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Member(Guid.NewGuid(), "Sam", null!, Role.Walker, null, MemberStatus.Active, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Constructor_NameLongerThanMax_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Member(Guid.NewGuid(), new string('a', Member.MaxNameLength + 1), Email, Role.Walker, null, MemberStatus.Active, DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void Constructor_NameOfMaxLength_Succeeds()
    {
        var member = new Member(Guid.NewGuid(), new string('a', Member.MaxNameLength), Email, Role.Walker, null, MemberStatus.Active, DateTimeOffset.UnixEpoch);

        Assert.Equal(Member.MaxNameLength, member.Name.Length);
    }
}
