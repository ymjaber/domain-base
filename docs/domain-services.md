### Domain Services

Stateless operations that involve multiple aggregates belong in domain services. Implement `IDomainService` to mark them.

```csharp
public interface IExchangeRateService : IDomainService
{
    Money Convert(Money amount, Currency targetCurrency);
}
```

