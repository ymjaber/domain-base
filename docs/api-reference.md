### API Reference (Overview)

- Core Types:
  - `Entity<TId>`
  - `AggregateRoot<TId>`
  - `DomainEvent`, `IDomainEventHandler<TEvent>`, `IDomainEventDispatcher`
  - `ValueObject<TSelf>`, `ValueObject<TSelf, TValue>`
  - `Enumeration`
  - `ISpecification<T>`, `Specification<T>`, `SpecificationEvaluator`
  - `IRepository<TEntity,TId>`, `Repository<TEntity,TId>`

- Generation Attributes:
  - `[ValueObject]`
  - `[IncludeInEquality]`, `[CustomEquality]`, `[SequenceEquality]`, `[IgnoreEquality]`
  - `[GenerateJsonConverter]`, `[GenerateEfValueConverter]`
  - `[GenerateVoJsonConverter]`, `[GenerateTypeConverter]`

See dedicated pages for examples and guidance.

