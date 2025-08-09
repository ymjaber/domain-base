### Migration Guide

- Replace primitive types with value objects using `[ValueObject]`
- Convert C# enums to `Enumeration` classes for behavior and safety
- Introduce `Specification<T>` for query logic
- Introduce `AggregateRoot<TId>` and domain events for side effects

