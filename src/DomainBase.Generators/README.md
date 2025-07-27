# DomainBase Generators

This project contains source generators for the DomainBase library.

## Enumeration Generator

The Enumeration source generator provides compile-time generation of helper methods for enumeration types, eliminating the need for reflection and improving performance.

### Usage

1. Mark your enumeration class with the `[Enumeration]` attribute
2. Make the class `partial`
3. The generator will create optimized implementations of:
   - `GetAll()` - Returns all enumeration values
   - `FromValue(int id)` - Gets enumeration by ID
   - `FromName(string name)` - Gets enumeration by name
   - `TryFromValue(int id, out T result)` - Tries to get by ID
   - `TryFromName(string name, out T result)` - Tries to get by name

### Example

```csharp
[Enumeration(GenerateJsonConverter = true)]
public partial class OrderStatus : Enumeration
{
    public static readonly OrderStatus Pending = new(1, "Pending");
    public static readonly OrderStatus Processing = new(2, "Processing");
    public static readonly OrderStatus Shipped = new(3, "Shipped");
    public static readonly OrderStatus Delivered = new(4, "Delivered");
    
    private OrderStatus(int id, string name) : base(id, name) { }
}

// Usage
var all = OrderStatus.GetAll(); // No reflection!
var status = OrderStatus.FromValue(2); // O(1) lookup
var shipped = OrderStatus.FromName("Shipped"); // O(1) lookup
```

### Options

- `GenerateJsonConverter` (default: true) - Generates a System.Text.Json converter
- `GenerateEfValueConverter` (default: false) - Generates an EF Core value converter

### Benefits

- **Performance**: No reflection overhead, O(1) lookups
- **AOT-friendly**: Works with Native AOT compilation
- **IntelliSense**: Full IDE support for generated methods
- **Type-safe**: Compile-time checking of enumeration usage
- **Validation**: Compile-time detection of duplicate IDs and names
- **Memory efficient**: Uses dictionaries instead of arrays, no data duplication

### Compile-Time Validation

The generator validates your enumerations and reports errors for:

- **DBENUM001**: Duplicate enumeration ID - When two or more enumeration values have the same ID
- **DBENUM002**: Duplicate enumeration name - When two or more enumeration values have the same name

Example of invalid enumeration:
```csharp
[Enumeration]
public partial class Status : Enumeration
{
    public static readonly Status Active = new(1, "Active");
    public static readonly Status Pending = new(1, "Pending"); // Error DBENUM001: Duplicate ID '1'
    public static readonly Status Inactive = new(2, "Active"); // Error DBENUM002: Duplicate name 'Active'
    
    private Status(int id, string name) : base(id, name) { }
}
```

These errors will be shown in your IDE and prevent compilation, ensuring your enumerations are always valid.