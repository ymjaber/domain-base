# DomainBase

![DomainBase](https://raw.githubusercontent.com/ymjaber/domain-base/main/assets/icon.png)

[![NuGet](https://img.shields.io/nuget/v/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![Downloads](https://img.shields.io/nuget/dt/DomainBase.svg?style=for-the-badge)](https://www.nuget.org/packages/DomainBase/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](https://github.com/ymjaber/domain-base/blob/main/LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-ymjaber%2Fdomain--base-181717?logo=github&style=for-the-badge)](https://github.com/ymjaber/domain-base)

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
dotnet add package DomainBase
```

Targets: `net9.0` (generators/analyzers: `netstandard2.0`).

## Quick start

```csharp
using DomainBase;

// 1) Simple value object wrapper
public sealed partial class Name : ValueObject<Name, string>
{
    public Name(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value));
    }
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

// 3) Smart Enumeration
public sealed partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Submitted = new(1, "Submitted");
    public static readonly OrderStatus Approved = new(2, "Approved");
    public static readonly OrderStatus Rejected = new(3, "Rejected");

    private OrderStatus(int value, string name) : base(value, name) { }


    // You can add domain-specific helpers, like:
    public bool IsFinal() => this == Approved || this == Rejected;
}

// 4) Aggregate root and domain event
public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id) { }

    public bool Submitted { get; private set; }
    public void Submit()
    {
        Submitted = true;
        
        AddDomainEvent(new OrderSubmitted(Id));
    } 
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

This root README is intentionally brief. For full and detailed documentation, see the docs on the repository:

- Index: [docs index](https://github.com/ymjaber/domain-base/tree/main/docs)
- Getting started: [docs/getting-started.md](https://github.com/ymjaber/domain-base/blob/main/docs/getting-started.md)
- Tutorial: [docs/tutorial.md](https://github.com/ymjaber/domain-base/blob/main/docs/tutorial.md)
- Guide: [docs/guide.md](https://github.com/ymjaber/domain-base/blob/main/docs/guide.md)
- API reference: [docs/reference.md](https://github.com/ymjaber/domain-base/blob/main/docs/reference.md)
- Examples: [docs/examples.md](https://github.com/ymjaber/domain-base/blob/main/docs/examples.md)
- Best practices: [docs/best-practices.md](https://github.com/ymjaber/domain-base/blob/main/docs/best-practices.md)
- Why DomainBase: [docs/why-domainbase.md](https://github.com/ymjaber/domain-base/blob/main/docs/why-domainbase.md)
- Contributing: [docs/contributing.md](https://github.com/ymjaber/domain-base/blob/main/docs/contributing.md)
- FAQ: [docs/faq.md](https://github.com/ymjaber/domain-base/blob/main/docs/faq.md)

## Links

- NuGet: [DomainBase on NuGet](https://www.nuget.org/packages/DomainBase/)
- Repository: [github.com/ymjaber/domain-base](https://github.com/ymjaber/domain-base)
- License: MIT ([LICENSE](https://github.com/ymjaber/domain-base/blob/main/LICENSE))

