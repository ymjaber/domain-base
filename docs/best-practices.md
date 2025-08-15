# Best Practices and Code Smells

## Value objects

- Prefer immutable init-only properties. Analyzers will warn on mutable members (DBVO010/011)
- Use `Order` to short-circuit equality on cheap, selective members first (e.g., Id then Name)
- For sequences, set `OrderMatters = false` if semantic equality is set-based. Use `DeepEquality = false` for reference comparison of large graphs
- Avoid putting behavior that mutates internal state; return new instances instead. This is related to the first tip, as making members mutable (setter keywords for properties or not making a field readonly) will trigger warning.
- For single-property VOs, prefer simple wrappers (`ValueObject<TSelf, TValue>`). This class includes a proprety `public TValue Value { get; }` and overrides the `EqualsCore` and `GetHasCodeCore` from the original base class.
- Reserve `[CustomEquality]` for complex multi-member VOs. When you use it, the analyzer will give you an error to warn you to implement the following methods.
    - `private static bool Equals_<Name>(in T value, in T otherValue)`
    - `private static int GetHashCode_<Name>(in T value)`
    - You can use the code fix (in your IDE) to add them for you automatically. The source generator includes those methods in the auto-generated `EqualsCore` and `GetHashCodeCore`.

## Custom equality

- Only use `[CustomEquality]` when default comparers are insufficient (e.g., case-insensitive string)
- Provide both required static methods, and keep them fast and allocation-free

## Entities and aggregates

- Keep invariants inside aggregates. Raise `DomainEvent`s for side-effects
- Use `AddDomainEvent` from within domain methods only. Clear events after persistence in your infrastructure
- Keep aggregate constructors minimal. Expose intention-revealing and ubiquitous language terms methods (e.g., `Submit()`, `Pay()`) and raise domain events there
- Entities compare by Id and type. Value objects compare by structure and type

### Aggregate design

- Keep aggregates small and cohesive around invariants. Avoid giant aggregates.
- Model business workflows with explicit methods and rich `Enumeration` behavior on statuses.
- Prefer behavior on Enumerations (e.g., `CanSubmit`, `CanPay`) versus switch statements scattered across the domain.

## Enumerations

- Keep `Value` unique and stable; the analyzer detects duplicates (DBEN001/002)
- Make classes `partial`. The analyzer enforces this (DBEN003)
- Use static readonly instances with stable `(int value, string name)`

### Edge cases

- Two instances with the same `(value, name)` in the same type cause a diagnostic. The analyzer will already give you an error for duplicate names or values.
- Use `FromValue`/`FromName` for strict parsing. Use `TryFromValue`/`TryFromName` to avoid exceptions.
- Add behavior to Enumerations. If you have none, consider a plain enum instead.

## Domain services

- Keep services thin and stateless. Prefer pushing logic to entities/VOs first. Avoid anemic domain models.
- Use services to coordinate across aggregates or integrate external policies (pricing, currency, time).
- Implement the marker interface `IDomainService` in you domain services.

---

Previous: [Examples](examples.md) · Back to index: [Docs index](README.md)
