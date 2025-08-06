using DomainBase;
using System.Collections.Generic;

namespace TestNullableTypes;

[ValueObject]
public partial class TestValue : ValueObject
{
    // Non-nullable reference type - should NOT have ? added
    [CustomEquality]
    public string Name { get; init; }
    
    // Nullable reference type - should keep the ?
    [CustomEquality]
    public string? Description { get; init; }
    
    // Non-nullable value type - should NOT have ? added
    [CustomEquality]
    public int Count { get; init; }
    
    // Nullable value type - should keep the ?
    [CustomEquality]
    public int? OptionalCount { get; init; }
    
    // Expected generated methods:
    
    // private static void Equals_Name(in string name, in string otherName, out bool result)
    // {
    //     result = EqualityComparer<string>.Default.Equals(name, otherName);
    // }
    //
    // private static void Equals_Description(in string? description, in string? otherDescription, out bool result)
    // {
    //     result = EqualityComparer<string>.Default.Equals(description, otherDescription);
    // }
    //
    // private static void Equals_Count(in int count, in int otherCount, out bool result)
    // {
    //     result = EqualityComparer<int>.Default.Equals(count, otherCount);
    // }
    //
    // private static void Equals_OptionalCount(in int? optionalCount, in int? otherOptionalCount, out bool result)
    // {
    //     result = EqualityComparer<int>.Default.Equals(optionalCount, otherOptionalCount);
    // }
}