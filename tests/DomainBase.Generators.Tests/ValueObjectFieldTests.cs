namespace DomainBase.Generators.Tests;

public class ValueObjectFieldTests
{
    [Fact]
    public Task GeneratesExpectedCode_ForFieldsWithNamingConventions()
    {
        var source = """
            using DomainBase;

            namespace TestNamespace;

            [ValueObject]
            public partial class FieldExample : ValueObject<FieldExample>
            {
                [CustomEquality(100)]
                private readonly string _name;
                
                [CustomEquality(90)]
                private readonly int m_age;
                
                [IncludeInEquality(80)]
                private readonly string _firstName;
                
                [IncludeInEquality(70)]
                public string LastName { get; init; }
                
                public FieldExample(string name, int age, string firstName, string lastName)
                {
                    _name = name;
                    m_age = age;
                    _firstName = firstName;
                    LastName = lastName;
                }
                
                private partial void IsEqualName(string? name, string? otherName, ref bool result)
                {
                    result = string.Equals(name, otherName, StringComparison.OrdinalIgnoreCase);
                }
                
                private partial void GetHashCodeName(string? name, ref System.HashCode hashCode)
                {
                    hashCode.Add(name?.ToUpperInvariant());
                }
                
                private partial void IsEqualAge(int age, int otherAge, ref bool result)
                {
                    result = age == otherAge;
                }
                
                private partial void GetHashCodeAge(int age, ref System.HashCode hashCode)
                {
                    hashCode.Add(age);
                }
            }
            """;

        return TestHelper.Verify(source);
    }
}