using Xunit;
using System.Threading.Tasks;

namespace DomainBase.Generators.Tests;

public class GlobalNamespaceTests
{
    [Fact]
    public Task EnumerationInGlobalNamespace_GeneratesCorrectCode()
    {
        var source = @"
using DomainBase;

public partial class GlobalStatus : Enumeration
{
    public static readonly GlobalStatus Active = new(1, ""Active"");
    public static readonly GlobalStatus Inactive = new(2, ""Inactive"");

    private GlobalStatus(int value, string name) : base(value, name) { }
}";

        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ValueObjectInGlobalNamespace_GeneratesCorrectCode()
    {
        var source = @"
using DomainBase;

[ValueObject]
public partial class GlobalValue : ValueObject<GlobalValue>
{
    public string Name { get; }
    public int Count { get; }

    public GlobalValue(string name, int count)
    {
        Name = name;
        Count = count;
    }
}";

        return TestHelper.Verify(source);
    }
}