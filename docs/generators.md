### Generators

This library ships two source generators:

- Enumeration Generator: augments partial classes inheriting `Enumeration` with helper methods and optional JSON/EF converters. See `docs/enumerations.md`.
- Value Object Generator: augments classes marked `[ValueObject]` and inheriting `ValueObject<TSelf>` with optimized equality, sequence helpers, and optional converters. See `docs/value-objects.md`.

