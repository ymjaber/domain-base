using DomainBase;
using System;

namespace TestProgram;

[ValueObject]
public partial class TestValue : ValueObject
{
    [CustomEquality]
    public string Name { get; init; } = "";
    
    [IncludeInEquality]
    public int Age { get; init; }
}

class Program
{
    static void Main()
    {
        var test1 = new TestValue { Name = "Test", Age = 25 };
        var test2 = new TestValue { Name = "Test", Age = 25 };
        
        Console.WriteLine($"test1 == test2: {test1 == test2}");
    }
}