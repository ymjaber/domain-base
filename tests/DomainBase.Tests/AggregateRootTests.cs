using DomainBase;

namespace DomainBase.Tests;

public class AggregateRootTests
{
    private class TestAggregateRoot : AggregateRoot<int>
    {
        public TestAggregateRoot(int id) : base(id) { }

        public void DoSomething()
        {
            AddDomainEvent(new TestDomainEvent(Guid.NewGuid()));
        }

        public void DoMultipleThings()
        {
            AddDomainEvent(new TestDomainEvent(Guid.NewGuid()));
            AddDomainEvent(new AnotherTestDomainEvent(Guid.NewGuid()));
        }
    }

    private record TestDomainEvent : DomainEvent
    {
        public TestDomainEvent(Guid id) : base(id, DateTimeOffset.UtcNow) { }
    }
    
    private record AnotherTestDomainEvent : DomainEvent
    {
        public AnotherTestDomainEvent(Guid id) : base(id, DateTimeOffset.UtcNow) { }
    }

    [Fact]
    public void Constructor_SetsId()
    {
        // Arrange & Act
        var aggregate = new TestAggregateRoot(123);

        // Assert
        Assert.Equal(123, aggregate.Id);
    }

    [Fact]
    public void DomainEvents_InitiallyEmpty()
    {
        // Arrange & Act
        var aggregate = new TestAggregateRoot(123);

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_AddsEventToCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(123);

        // Act
        aggregate.DoSomething();

        // Assert
        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<TestDomainEvent>(aggregate.DomainEvents.First());
    }

    [Fact]
    public void AddDomainEvent_CanAddMultipleEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(123);

        // Act
        aggregate.DoMultipleThings();

        // Assert
        Assert.Equal(2, aggregate.DomainEvents.Count);
        Assert.Contains(aggregate.DomainEvents, e => e is TestDomainEvent);
        Assert.Contains(aggregate.DomainEvents, e => e is AnotherTestDomainEvent);
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(123);
        aggregate.DoMultipleThings();
        Assert.Equal(2, aggregate.DomainEvents.Count);

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_ReturnsReadOnlyCollection()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(123);

        // Act
        var events = aggregate.DomainEvents;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyCollection<DomainEvent>>(events);
    }

    [Fact]
    public void DomainEvents_PreservesOrderOfAddition()
    {
        // Arrange
        var aggregate = new TestAggregateRoot(123);

        // Act
        aggregate.DoSomething(); // First event
        aggregate.DoSomething(); // Second event
        
        // Assert
        var eventsList = aggregate.DomainEvents.ToList();
        Assert.Equal(2, eventsList.Count);
        Assert.All(eventsList, e => Assert.IsType<TestDomainEvent>(e));
    }

    [Fact]
    public void InheritsFromEntity_HasEntityBehavior()
    {
        // Arrange
        var aggregate1 = new TestAggregateRoot(123);
        var aggregate2 = new TestAggregateRoot(123);

        // Act & Assert
        Assert.True(aggregate1 == aggregate2);
        Assert.Equal(aggregate1.GetHashCode(), aggregate2.GetHashCode());
        Assert.Equal("TestAggregateRoot: 123", aggregate1.ToString());
    }
}