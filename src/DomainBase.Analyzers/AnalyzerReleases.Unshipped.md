; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DBENUM001 | Usage | Error | Duplicate enumeration value
DBENUM002 | Usage | Error | Duplicate enumeration name
DBENUM003 | Usage | Error | Enumeration class must be partial
DBVO001 | Usage | Error | ValueObject class must be partial
DBVO002 | Usage | Warning | Property or field missing equality attribute
DBVO003 | Usage | Error | Missing custom equality method
DBVO004 | Usage | Error | Missing custom hash code method
DBVO006 | Usage | Info | SequenceEquality on non-sequence type
DBVO007 | Usage | Error | Equality attribute on non-ValueObject class
DBVO008 | Usage | Error | ValueObject attribute without inheritance
DBVO009 | Naming | Error | Duplicate method names after cleaning
DBVO005 | Usage | Error | Multiple equality attributes on member
DBVO010 | Usage | Error | Mutable property in ValueObject
DBVO011 | Usage | Error | Mutable field in ValueObject

