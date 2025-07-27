using DomainBase;

namespace DomainBase.Tests;

public class DomainEventTests
{
    private record TestDomainEvent : DomainEvent
    {
        public TestDomainEvent(Guid id, DateTimeOffset occurredOn) : base(id, occurredOn) { }
    }
    
    private record SimpleTestDomainEvent : DomainEvent
    {
        public SimpleTestDomainEvent(Guid id) : base() 
        {
            // We need to set the Id after construction since the parameterless constructor generates a new Id
            // This is a test limitation - in real usage, you'd either use the parameterless constructor for new events
            // or the parameterized constructor for reconstruction
        }
    }

    [Fact]
    public void Constructor_WithIdAndOccurredOn_SetsProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = DateTimeOffset.Now;

        // Act
        var domainEvent = new TestDomainEvent(id, occurredOn);

        // Assert
        Assert.Equal(id, domainEvent.Id);
        Assert.Equal(occurredOn, domainEvent.OccurredOn);
    }

    [Fact]
    public void Constructor_Parameterless_SetsNewIdAndCurrentTime()
    {
        // Arrange
        var beforeTime = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new SimpleTestDomainEvent(Guid.NewGuid());

        // Assert
        Assert.NotEqual(Guid.Empty, domainEvent.Id);
        Assert.True(domainEvent.OccurredOn >= beforeTime);
        Assert.True(domainEvent.OccurredOn <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Records_WithSameValues_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = DateTimeOffset.Now;

        // Act
        var event1 = new TestDomainEvent(id, occurredOn);
        var event2 = new TestDomainEvent(id, occurredOn);

        // Assert
        Assert.Equal(event1, event2);
        Assert.True(event1 == event2);
        Assert.Equal(event1.GetHashCode(), event2.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentIds_AreNotEqual()
    {
        // Arrange
        var occurredOn = DateTimeOffset.Now;

        // Act
        var event1 = new TestDomainEvent(Guid.NewGuid(), occurredOn);
        var event2 = new TestDomainEvent(Guid.NewGuid(), occurredOn);

        // Assert
        Assert.NotEqual(event1, event2);
        Assert.True(event1 != event2);
    }

    [Fact]
    public void Records_WithDifferentOccurredOn_AreNotEqual()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var event1 = new TestDomainEvent(id, DateTimeOffset.Now);
        var event2 = new TestDomainEvent(id, DateTimeOffset.Now.AddSeconds(1));

        // Assert
        Assert.NotEqual(event1, event2);
    }

    [Fact]
    public void CanBeUsedInCollections()
    {
        // Arrange
        var events = new List<DomainEvent>();
        var event1 = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.Now);
        var event2 = new SimpleTestDomainEvent(Guid.NewGuid());

        // Act
        events.Add(event1);
        events.Add(event2);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Contains(event1, events);
        Assert.Contains(event2, events);
    }

    [Fact]
    public void RecordToString_ContainsTypeAndProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var occurredOn = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var domainEvent = new TestDomainEvent(id, occurredOn);

        // Act
        var result = domainEvent.ToString();

        // Assert
        Assert.Contains("TestDomainEvent", result);
        Assert.Contains("Id", result);
        Assert.Contains("OccurredOn", result);
    }
    
    [Fact]
    public void GetEventName_ReturnsTypeName()
    {
        // Arrange
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Act
        var eventName = domainEvent.GetEventName();

        // Assert
        Assert.Equal("TestDomainEvent", eventName);
    }
}