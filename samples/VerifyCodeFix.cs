using DomainBase;

namespace VerifyCodeFix;

[ValueObject]
public partial class Person : ValueObject
{
    [CustomEquality]
    public string Name { get; init; }
    
    [CustomEquality]
    public int _age { get; init; }
    
    [CustomEquality]
    public string m_address { get; init; }
}

// Expected generated methods:
// private partial void IsEqualName(string? name, string? otherName, ref bool result)
// private partial void GetHashCodeName(string? name, ref HashCode hashCode)
// private partial void IsEqualAge(int age, int otherAge, ref bool result)
// private partial void GetHashCodeAge(int age, ref HashCode hashCode)
// private partial void IsEqualAddress(string? m_address, string? otherAddress, ref bool result)
// private partial void GetHashCodeAddress(string? m_address, ref HashCode hashCode)