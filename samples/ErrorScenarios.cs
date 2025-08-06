using DomainBase;
using System;

namespace Samples.ErrorScenarios;

// Scenario 1: Class with equality attributes but no [ValueObject] attribute
// Result: No error - attributes are simply ignored, no code generation happens
public class ClassWithoutValueObjectAttribute
{
    [IncludeInEquality]
    public string Name { get; set; }
    
    [CustomEquality]
    public int Age { get; set; }
}

// Scenario 2: Class with [ValueObject] but doesn't inherit from ValueObject<T>
// Result: No code generation - the generator requires BOTH the attribute AND inheritance
[ValueObject]
public partial class ClassWithoutInheritance
{
    [IncludeInEquality]
    public string Name { get; set; }
}

// Scenario 3: Correct usage - has both [ValueObject] and inherits from ValueObject<T>
[ValueObject]
public partial class CorrectValueObject : ValueObject<CorrectValueObject>
{
    [IncludeInEquality]
    public string Name { get; set; }
}

// Example showing how priority works with custom equality
[ValueObject]
public partial class PriorityExample : ValueObject<PriorityExample>
{
    // Priority 100 - checked first
    [CustomEquality(100)]
    public string Id { get; set; }
    
    // Priority 50 - checked second  
    [IncludeInEquality(50)]
    public string Category { get; set; }
    
    // Priority 10 - checked third
    [CustomEquality(10)]
    public string Name { get; set; }
    
    // Priority 0 (default) - checked last
    [IncludeInEquality]
    public int Value { get; set; }
    
    // Custom equality for Id - if this returns false, the comparison stops immediately
    private partial void IsEqualId(string? id, string? otherId, ref bool result)
    {
        Console.WriteLine($"Checking Id: {id} vs {otherId}");
        result = string.Equals(id, otherId, StringComparison.OrdinalIgnoreCase);
    }
    
    private partial void GetHashCodeId(string? id, ref System.HashCode hashCode)
    {
        hashCode.Add(id?.ToUpperInvariant());
    }
    
    // Custom equality for Name - only called if Id and Category are equal
    private partial void IsEqualName(string? name, string? otherName, ref bool result)
    {
        Console.WriteLine($"Checking Name: {name} vs {otherName}");
        result = string.Equals(name?.Trim(), otherName?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
    
    private partial void GetHashCodeName(string? name, ref System.HashCode hashCode)
    {
        hashCode.Add(name?.Trim().ToUpperInvariant());
    }
}

// Test program
public class ErrorScenariosTest
{
    public static void TestPriority()
    {
        var obj1 = new PriorityExample 
        { 
            Id = "ABC", 
            Category = "Test",
            Name = "  Example  ",
            Value = 100
        };
        
        var obj2 = new PriorityExample 
        { 
            Id = "XYZ",  // Different ID
            Category = "Test",
            Name = "Example",
            Value = 100
        };
        
        // This will only check Id (priority 100) and return false immediately
        Console.WriteLine($"Equal: {obj1.Equals(obj2)}"); // Output: Checking Id: ABC vs XYZ
                                                          // Output: Equal: False
        
        var obj3 = new PriorityExample 
        { 
            Id = "abc",  // Same ID (case insensitive)
            Category = "Different", // Different category
            Name = "Example",
            Value = 100
        };
        
        // This will check Id first (matches), then Category (doesn't match)
        Console.WriteLine($"Equal: {obj1.Equals(obj3)}"); // Output: Checking Id: ABC vs abc
                                                          // Output: Equal: False
    }
}