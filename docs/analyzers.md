### Analyzers

Value Object Analyzer diagnostics:
- DBVO001: ValueObject class must be partial
- DBVO002: Property or field missing equality attribute
- DBVO003: Missing custom equality method Equals_{Member}
- DBVO004: Missing custom hash code method GetHashCode_{Member}
- DBVO005: Multiple equality attributes on member
- DBVO006: SequenceEquality on non-sequence type
- DBVO007: Equality attribute on non-ValueObject class
- DBVO008: [ValueObject] attribute without inheritance
- DBVO009: Duplicate method names after cleaning
- DBVO010: Mutable property in ValueObject (must be get-only or init-only)
- DBVO011: Field must be readonly in ValueObject

Enumeration Analyzer diagnostics:
- DBENUM001: Duplicate enumeration value
- DBENUM002: Duplicate enumeration name
- DBENUM003: Enumeration class must be partial

