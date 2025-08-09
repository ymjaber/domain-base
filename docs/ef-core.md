### EF Core Integration

- Use `Repository<TEntity,TId>` to dispatch domain events after SaveChanges
- For Enumerations, add `[GenerateEfValueConverter]` to generate a converter
- For Value Objects wrapping a single value, add `[GenerateEfValueConverter]` to generate a converter

