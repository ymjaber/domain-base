# Best practices and code smells

## Value objects

- Prefer immutable init-only properties; analyzers will warn on mutable members (DBVO010/011)
- Annotate exactly the members that define equality; one attribute per member
- Use `Priority` to short-circuit equality on cheap, selective members first (e.g., Id then Name)
- For sequences, set `OrderMatters = false` if semantic equality is set-based; use `DeepEquality = false` for reference comparison of large graphs
- Avoid putting behavior that mutates internal state; return new instances instead

## Custom equality

- Only use `[CustomEquality]` when default comparers are insufficient (e.g., case-insensitive string)
- Provide both required static methods, and keep them fast and allocation-free

## Entities and aggregates

- Keep invariants inside aggregates; raise `DomainEvent`s for side-effects
- Use `AddDomainEvent` from within domain methods only; clear events after persistence (the base `Repository` handles this)
- Use `Auditable*` variants when you need timestamps/user tracking

## Enumerations

- Keep `Value` unique and stable; the analyzer detects duplicates (DBEN001/002)
- Make classes `partial`; the analyzer enforces this (DBEN003)
- Consider `[GenerateJsonConverter]` with `Behavior = ThrowException` when you need strict JSON

## Specifications

- Keep `Criteria` side-effect free and translatable by your LINQ provider
- Use `ApplyOrderBy`/`ApplyOrderByDescending` and `ApplyPaging` for paging-friendly APIs
- Compose small specifications with `And/Or/Not`

## Repositories

- Optional abstraction; use only if it adds value in your architecture
- Ensure persistence hooks do not throw after events have been raised; failures should roll back before dispatch

## Code smells

See also: [examples.md](examples.md) and [guide.md](guide.md).

- Mutable value object members
- Equality depending on non-essential fields
- Aggregates exposing setters or collections directly
- Domain events that mirror CRUD without domain meaning
- Fat repositories with non-domain responsibilities

