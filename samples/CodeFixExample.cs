using DomainBase;
using System;

namespace Samples;

// Example to test the code fix
[ValueObject]
public partial class Product : ValueObject<Product>
{
    [CustomEquality(100)]
    private readonly string _name;
    
    [CustomEquality(50)]
    public decimal Price { get; init; }
    
    public Product(string name, decimal price)
    {
        _name = name;
        Price = price;
    }
    
    // The code fix should generate these methods with correct names and signatures:
    private partial void IsEqualName(string? name, string? otherName, ref bool result)
    {
        result = string.Equals(name, otherName, StringComparison.OrdinalIgnoreCase);
    }
    
    private partial void GetHashCodeName(string? name, ref System.HashCode hashCode)
    {
        hashCode.Add(name?.ToUpperInvariant());
    }
    
    private partial void IsEqualPrice(decimal price, decimal otherPrice, ref bool result)
    {
        result = price == otherPrice;
    }
    
    private partial void GetHashCodePrice(decimal price, ref System.HashCode hashCode)
    {
        hashCode.Add(price);
    }
}