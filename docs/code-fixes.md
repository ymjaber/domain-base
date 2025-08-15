# Code fixes

The package bundles code fixes to accelerate common corrections when using value objects and enumerations.

## Make class partial

- Fixes: DBVO001, DBEN003
- Provider: `MakeClassPartialCodeFixProvider`
- Action: Adds the `partial` modifier to the class declaration

## Add equality attribute

- Fixes: DBVO002
- Provider: `AddEqualityAttributeCodeFixProvider`
- Actions:
  - Add `[IncludeInEquality]`
  - Add `[IgnoreEquality]`
  - Add `[SequenceEquality]` (only when the member is a sequence)

## Generate custom equality methods

- Fixes: DBVO003, DBVO004
- Provider: `GenerateCustomEqualityMethodsCodeFixProvider`
- Action: Generates the required static methods for `[CustomEquality]` members:
- `private static bool Equals_Name(in T value, in T otherValue)`
  - `private static int GetHashCode_Name(in T value)`

---

Previous: [Analyzers](analyzers.md) · Next: [Reference](reference.md)