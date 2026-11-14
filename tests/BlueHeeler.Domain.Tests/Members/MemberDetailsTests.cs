using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Members;

public sealed class MemberDetailsTests
{
    private static MemberDetailsResult Create(
        string? name = "Sam",
        string? email = "sam@example.org",
        Role? role = Role.Walker,
        int? tier = 1,
        MemberStatus? status = MemberStatus.Active) =>
        MemberDetails.Create(name, email, role, tier, status);

    private static IReadOnlyDictionary<string, string[]> ErrorsOf(MemberDetailsResult result) =>
        Assert.IsType<MemberDetailsResult.Invalid>(result).Errors;

    [Fact]
    public void Create_ValidInput_NormalisesAndKeepsValues()
    {
        var result = Create(name: "  Sam  ", email: " Sam@Example.org ", role: Role.ShiftLead, tier: 2, status: MemberStatus.Inactive);

        var details = Assert.IsType<MemberDetailsResult.Valid>(result).Details;
        Assert.Equal(
            new MemberDetails("Sam", EmailAddress.TryCreate("sam@example.org")!, Role.ShiftLead, WalkerTier.Tier2, MemberStatus.Inactive),
            details);
    }

    [Fact]
    public void Create_NoTier_IsValidWithNullTier()
    {
        var details = Assert.IsType<MemberDetailsResult.Valid>(Create(tier: null)).Details;

        Assert.Null(details.Tier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_ReportsName(string? name)
    {
        Assert.Equal(["name"], ErrorsOf(Create(name: name)).Keys);
    }

    [Fact]
    public void Create_NameOverMax_ReportsName()
    {
        Assert.Equal(["name"], ErrorsOf(Create(name: new string('a', Member.MaxNameLength + 1))).Keys);
    }

    [Fact]
    public void Create_NameOfMaxLengthAfterTrim_IsValid()
    {
        Assert.IsType<MemberDetailsResult.Valid>(Create(name: " " + new string('a', Member.MaxNameLength) + " "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Create_BadEmail_ReportsEmail(string? email)
    {
        Assert.Equal(["email"], ErrorsOf(Create(email: email)).Keys);
    }

    [Fact]
    public void Create_MissingRole_ReportsRole()
    {
        Assert.Equal(["role"], ErrorsOf(Create(role: null)).Keys);
    }

    [Fact]
    public void Create_MissingStatus_ReportsStatus()
    {
        Assert.Equal(["status"], ErrorsOf(Create(status: null)).Keys);
    }

    [Fact]
    public void Create_UndefinedRole_ReportsRole()
    {
        Assert.Equal(["role"], ErrorsOf(Create(role: (Role)42)).Keys);
    }

    [Fact]
    public void Create_UndefinedStatus_ReportsStatus()
    {
        Assert.Equal(["status"], ErrorsOf(Create(status: (MemberStatus)42)).Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Create_TierOutOfRange_ReportsTier(int tier)
    {
        Assert.Equal(["tier"], ErrorsOf(Create(tier: tier)).Keys);
    }

    [Theory]
    [InlineData(1, WalkerTier.Tier1)]
    [InlineData(3, WalkerTier.Tier3)]
    public void Create_TierAtBounds_MapsToWalkerTier(int tier, WalkerTier expected)
    {
        Assert.Equal(expected, Assert.IsType<MemberDetailsResult.Valid>(Create(tier: tier)).Details.Tier);
    }

    [Fact]
    public void Create_SeveralBadFields_ReportsEach()
    {
        var errors = ErrorsOf(Create(name: "", email: "x", role: null, tier: 9, status: null));

        Assert.Equal(["email", "name", "role", "status", "tier"], errors.Keys.Order());
        Assert.All(errors.Values, messages => Assert.NotEmpty(Assert.Single(messages)));
    }
}
