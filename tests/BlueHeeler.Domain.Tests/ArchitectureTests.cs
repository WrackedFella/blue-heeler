using BlueHeeler.Domain.Members;

namespace BlueHeeler.Domain.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainAssembly_ReferencesOnlyTheBcl()
    {
        var references = typeof(Member).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        Assert.All(references, name => Assert.StartsWith("System.", name, StringComparison.Ordinal));
    }
}
