using DomainBase;

namespace DomainBase.Tests;

public class GeneratedValueObjectTests
{
    [ValueObject]
    public partial class SimpleValueObject : ValueObject<SimpleValueObject>
    {
        [IncludeInEquality]
        public string Name { get; init; } = "";
        
        [IncludeInEquality]
        public int Age { get; init; }
        
        [IgnoreEquality]
        public DateTime LastModified { get; set; }
        
        // Temporary manual implementation until generator runs
        protected override bool EqualsCore(SimpleValueObject other)
        {
            return Name == other.Name && Age == other.Age;
        }
        
        protected override int GetHashCodeCore()
        {
            return HashCode.Combine(Name, Age);
        }
    }

    [ValueObject]
    public partial class PriorityValueObject : ValueObject<PriorityValueObject>
    {
        [IncludeInEquality(Priority = 10)]
        public string Id { get; init; } = "";
        
        [IncludeInEquality(Priority = 5)]
        public string Name { get; init; } = "";
        
        [IncludeInEquality(Priority = 1)]
        public string Description { get; init; } = "";
        
        // Temporary manual implementation
        protected override bool EqualsCore(PriorityValueObject other)
        {
            return Id == other.Id && Name == other.Name && Description == other.Description;
        }
        
        protected override int GetHashCodeCore()
        {
            return HashCode.Combine(Id, Name, Description);
        }
    }

    [ValueObject]
    public partial class CustomEqualityValueObject : ValueObject<CustomEqualityValueObject>
    {
        [IncludeInEquality]
        public string FirstName { get; init; } = "";
        
        [CustomEquality]
        public string LastName { get; init; } = "";
        
        private partial void IsEqualLastName(string? lastName, string? otherLastName, ref bool result);
        private partial void GetHashCodeLastName(string? lastName, ref HashCode hashCode);
        
        private partial void IsEqualLastName(string? lastName, string? otherLastName, ref bool result)
        {
            result = string.Equals(lastName, otherLastName, StringComparison.OrdinalIgnoreCase);
        }
        
        private partial void GetHashCodeLastName(string? lastName, ref HashCode hashCode)
        {
            hashCode.Add(lastName?.ToUpperInvariant());
        }
        
        // Temporary manual implementation
        protected override bool EqualsCore(CustomEqualityValueObject other)
        {
            if (FirstName != other.FirstName) return false;
            bool customResult = true;
            IsEqualLastName(LastName, other.LastName, ref customResult);
            return customResult;
        }
        
        protected override int GetHashCodeCore()
        {
            var hashCode = new HashCode();
            hashCode.Add(FirstName);
            GetHashCodeLastName(LastName, ref hashCode);
            return hashCode.ToHashCode();
        }
    }

    [ValueObject]
    public partial class SequenceValueObject : ValueObject<SequenceValueObject>
    {
        [IncludeInEquality]
        public string Name { get; init; } = "";
        
        [SequenceEquality(OrderMatters = true)]
        public List<string> OrderedTags { get; init; } = new();
        
        [SequenceEquality(OrderMatters = false)]
        public IReadOnlyList<int> UnorderedNumbers { get; init; } = new List<int>();
        
        // Temporary manual implementation
        protected override bool EqualsCore(SequenceValueObject other)
        {
            if (Name != other.Name) return false;
            if (!SequenceEquals(OrderedTags, other.OrderedTags, true)) return false;
            if (!SequenceEquals(UnorderedNumbers, other.UnorderedNumbers, false)) return false;
            return true;
        }
        
        protected override int GetHashCodeCore()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);
            if (OrderedTags != null)
            {
                foreach (var item in OrderedTags)
                    hashCode.Add(item);
            }
            if (UnorderedNumbers != null)
            {
                foreach (var item in UnorderedNumbers)
                    hashCode.Add(item);
            }
            return hashCode.ToHashCode();
        }
        
        private static bool SequenceEquals<T>(IEnumerable<T>? first, IEnumerable<T>? second, bool orderMatters)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first is null || second is null) return false;
            
            return orderMatters
                ? first.SequenceEqual(second)
                : first.OrderBy(x => x).SequenceEqual(second.OrderBy(x => x));
        }
    }

    [Fact]
    public void SimpleValueObject_Equality_WorksCorrectly()
    {
        // Arrange
        var vo1 = new SimpleValueObject { Name = "John", Age = 30, LastModified = DateTime.Now };
        var vo2 = new SimpleValueObject { Name = "John", Age = 30, LastModified = DateTime.Now.AddHours(1) };
        var vo3 = new SimpleValueObject { Name = "Jane", Age = 30, LastModified = DateTime.Now };

        // Act & Assert
        Assert.True(vo1.Equals(vo2)); // LastModified is ignored
        Assert.False(vo1.Equals(vo3)); // Different name
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode());
        Assert.NotEqual(vo1.GetHashCode(), vo3.GetHashCode());
    }

    [Fact]
    public void CustomEquality_PerformsCaseInsensitiveComparison()
    {
        // Arrange
        var vo1 = new CustomEqualityValueObject { FirstName = "John", LastName = "Doe" };
        var vo2 = new CustomEqualityValueObject { FirstName = "John", LastName = "DOE" };
        var vo3 = new CustomEqualityValueObject { FirstName = "John", LastName = "Smith" };

        // Act & Assert
        Assert.True(vo1.Equals(vo2)); // Case insensitive last name
        Assert.False(vo1.Equals(vo3)); // Different last name
        Assert.Equal(vo1.GetHashCode(), vo2.GetHashCode()); // Same hash for case variations
    }

    [Fact]
    public void SequenceEquality_OrderedComparison_WorksCorrectly()
    {
        // Arrange
        var vo1 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = new List<string> { "A", "B", "C" },
            UnorderedNumbers = new List<int> { 1, 2, 3 }
        };
        
        var vo2 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = new List<string> { "A", "B", "C" },
            UnorderedNumbers = new List<int> { 3, 1, 2 } // Different order
        };
        
        var vo3 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = new List<string> { "C", "B", "A" }, // Different order
            UnorderedNumbers = new List<int> { 1, 2, 3 }
        };

        // Act & Assert
        Assert.True(vo1.Equals(vo2)); // Unordered numbers match despite different order
        Assert.False(vo1.Equals(vo3)); // Ordered tags don't match due to different order
    }

    [Fact]
    public void SequenceEquality_NullHandling_WorksCorrectly()
    {
        // Arrange
        var vo1 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = null!,
            UnorderedNumbers = null!
        };
        
        var vo2 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = null!,
            UnorderedNumbers = null!
        };
        
        var vo3 = new SequenceValueObject 
        { 
            Name = "Test",
            OrderedTags = new List<string>(),
            UnorderedNumbers = new List<int>()
        };

        // Act & Assert
        Assert.True(vo1.Equals(vo2)); // Both null sequences
        Assert.False(vo1.Equals(vo3)); // Null vs empty list
    }

    [Fact]
    public void ValueObject_OperatorOverloads_WorkCorrectly()
    {
        // Arrange
        var vo1 = new SimpleValueObject { Name = "John", Age = 30 };
        var vo2 = new SimpleValueObject { Name = "John", Age = 30 };
        var vo3 = new SimpleValueObject { Name = "Jane", Age = 30 };

        // Act & Assert
        Assert.True(vo1 == vo2);
        Assert.False(vo1 == vo3);
        Assert.False(vo1 != vo2);
        Assert.True(vo1 != vo3);
    }
}