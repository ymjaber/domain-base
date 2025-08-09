# DomainBase

![DomainBase](assets/icon.png)

[![NuGet](https://img.shields.io/nuget/v/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![Downloads](https://img.shields.io/nuget/dt/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](LICENSE)

Lightweight, pragmatic building blocks for Domain-Driven Design (DDD) in .NET: entities, aggregate roots, value objects, domain events, specifications, repositories, and more. Includes source generators and analyzers to keep your domain model clean, safe, and fast.

## Table of contents

- [Why DomainBase](#why-domainbase)
- [Install](#install)
- [Quick start](#quick-start)
- [Documentation](#documentation)
- [Links](#links)

## Why DomainBase

- Clear, minimal primitives: `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject<TSelf>`, `Enumeration`, `DomainEvent`, `Specification<T>`
- Batteries included: analyzers (diagnostics + code fixes) and source generators that eliminate boilerplate
- AOT-friendly and fast: optimized equality, no runtime code emission
- Production-minded: exceptions, auditing interfaces, domain event dispatcher

## Install

```bash
dotnet add package DomainBase --version 2.0.0
```

Targets: `net9.0` (generators/analyzers: `netstandard2.0`).

## Quick start

```csharp
using DomainBase;

// 1) Simple value object wrapper
[GenerateVoJsonConverter]
[GenerateTypeConverter]
public sealed partial class OrderId : ValueObject<OrderId, Guid>
{
    public OrderId(Guid value) : base(value) { }
}

// 2) Rich value object with generated equality
[ValueObject]
public sealed partial class Money : ValueObject<Money>
{
    [IncludeInEquality] public decimal Amount { get; init; }
    [IncludeInEquality] public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}

// 3) Enumeration with helper APIs & JSON converter
[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Approved = new(2, "Approved");
    public static readonly OrderStatus Rejected = new(3, "Rejected");
    public OrderStatus(int value, string name) : base(value, name) { }
}

// 4) Aggregate root and domain event
public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }
    public void Submit() => AddDomainEvent(new OrderSubmitted(Id));
}

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;

// 5) Register dispatcher and handlers (register handlers as non-generic)
// services.AddSingleton<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
// services.AddScoped<IDomainEventHandler, OrderSubmittedHandler>();

public sealed class OrderSubmittedHandler : IDomainEventHandler<OrderSubmitted>
{
    public Task HandleAsync(OrderSubmitted domainEvent, CancellationToken ct = default)
    {
        // ... react
        return Task.CompletedTask;
    }
}
```

## Documentation

- Start here: [docs index](docs/README.md)
- Getting started: [docs/getting-started.md](docs/getting-started.md)
- Tutorial: [docs/tutorial.md](docs/tutorial.md)
- Guide: [docs/guide.md](docs/guide.md)
- API reference: [docs/reference.md](docs/reference.md)
- Examples: [docs/examples.md](docs/examples.md)
- Best practices: [docs/best-practices.md](docs/best-practices.md)
- Why DomainBase: [docs/why-domainbase.md](docs/why-domainbase.md)
- Contributing: [docs/contributing.md](docs/contributing.md)
- FAQ: [docs/faq.md](docs/faq.md)

## Links

- NuGet: [DomainBase on NuGet](https://www.nuget.org/packages/DomainBase/)
- Repository: [github.com/ymjaber/domain-base](https://github.com/ymjaber/domain-base)
- Docs index: [docs/README.md](docs/README.md)
- Why use this library: [docs/why-domainbase.md](docs/why-domainbase.md)
- Contributing: [docs/contributing.md](docs/contributing.md)
- FAQ: [docs/faq.md](docs/faq.md)
- License: MIT ([LICENSE](LICENSE))

