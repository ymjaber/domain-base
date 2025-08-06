using DomainBase;
using System;
using System.Collections.Generic;

namespace Samples;

// Example 1: Simple value object with auto properties
[ValueObject]
public partial class Address : ValueObject<Address>
{
    // Using constructor syntax for priority (cleaner for single parameter)
    [IncludeInEquality(10)]
    public string Street { get; init; }
    
    // Using property syntax (useful when you might add more properties later)
    [IncludeInEquality(Priority = 5)]
    public string City { get; init; }
    
    [IncludeInEquality(1)]
    public string PostalCode { get; init; }
    
    [IgnoreEquality]
    public DateTime CreatedAt { get; init; }
}

// Example 2: Value object with custom equality
[ValueObject]
public partial class Email : ValueObject<Email>
{
    // CustomEquality with high priority to check first
    [CustomEquality(100)]
    public string Value { get; init; }
    
    // Custom equality: case-insensitive comparison
    private partial void IsEqualValue(string? value, string? otherValue, ref bool result)
    {
        result = string.Equals(value, otherValue, StringComparison.OrdinalIgnoreCase);
    }
    
    private partial void GetHashCodeValue(string? value, ref System.HashCode hashCode)
    {
        hashCode.Add(value?.ToUpperInvariant());
    }
}

// Example 3: Value object with sequence equality
[ValueObject]
public partial class ShoppingCart : ValueObject<ShoppingCart>
{
    [IncludeInEquality(100)] // Check cart ID first
    public string CartId { get; init; }
    
    // Using property syntax when you need to set multiple properties
    [SequenceEquality(Priority = 50, OrderMatters = false)]
    public List<string> ItemIds { get; init; } = new();
    
    [SequenceEquality(Priority = 10, OrderMatters = true)]
    public List<decimal> PriceHistory { get; init; } = new();
}

// Example 4: Complex value object with mixed equality types
[ValueObject]
public partial class CustomerProfile : ValueObject<CustomerProfile>
{
    [IncludeInEquality(Priority = 100)]
    public string CustomerId { get; init; }
    
    [CustomEquality(Priority = 50)]
    public string Name { get; init; }
    
    [SequenceEquality(OrderMatters = false, Priority = 10)]
    public HashSet<string> Tags { get; init; } = new();
    
    [IncludeInEquality(Priority = 5)]
    public decimal CreditLimit { get; init; }
    
    [IgnoreEquality]
    public DateTime LastModified { get; init; }
    
    private partial void IsEqualName(string? name, string? otherName, ref bool result)
    {
        // Normalize whitespace and case for name comparison
        var normalized1 = name?.Trim().ToUpperInvariant();
        var normalized2 = otherName?.Trim().ToUpperInvariant();
        result = string.Equals(normalized1, normalized2);
    }
    
    private partial void GetHashCodeName(string? name, ref System.HashCode hashCode)
    {
        hashCode.Add(name?.Trim().ToUpperInvariant());
    }
}

// Usage example
public class Program
{
    public static void Main()
    {
        // Example 1: Simple equality
        var address1 = new Address 
        { 
            Street = "123 Main St", 
            City = "New York", 
            PostalCode = "10001",
            CreatedAt = DateTime.Now 
        };
        
        var address2 = new Address 
        { 
            Street = "123 Main St", 
            City = "New York", 
            PostalCode = "10001",
            CreatedAt = DateTime.Now.AddDays(1) // Different, but ignored
        };
        
        Console.WriteLine($"Addresses equal: {address1.Equals(address2)}"); // True
        
        // Example 2: Custom equality (case-insensitive)
        var email1 = new Email { Value = "John@Example.Com" };
        var email2 = new Email { Value = "john@example.com" };
        
        Console.WriteLine($"Emails equal: {email1.Equals(email2)}"); // True
        
        // Example 3: Sequence equality
        var cart1 = new ShoppingCart
        {
            CartId = "CART-123",
            ItemIds = new List<string> { "ITEM-1", "ITEM-2", "ITEM-3" },
            PriceHistory = new List<decimal> { 10.50m, 20.00m, 15.75m }
        };
        
        var cart2 = new ShoppingCart
        {
            CartId = "CART-123",
            ItemIds = new List<string> { "ITEM-3", "ITEM-1", "ITEM-2" }, // Different order, but OrderMatters = false
            PriceHistory = new List<decimal> { 10.50m, 20.00m, 15.75m } // Same order required
        };
        
        Console.WriteLine($"Carts equal: {cart1.Equals(cart2)}"); // True
        
        // Example 4: Priority-based equality (short-circuit evaluation)
        var profile1 = new CustomerProfile
        {
            CustomerId = "CUST-001",
            Name = "  John Doe  ",
            Tags = new HashSet<string> { "VIP", "Premium" },
            CreditLimit = 10000m,
            LastModified = DateTime.Now
        };
        
        var profile2 = new CustomerProfile
        {
            CustomerId = "CUST-002", // Different - will short-circuit here due to highest priority
            Name = "john doe", // Would match with custom equality
            Tags = new HashSet<string> { "Premium", "VIP" }, // Would match
            CreditLimit = 10000m,
            LastModified = DateTime.Now.AddDays(1)
        };
        
        Console.WriteLine($"Profiles equal: {profile1.Equals(profile2)}"); // False (different CustomerId)
    }
}