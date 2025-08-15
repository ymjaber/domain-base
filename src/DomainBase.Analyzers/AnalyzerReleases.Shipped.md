## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DBEN001 | Usage | Error | Duplicate enumeration value
DBEN002 | Usage | Error | Duplicate enumeration name
DBEN003 | Usage | Error | Enumeration class must be partial
DBVO001 | Usage | Error | ValueObject class must be partial
DBVO002 | Usage | Warning | Property or field missing equality attribute
DBVO003 | Usage | Error | Missing custom equality method
DBVO004 | Usage | Error | Missing custom hash code method
DBVO005 | Usage | Error | Multiple equality attributes on member
DBVO006 | Usage | Error | SequenceEquality on non-sequence type
DBVO007 | Usage | Error | Equality attribute on non-ValueObject class
DBVO008 | Usage | Error | ValueObject attribute without inheritance
DBVO009 | Naming | Error | Duplicate method names after cleaning
DBVO010 | Usage | Warning | Mutable property in ValueObject
DBVO011 | Usage | Warning | Mutable field in ValueObject
DBVO012 | Usage | Warning | Additional members in simple ValueObject
DBVO013 | Usage | Warning | Duplicate order on equality members
DBVO014 | Usage | Warning | [ValueObject] attribute unnecessary on simple wrapper
DBVO015 | Usage | Error | Equality attribute on unsupported member

