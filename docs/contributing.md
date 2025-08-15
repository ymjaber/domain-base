# Contributing

Thank you for considering a contribution!

## How to build

1. Requirements: .NET SDK 9
2. Build the solution:

```bash
dotnet build -c Release
```

3. Run tests:

```bash
dotnet test -c Release
```

Changes that include analyzers/generators should keep tests green in:

- `tests/DomainBase.Generators.Tests`
- `tests/DomainBase.Analyzers.Tests`
- `tests/DomainBase.Tests`

## Pull requests

- Open an issue first for substantial changes
- Write tests where practical
- Match existing code style; keep code readable and explicit
- Avoid adding new dependencies unless necessary

## Docs

- Update `docs/` when changing public APIs or behavior
- Keep examples short and runnable

## Releasing (maintainers)

- Update version in project files
- Ensure root `README.md` is accurate (used as NuGet readme)
- Tag and publish package

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Back to index: [Docs index](README.md)
