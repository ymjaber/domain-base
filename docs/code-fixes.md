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
  - `private static void Equals_Name(in T value, in T otherValue, out bool result)`
  - `private static void GetHashCode_Name(in T value, ref HashCode hashCode)`