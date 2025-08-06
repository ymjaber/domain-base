using DomainBase;

namespace DebugCodeFix;

[ValueObject]
public partial class TestValue : ValueObject
{
    [CustomEquality]
    public string Name { get; init; }
    
    // Expected to generate:
    // private partial void IsEqualName(string? name, string? otherName, ref bool result)
    // {
    //     result = EqualityComparer<string>.Default.Equals(name, otherName);
    // }
    //
    // private partial void GetHashCodeName(string? name, ref HashCode hashCode)
    // {
    //     hashCode.Add(name);
    // }
}