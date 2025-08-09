# Why DomainBase

Domain models deserve first-class ergonomics. DomainBase focuses on:

- Productivity: generate equality and infrastructure glue instead of writing it by hand
- Safety: analyzers surface mistakes early with actionable code fixes
- Performance: optimized equality; AOT-friendly; minimal allocations
- Clarity: small, well-named primitives instead of heavy frameworks
- Interop: JSON converters, EF Core converters, and type converters when you need them

## When to use it

- You want clean DDD primitives without adopting a full-blown framework
- You value compile-time guidance and code fixes for value objects and enumerations
- You need domain events and a simple dispatcher to integrate with DI
- You prefer explicit specifications for query logic

## When not to use it

- You need a full persistence/ORM repository framework out-of-the-box
- You prefer runtime reflection-heavy libraries over compile-time generation

