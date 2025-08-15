# DomainBase

![DomainBase](https://raw.githubusercontent.com/ymjaber/domain-base/main/assets/logo.png)

[![NuGet](https://img.shields.io/nuget/v/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![Downloads](https://img.shields.io/nuget/dt/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](https://github.com/ymjaber/domain-base/blob/main/LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-ymjaber%2Fdomain--base-181717?logo=github&style=for-the-badge)](https://github.com/ymjaber/domain-base)

Lightweight but feature-rich, pragmatic building blocks for Domain-Driven Design (DDD) in .NET: entities, aggregate roots, value objects, domain events, enumerations, and more. Includes source generators and analyzers to keep your domain model clean, safe, and fast.

## Table of contents

- [Why DomainBase](#why-domainbase)
- [Install](#install)
- [DDD primer](#ddd-primer)
- [Types and rules](#types-and-rules)
- [Documentation](#documentation)
- [Links](#links)

## Why DomainBase

- **Clear, minimal primitives**: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject<TSelf>`, `Enumeration`, `DomainEvent`
- **Batteries included**: analyzers (diagnostics + code fixes) and source generators that eliminate boilerplate
- **Enhanced types**: Source generators give extensive capabilities to the value objects and enumerations.
- **AOT-friendly and fast**: AOT compatibility is enabled, optimized equality, no runtime code emission or reflection

## Install

```bash
dotnet add package DomainBase
```

Targets: `net9.0` (generators/analyzers: `netstandard2.0`).

## DDD tactical patterns

The following are some of the tactical patterns of DDD which are included in this library, with brief and too short description for each.

- **Entity**: Entities are compared with their identity (`Id`) and have a lifecycle. Two entities can only be equal if they have the same id, regardless of their other properties. Example: `Order`, `Customer`.
- **Value object**: Identity-less, immutable values compared by their content. Value objects use structural equality, which means that 2 value objects can only be equal if all their properties are equal. You should usually aim to move your business logic and domain rules towards value objects inside your entities. Example: `Money`, `Email`.
- **Aggregate & aggregate root**: An aggregate is a cluster of entities and value objects that change together. The root (e.g., `Order`) is the only entry point. The aggregate is your transactional consistency boundary: enforce invariants inside it and commit changes atomically.
- **Domain event**: Something significant that happened in the domain (e.g., `OrderSubmitted`). Raised by the aggregate root and handled asynchronously.
- **Domain service**: Domain behavior that doesn’t belong to a particular aggregate. Its usually used to share behavior between two or more aggregates. You should always try to avoid them if not needed, and move your business logic to your entities rather than handling them with domain services. For more information, you can search about `Rich vs anemic domain models` in DDD. You can find many useful videos on YouTube or any other platform about this topic.

> **NOTE**: Before using `DomainBase` library, you should already be familiar with DDD, and at least created your own basic classes for implementing the value object and entity equality behaviors, to know how they exactly work. I will leave you with some of my favourite resources to learn about DDD at the end of this file.

## Types and rules

### Entities and aggregate roots

- **What they are**: Objects with identity (`Id`). Aggregates group related entities/value objects and can raise domain events.
- **Use when**: The thing has a life-cycle and identity (e.g., `Order`, `Customer`).
- **Rules**:
    - Compare by `Id`.
    - The aggregate is a transactional consistency boundary. Keep invariants inside; only the root exposes behaviors that mutate state.
    - Use `AddDomainEvent` on the root to record significant changes; dispatch and clear after saving.

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }
    public bool Submitted { get; private set; }
    public void Submit() { if (Submitted) return; Submitted = true; AddDomainEvent(new OrderSubmitted(Id)); }
}
public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;
```

### Value objects

- **What they are**: Immutable values compared by their content (not identity).
- **Three approaches to declare equality behaviors (more explanation in the "Guide" section)**:
    - **Wrapper approach**: `ValueObject<TSelf, TValue>`. This is used when the value object has only 1 single value wrapped within it.

    ```csharp
    // Now the type `Email` will have a property caled `Value` of type `string`
    public sealed class Email : ValueObject<Email, string>
    {
        public Email(string value) : base(value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(name(value));
        }
    }




    // Usage 1
    Email email = new Email("example@example.com");

    string emailValue = email.Value;
    // OR simply
    string emailValue = email; // implicit conversion the value type



    // Usage 2

    Email email1 = new Email("example@example.com");
    Email email2 = new Email("example@example.com");

    email1 == email2 // true because their values are equal
    ```

    - **Manual approach**: `ValueObject<TSelf>` + your own `EqualsCore`/`GetHashCodeCore`. This is the default VO initializer (although the other ones are recommended in most cases). (You maybe familiar wit this approach as most instructors and authors use it in their courses or references).

    > **NOTE**: In most cases, its recommended to use either the wrapper (the above example) in case you have a single value, OR add the [ValueObject] attribute (the example after this one) to trigger the source generator to automatically generate the `EqualsCore` and `GetHashCodeCore` for you.

    ```csharp
    public sealed class PersonName : ValueObject<PersonName>
    {
        public PersonName(string first, string second)
        {
            First = first;
            Second = second;
        }

        public string First { get; }
        public string Second { get; }

        protected override bool EqualsCore(PersonName other) => First == other.First && Second == other.Second;
        protected override int GetHashCodeCore() => HashCode.Combine(First, Second);
    }
    ```

    - **Generator-driven approach**: Adding `partial` flag to the value object with `[ValueObject]` and property attributes. This will trigger the source generator to automatically generate the `EqualsCore` and `GetHashCodeCore` behind the scenes. So no need to implement those methods manually anymore, which greatly enhances readability and maintainability (More about the generator-driven approach and its options in the "Guide" section)

    ```csharp
    [ValueObject]
    public sealed partial class Post : ValueObject<Post>
    {
        // The sequence equality here informs the source generator to compare the elements of the list one by one
        // More about members equality attributes in the "Guide" section
        [SequenceEquality] private readonly List<string> _comments;

        [IncludeInEquality] public string Title { get; }
        [IncludeInEquality] public string Body { get; }

        public Post(string title, string body, List<string> comments)
        {
            Title = title;
            Body = body;
            _comments = comments;
        }

        // Normally The static code analyzer will raise a warning if you didn't implement an equality attribute like [IncludeInEquality] or [IgnoreEquality] for example, as a reminder to prevent you from missing an auto property or field
        // But here the analyzer knows that this is not an auto property nor a field, and thus, will not raise a warning
        public ReadOnlyList<string> Comments => _comments;
    }
    ```

- **Rules**:
    - Keep members immutable (get-only or init-only; fields `readonly`).
        > Not assigning a field with `readonly` or adding the `set` keyword to a property will trigger a warning for misusing the value object
    - Equality attribute per member to define the equality behavior:
        - `[IncludeInEquality]`: Uses the normal `.Equals` and `GetHashCode` methods to compare the members
        - `[IgnoreEquality]`: Ignores the member when comparing two value objects or getting the hash code of a value object
        - `[SequenceEquality]`: This is used for types implementing the `IEnumerable<T>`. It tells the source generator to compare the elements inside the collection rather than the collection itself. This attribute comes with 2 additional options described in the "Guide" section.
        - `[CustomEquality]`: Lets you define the Equals and HashCode manually. This can be useful for some cases.
        - Additional Options will be added soon in a later subversion (for example, floating numbers with specific precisions).
    - Each member equality attribute (except the `[IgnoreEquality]`) comes with an optional `Order` property, which is an integer that lets you choose which members to compare first. (More about it in the "Guide" and "Best Practices" sections).

### Enumerations

- **What they are**: Smart, type-safe alternatives to enums. They are simply _"enums with behaviour"_, and comes with comes with source generators to add lookup functionality methods at compile-time rather than run-time.
- **Use when**: You need named constants with behavior and lookup helpers.
- **Rules**: Make the class `partial`. Each instance has unique `Value` and `Name` (The static code analyzer will raise and error when it finds a duplicate value or name).
- **Helpers**: `GetAll()`, `FromValue`, `FromName`, `TryFromValue`, `TryFromName`. Those methods are all generated automatically once you derive the class from the `Enumeration` type and make it `partial`

```csharp
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Draft = new(0, "Draft");
    public static readonly OrderStatus Submitted = new(1, "Submitted");

    private OrderStatus(int value, string name) : base(value, name) { }

    public bool CanSubmit() => this == Draft;
}
```

### Domain events and metadata

- **What they are**: Notifications raised by aggregates when something important happens.
- **Rules**:
    - Events are records with `Id` and `OccurredOn`.
    - Make it as minimal as possible. Don't implement more than the information needed in the event. You should only add the required information needed to do some functionality when the event is consumed.

```csharp
public sealed record ArticlePublishedDomainEvent(string authorName);

var event = new ArticlePublishedDomainEvent("Yousef");
```

For flexibility reasons, This `DomainBase` library doesn't depend on any other library. If you are using `mediatR` or any other similar library, then you may want to add another abstraction to the domain layer by doing the following:

1. Add the package `MediatR.Contracts` (or the whole package `MediatR`) to the domain layer.
2. Create an abstract record that implements the `DomainEvent` base class and the `INotification`

```csharp
public abstract record NotifyableDomainEvent : DomainEvent, INotification
{
    protected DomainEvent() : base()
    {
    }

    protected DomainEvent(Guid id, DateTimeOffset occurredOn) : base(id, occurredOn)
    {
    }

}
```

3. The domain events derive from the `NotifyableDomainEvent` rather than `DomainEvent` directly

### Specifications, Repositories, Handlers, Dispatchers

- Those where meant to help in consuming the domain functionality in the upper layers.
- They were removed to make the library focused only on the core domain layer and for flexibility and variety reasons.
- Extension libraries will be added later to be implemented later, each aimed for specific set of tasks.

### Exceptions

- Its always advisable to use descriptive errors rather than normal exceptions for defining the domain-specific errors. This could be handled either by defining custom exceptions, or by using the result pattern. `DomainBase` doesn't implement the Result pattern and only defines base base exceptions to either use directly or derive from.
- All derive from `DomainException`:
    - `DomainValidationException`
    - `DomainConflictException`
    - `DomainNotFoundException` / `DomainNotFoundException<TId>`

> **Note**: This is an optional feature. If you are using the result pattern to control the flow of the domain-specific errors, then depending on how strictly you implement them, you might not want to use domain exceptions at all.

This is a good place to present my other 2 packages which provide rich beneficial features in this area:

- [FluentUnions](https://github.com/ymjaber/fluent-unions): This is a very rich library for the Result and Option patterns.
- [FluentEnforce](https://github.com/ymjaber/fluent-enforce): This includes built-in validations to throw exceptions when rules are not satisfied. It can be combined with the `Exceptions` feature of this `DomainBase` library for making specific validations and throw custom domain exceptions.

### Services

- `IDomainService`: a small marker for domain services to provide behavior that don't belong to single aggregate.

## Documentation

This root README is intentionally brief. For full and detailed documentation, see the docs on the repository:

- Index: [docs index](https://github.com/ymjaber/domain-base/tree/main/docs)
- Guide: [docs/guide.md](https://github.com/ymjaber/domain-base/blob/main/docs/guide.md)
- API reference: [docs/reference.md](https://github.com/ymjaber/domain-base/blob/main/docs/reference.md)
- Examples: [docs/examples.md](https://github.com/ymjaber/domain-base/blob/main/docs/examples.md)
- Best practices: [docs/best-practices.md](https://github.com/ymjaber/domain-base/blob/main/docs/best-practices.md)
- Contributing: [docs/contributing.md](https://github.com/ymjaber/domain-base/blob/main/docs/contributing.md)

## Resources

Short list of high-signal materials to learn and apply DDD well:

- Domain-Driven Design: Tackling Complexity in the Heart of Software — Eric Evans
- Implementing Domain-Driven Design — Vaughn Vernon
- Effective Aggregate Design (free article series) — Vaughn Vernon (`https://vaughnvernon.co/?page_id=168`)
- Enumeration pattern write-ups — Jimmy Bogard (`https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/`)
- Smart Enum and related posts — Steve Smith (Ardalis) (`https://ardalis.com/tag/smartenum/`)

## Links

- NuGet: [DomainBase on NuGet](https://www.nuget.org/packages/DomainBase/)
- Repository: [github.com/ymjaber/domain-base](https://github.com/ymjaber/domain-base)
- License: MIT ([LICENSE](https://github.com/ymjaber/domain-base/blob/main/LICENSE))
