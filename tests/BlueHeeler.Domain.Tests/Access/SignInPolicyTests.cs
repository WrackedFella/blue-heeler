using BlueHeeler.Domain.Access;
using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Access;

public sealed class SignInPolicyTests
{
    private static readonly EmailAddress FirstAdmin = EmailAddress.TryCreate("admin@example.org")!;
    private static readonly EmailAddress Sam = EmailAddress.TryCreate("sam@example.org")!;
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);

    private static ExternalIdentity Verified(EmailAddress email, string name = "Someone") =>
        new(email, EmailVerified: true, name);

    private static Member Walker(EmailAddress email, MemberStatus status) =>
        new(Guid.NewGuid(), "Sam", email, Role.Walker, WalkerTier.Tier1, status, Now);

    [Fact]
    public void Decide_ActiveMember_SignsIn()
    {
        var sam = Walker(Sam, MemberStatus.Active);

        var decision = SignInPolicy.Decide(Verified(Sam), sam, FirstAdmin, adminExists: true, Now);

        var signIn = Assert.IsType<SignInDecision.SignIn>(decision);
        Assert.Same(sam, signIn.Member);
    }

    [Fact]
    public void Decide_InactiveMember_Refuses()
    {
        var sam = Walker(Sam, MemberStatus.Inactive);

        var decision = SignInPolicy.Decide(Verified(Sam), sam, FirstAdmin, adminExists: true, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.Inactive), decision);
    }

    [Fact]
    public void Decide_UnknownEmail_Refuses()
    {
        var decision = SignInPolicy.Decide(Verified(Sam), existing: null, FirstAdmin, adminExists: false, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.NotAMember), decision);
    }

    [Fact]
    public void Decide_UnverifiedEmail_Refuses()
    {
        var sam = Walker(Sam, MemberStatus.Active);
        var identity = new ExternalIdentity(Sam, EmailVerified: false, "Sam");

        var decision = SignInPolicy.Decide(identity, sam, FirstAdmin, adminExists: true, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.EmailNotVerified), decision);
    }

    [Fact]
    public void Decide_UnverifiedFirstAdminEmail_DoesNotBootstrap()
    {
        var identity = new ExternalIdentity(FirstAdmin, EmailVerified: false, "Justin");

        var decision = SignInPolicy.Decide(identity, existing: null, FirstAdmin, adminExists: false, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.EmailNotVerified), decision);
    }

    [Fact]
    public void Decide_FirstAdminEmail_NoAdminYet_BootstrapsActiveAdmin()
    {
        var decision = SignInPolicy.Decide(Verified(FirstAdmin, "Justin"), existing: null, FirstAdmin, adminExists: false, Now);

        var bootstrap = Assert.IsType<SignInDecision.BootstrapFirstAdmin>(decision);
        Assert.Equal(FirstAdmin, bootstrap.Member.Email);
        Assert.Equal("Justin", bootstrap.Member.Name);
        Assert.Equal(Role.Admin, bootstrap.Member.Role);
        Assert.Equal(MemberStatus.Active, bootstrap.Member.Status);
        Assert.Null(bootstrap.Member.Tier);
        Assert.Equal(Now, bootstrap.Member.CreatedAt);
        Assert.NotEqual(Guid.Empty, bootstrap.Member.Id);
    }

    [Fact]
    public void Decide_FirstAdminEmail_AdminExists_Refuses()
    {
        var decision = SignInPolicy.Decide(Verified(FirstAdmin), existing: null, FirstAdmin, adminExists: true, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.NotAMember), decision);
    }

    [Fact]
    public void Decide_NoFirstAdminConfigured_UnknownEmailRefuses()
    {
        var decision = SignInPolicy.Decide(Verified(FirstAdmin), existing: null, firstAdminEmail: null, adminExists: false, Now);

        Assert.Equal(new SignInDecision.Refuse(RefusalReason.NotAMember), decision);
    }

    [Fact]
    public void Decide_FirstAdminEmail_ExistingMember_SignsInAsThatMember()
    {
        var existing = Walker(FirstAdmin, MemberStatus.Active);

        var decision = SignInPolicy.Decide(Verified(FirstAdmin), existing, FirstAdmin, adminExists: false, Now);

        var signIn = Assert.IsType<SignInDecision.SignIn>(decision);
        Assert.Same(existing, signIn.Member);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Decide_BootstrapWithoutName_UsesEmail(string name)
    {
        var decision = SignInPolicy.Decide(Verified(FirstAdmin, name), existing: null, FirstAdmin, adminExists: false, Now);

        var bootstrap = Assert.IsType<SignInDecision.BootstrapFirstAdmin>(decision);
        Assert.Equal("admin@example.org", bootstrap.Member.Name);
    }

    [Fact]
    public void Decide_NullIdentity_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SignInPolicy.Decide(null!, existing: null, FirstAdmin, adminExists: false, Now));
    }
}
