# Analyzers

The analyzers run in the IDE and at build-time to catch issues early and offer fixes.

## ValueObject analyzer (DomainBase.Analyzers.ValueObjectAnalyzer)

Diagnostics:

- DBVO001 (Error): ValueObject class must be partial
- DBVO002 (Warning): Property or field missing equality attribute
- DBVO003 (Error): Missing custom equality method `Equals_Name`
- DBVO004 (Error): Missing custom hash code method `GetHashCode_Name`
- DBVO005 (Error): Multiple equality attributes on member
- DBVO006 (Error): `[SequenceEquality]` used on non-sequence type
- DBVO007 (Error): Equality attribute on non-ValueObject class
- DBVO008 (Error): `[ValueObject]` without inheriting `ValueObject<TSelf>`
- DBVO009 (Error): Duplicate custom method names after cleaning
- DBVO010 (Warning): Mutable property (must be get-only or init-only)
- DBVO011 (Warning): Mutable field (must be readonly)
- DBVO012 (Warning): Additional members in simple value object wrappers
- DBVO013 (Warning): Duplicate order on equality members
- DBVO014 (Warning): `[ValueObject]` attribute unnecessary on simple wrapper
- DBVO015 (Error): Equality attribute on unsupported member

## Enumeration analyzer (DomainBase.Analyzers.EnumerationAnalyzer)

Diagnostics:

- DBEN001 (Error): Duplicate enumeration value
- DBEN002 (Error): Duplicate enumeration name
- DBEN003 (Error): Enumeration class must be partial

## Suppression

Although its highly not advisable, You can suppress diagnostics via `#pragma warning disable` or `.editorconfig` as needed.

---

Previous: [Generators](generators.md) · Next: [Code fixes](code-fixes.md)

