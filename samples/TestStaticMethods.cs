using DomainBase;
using System.Collections.Generic;

namespace TestStaticMethods;

[ValueObject]
public partial class Product : ValueObject
{
    [CustomEquality]
    public string Name { get; init; }
    
    [CustomEquality]
    public decimal _price { get; init; }
    
    [IncludeInEquality]
    public int Stock { get; init; }
    
    // The code fix should generate these static methods:
    
    // private static void Equals_Name(in string? name, in string? otherName, ref bool result)
    // {
    //     result = EqualityComparer<string>.Default.Equals(name, otherName);
    // }
    //
    // private static void GetHashCode_Name(in string? name, ref HashCode hashCode)
    // {
    //     hashCode.Add(name);
    // }
    //
    // private static void Equals_Price(in decimal price, in decimal otherPrice, ref bool result)
    // {
    //     result = EqualityComparer<decimal>.Default.Equals(price, otherPrice);
    // }
    //
    // private static void GetHashCode_Price(in decimal price, ref HashCode hashCode)
    // {
    //     hashCode.Add(price);
    // }
}