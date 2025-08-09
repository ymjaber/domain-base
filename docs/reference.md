# API reference

## Table of contents

- [Entities](#entities)
- [Aggregate roots](#aggregate-roots)
- [Auditable entities](#auditable-entities)
- [Value objects](#value-objects)
- [Equality attributes (VO)](#equality-attributes-vo)
- [Enumerations](#enumerations)
- [Generation attributes](#generation-attributes)
- [Generated APIs (Enumeration)](#generated-apis-enumeration)
- [Generated APIs (ValueObject)](#generated-apis-valueobject)
- [Domain events](#domain-events)
- [Event handlers and dispatcher](#event-handlers-and-dispatcher)
- [Specifications](#specifications)
- [Repositories (optional)](#repositories-optional)
- [Exceptions](#exceptions)
- [Services](#services)

Namespaces: all public types live under `DomainBase`.

## Entities

- `abstract class Entity<TId> where TId : struct`
  - ctor(TId id)
  - `TId Id { get; }`
  - Equality: `==`, `!=`, overrides `Equals(object)`, `GetHashCode()`, `ToString()`

## Aggregate roots

- `abstract class AggregateRoot<TId> : Entity<TId> where TId : struct`
  - `IReadOnlyCollection<DomainEvent> DomainEvents { get; }`
  - `void ClearDomainEvents()`
  - `protected void AddDomainEvent(DomainEvent domainEvent)`

## Auditable entities

- `interface IAuditableEntity`
  - `DateTimeOffset CreatedAt { get; set; }`
  - `DateTimeOffset? UpdatedAt { get; set; }`
- `interface IAuditableEntity<TUserId> : IAuditableEntity`
  - `TUserId? CreatedBy { get; set; }`
  - `TUserId? UpdatedBy { get; set; }`
- `abstract class AuditableEntity<TId> : Entity<TId>`
  - `void MarkAsUpdated()`
- `abstract class AuditableEntity<TId, TUserId> : Entity<TId>`
  - `void MarkAsUpdated()` / `void MarkAsUpdated(TUserId userId)`
- `abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>`
  - `void MarkAsUpdated()`
- `abstract class AuditableAggregateRoot<TId, TUserId> : AggregateRoot<TId>`
  - `void MarkAsUpdated()` / `void MarkAsUpdated(TUserId userId)`

## Value objects

- `abstract class ValueObject<TSelf> : IEquatable<TSelf>`
  - Equality operators, `Equals`, `GetHashCode()`
  - `protected abstract bool EqualsCore(TSelf other)`
  - `protected abstract int GetHashCodeCore()`

- `abstract class ValueObject<TSelf, TValue> : ValueObject<TSelf>`
  - ctor(TValue value)
  - `TValue Value { get; }`
  - implicit conversion to `TValue`

- `sealed class ValueObjectAttribute : Attribute`

## Equality attributes (VO)

- `IncludeInEqualityAttribute(int Priority = 0)`
- `IgnoreEqualityAttribute`
- `SequenceEqualityAttribute(bool OrderMatters = true, bool DeepEquality = true, int Priority = 0)`
- `CustomEqualityAttribute(int Priority = 0)`
  - Requires static members on the VO class for each annotated member `<Name>`:
    - `private static void Equals_<Name>(in T value, in T otherValue, out bool result)`
    - `private static void GetHashCode_<Name>(in T value, ref HashCode hashCode)`

## Enumerations

- `abstract class Enumeration : IComparable`
  - `string Name { get; }`
  - `int Value { get; }`
  - Comparers and operators: `<, <=, >, >=, ==, !=`

## Generation attributes

- `GenerateJsonConverterAttribute { UnknownValueBehavior Behavior = ReturnNull }` (Enumeration)
- `GenerateEfValueConverterAttribute` (Enumeration, VO)
- `GenerateVoJsonConverterAttribute` (VO wrappers)
- `GenerateTypeConverterAttribute` (VO wrappers)
- `enum UnknownValueBehavior { ReturnNull = 0, ThrowException = 1 }`

## Generated APIs (Enumeration)

- `static IReadOnlyCollection<T> GetAll()`
- `static T FromValue(int value)`
- `static T FromName(string name)`
- `static bool TryFromValue(int value, out T? result)`
- `static bool TryFromName(string name, out T? result)`
- Optional: `JsonConverter<T>`, `ValueConverter<T,int>`

## Generated APIs (ValueObject)

- Optimized overrides of `EqualsCore` and `GetHashCodeCore`
- Optional: `JsonConverter<T>`, `TypeConverter`, `ValueConverter<T,object>`

## Domain events

- `abstract record DomainEvent`
  - `Guid Id { get; init; }`
  - `DateTimeOffset OccurredOn { get; init; }`
  - `string GetEventName()`
- `abstract record DomainEventWithMetadata : DomainEvent`
  - `DomainEventMetadata Metadata { get; init; }`
  - `string? UserId`, `Guid? CorrelationId`, `Guid? CausationId`
  - `abstract DomainEventWithMetadata WithMetadata(DomainEventMetadata metadata)`
- `record DomainEventMetadata(Guid EventId, DateTimeOffset OccurredOn, string? UserId = null, Guid? CorrelationId = null, Guid? CausationId = null)`
  - `static Create()`, `CreateWithUser(string userId)`, `CreateWithCorrelation(Guid correlationId, Guid? causationId = null, string? userId = null)`

## Event handlers and dispatcher

- `interface IDomainEventHandler`
  - `bool CanHandle(Type eventType)`
  - `Task HandleAsync(DomainEvent domainEvent, CancellationToken ct = default)`
- `interface IDomainEventHandler<in TEvent> : IDomainEventHandler where TEvent : DomainEvent`
  - `Task HandleAsync(TEvent domainEvent, CancellationToken ct = default)`
- `interface IDomainEventDispatcher`
  - `Task DispatchAsync(DomainEvent domainEvent, CancellationToken ct = default)`
  - `Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken ct = default)`
- `sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher`

## Specifications

- `interface ISpecification<T>`
  - `Expression<Func<T,bool>> Criteria { get; }`
  - `List<Expression<Func<T,object>>> Includes { get; }`
  - `List<string> IncludeStrings { get; }`
  - `Expression<Func<T,object>>? OrderBy { get; }`
  - `Expression<Func<T,object>>? OrderByDescending { get; }`
  - `int? Take { get; }`, `int? Skip { get; }`, `bool IsPagingEnabled { get; }`
  - `bool IsSatisfiedBy(T entity)`

- `abstract class Specification<T> : ISpecification<T>`
  - Constructors: `Specification()`, `Specification(Expression<Func<T,bool>> criteria)`
  - Protected builders:
    - `AddInclude(Expression<Func<T,object>>)`
    - `AddInclude(string)`
    - `ApplyOrderBy(Expression<Func<T,object>>)`
    - `ApplyOrderByDescending(Expression<Func<T,object>>)`
    - `ApplyPaging(int skip, int take)`
  - Combinators: `Specification<T> And(ISpecification<T>)`, `Or(ISpecification<T>)`, `Not()`

- `static class SpecificationEvaluator`
  - `IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecification<T> spec)`

## Repositories (optional)

- `interface IRepository<TEntity, in TId>` where `TEntity : Entity<TId>` and `TId : struct`
  - CRUD + `Find*`, `CountAsync`, `AnyAsync`
- `abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>`
  - Wires dispatch for aggregates via protected `DispatchDomainEventsAsync`

## Exceptions

- `abstract class DomainException : Exception`
  - `virtual string ErrorCode { get; }`
- `EntityNotFoundException(string entityName, object id)`
- `BusinessRuleViolationException(string ruleName, string message)`
- `DomainValidationException(string message)` / `(IDictionary<string,string[]> errors)` / `(string propertyName, string errorMessage)`
- `InvariantViolationException(string invariantName, string message)`
- `InvalidOperationDomainException(string message)` / `(string operation, string reason)`

## Services

Related: [guide.md](guide.md), [examples.md](examples.md).

- `interface IDomainService` (marker)

## Generation attributes (full list)

Enumeration:
- `GenerateJsonConverterAttribute { UnknownValueBehavior Behavior = ReturnNull }`
- `GenerateEfValueConverterAttribute`

Value objects:
- `GenerateVoJsonConverterAttribute`
- `GenerateTypeConverterAttribute`
- `GenerateEfValueConverterAttribute` (for simple wrappers)

## Equality attributes (full list)

- `IncludeInEqualityAttribute(int Priority = 0)`
- `IgnoreEqualityAttribute`
- `SequenceEqualityAttribute(bool OrderMatters = true, bool DeepEquality = true, int Priority = 0)`
- `CustomEqualityAttribute(int Priority = 0)` with required static methods

