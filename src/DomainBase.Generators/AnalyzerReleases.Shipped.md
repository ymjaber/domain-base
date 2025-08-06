; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.1.0

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