using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests.Members;

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("admin@example.org", "admin@example.org")]
    [InlineData("  Admin@Example.ORG ", "admin@example.org")]
    public void TryCreate_ValidInput_IsTrimmedAndLowerCased(string input, string expected)
    {
        var email = EmailAddress.TryCreate(input);

        Assert.NotNull(email);
        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign.example.org")]
    [InlineData("@example.org")]
    [InlineData("admin@")]
    [InlineData("a@b@example.org")]
    [InlineData("ad min@example.org")]
    public void TryCreate_InvalidInput_ReturnsNull(string? input)
    {
        Assert.Null(EmailAddress.TryCreate(input));
    }

    [Fact]
    public void TryCreate_LongerThan254Characters_ReturnsNull()
    {
        var input = new string('a', 243) + "@example.org";

        Assert.Equal(255, input.Length);
        Assert.Null(EmailAddress.TryCreate(input));
    }

    [Fact]
    public void TryCreate_Exactly254Characters_Succeeds()
    {
        var input = new string('a', 242) + "@example.org";

        Assert.NotNull(EmailAddress.TryCreate(input));
    }

    [Fact]
    public void Equality_DifferentCaseOfSameAddress_AreEqual()
    {
        Assert.Equal(EmailAddress.TryCreate("Sam@Example.org"), EmailAddress.TryCreate("sam@example.org"));
    }
}
