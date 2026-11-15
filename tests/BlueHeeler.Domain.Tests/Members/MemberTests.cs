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

public sealed class MemberChangeTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 9, 0, 0, TimeSpan.Zero);

    private static MemberDetails Details(string name, Role role, WalkerTier? tier, MemberStatus status) =>
        new(name, EmailAddress.TryCreate(name.ToLowerInvariant() + "@example.org")!, role, tier, status);

    private static MemberDetails DetailsOf(Member m) => new(m.Name, m.Email, m.Role, m.Tier, m.Status);

    [Fact]
    public void Add_FromDetails_CopiesEveryField()
    {
        var id = Guid.NewGuid();

        var member = Member.Add(id, Details("Sam", Role.Walker, WalkerTier.Tier1, MemberStatus.Active), Now);

        Assert.Equal(id, member.Id);
        Assert.Equal(Now, member.CreatedAt);
        Assert.Equal(Details("Sam", Role.Walker, WalkerTier.Tier1, MemberStatus.Active), DetailsOf(member));
    }

    [Fact]
    public void Apply_NewDetails_ReplacesEditableFieldsOnly()
    {
        var member = Member.Add(Guid.NewGuid(), Details("Sam", Role.Walker, WalkerTier.Tier1, MemberStatus.Active), Now);
        var (id, created) = (member.Id, member.CreatedAt);
        var changed = Details("Alex", Role.ShiftLead, WalkerTier.Tier2, MemberStatus.Inactive);

        member.Apply(changed);

        Assert.Equal(changed, DetailsOf(member));
        Assert.Equal((id, created), (member.Id, member.CreatedAt));
    }

    [Fact]
    public void Apply_Null_Throws()
    {
        var member = Member.Add(Guid.NewGuid(), Details("Sam", Role.Walker, null, MemberStatus.Active), Now);

        Assert.Throws<ArgumentNullException>(() => member.Apply(null!));
    }
}

public sealed class MemberApplyInvariantTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_DetailsWithBlankName_Throws(string name)
    {
        var email = EmailAddress.TryCreate("sam@example.org")!;
        var member = new Member(Guid.NewGuid(), "Sam", email, Role.Walker, null, MemberStatus.Active, DateTimeOffset.UnixEpoch);

        Assert.Throws<ArgumentException>(() => member.Apply(new MemberDetails(name, email, Role.Walker, null, MemberStatus.Active)));
        Assert.Equal("Sam", member.Name);
    }
}
