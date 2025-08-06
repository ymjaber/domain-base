using DomainBase;

namespace Samples;

[ValueObject]
public partial class TestProduct : ValueObject<TestProduct>
{
    [CustomEquality]
    public string Name { get; init; }
    
    // The code fix should generate:
    // private partial void IsEqualName(string? name, string? otherName, ref bool result);
    // private partial void GetHashCodeName(string? name, ref HashCode hashCode);
}