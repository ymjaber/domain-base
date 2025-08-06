using DomainBase;
using System.Collections.Generic;

namespace TestCodeFixDebug;

[ValueObject]  
public partial class Person : ValueObject
{
    [CustomEquality]
    public string FirstName { get; init; }
    
    // When the code fix runs, it should generate these methods in THIS file:
    
    // private partial void IsEqualFirstName(string? firstName, string? otherFirstName, ref bool result)
    // {
    //     // Generated for property: FirstName
    //     result = EqualityComparer<string>.Default.Equals(firstName, otherFirstName);
    // }
    
    // private partial void GetHashCodeFirstName(string? firstName, ref HashCode hashCode)
    // {
    //     hashCode.Add(firstName);
    // }
}