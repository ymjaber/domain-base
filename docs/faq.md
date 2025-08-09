# FAQ

Does this replace EF Core or MediatR?
-------------------------------------

No. DomainBase provides DDD primitives, generators, and analyzers. It plays well with EF Core and your mediator/event bus of choice. An in-memory dispatcher is included for simple setups.

Do I need to use the repository base?
-------------------------------------

No. It's optional. If your architecture prefers direct DbContext usage or query side libraries, skip it.

Why `partial` classes for value objects and enumerations?
--------------------------------------------------------

So the source generators can augment your types without reflection or runtime cost. Analyzers ensure you don’t forget.

Can I customize equality ordering?
----------------------------------

Yes. Use `Priority` on `[IncludeInEquality]`, `[SequenceEquality]`, or `[CustomEquality]`. Higher priority members are compared first.

How do JSON converters handle unknown enum values?
--------------------------------------------------

Use `[GenerateJsonConverter(Behavior = UnknownValueBehavior.ThrowException)]` to be strict. The default returns null for unknown values.

Is this AOT-friendly?
---------------------

More: [guide.md](guide.md) and [reference.md](reference.md). NuGet: [DomainBase](https://www.nuget.org/packages/DomainBase/)

Yes. No runtime IL emit is used. Expression compilation uses interpretation.

