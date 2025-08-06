using DomainBase;
using System.Collections.Generic;

namespace TestCodeFix;

[ValueObject]
public partial class Product : ValueObject
{
    [CustomEquality]
    public string _name { get; init; }
    
    [IncludeInEquality]
    public decimal Price { get; init; }
    
    // After applying code fix, these methods should be generated:
    // private partial void IsEqualName(string? _name, string? otherName, ref bool result)
    // {
    //     result = EqualityComparer<string>.Default.Equals(_name, otherName);
    // }
    //
    // private partial void GetHashCodeName(string? _name, ref HashCode hashCode)
    // {
    //     hashCode.Add(_name);
    // }
}