# SemanticTypeModel

## Goal

A .NET 10 library for building, querying, and projecting canonical semantic type models.

## Documentation Entry Points

- [docs/TERMINOLOGY.md](docs/TERMINOLOGY.md)
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)
- [docs/SPECS.md](docs/SPECS.md)
- [docs/MILESTONES.md](docs/MILESTONES.md)
- [docs/TBPS.md](docs/TBPS.md)
- [docs/GUARDRAILS.md](docs/GUARDRAILS.md)
- [docs/ENGINEERING.md](docs/ENGINEERING.md)
- [docs/WORKFLOWS.md](docs/WORKFLOWS.md)

## Engineering Commands

See [docs/ENGINEERING.md](docs/ENGINEERING.md).

```sh
./eng/restore.sh   # Restore dependencies
./eng/build.sh     # Build the solution
./eng/test.sh      # Run short-running tests
./eng/format.sh    # Format code
./eng/check.sh     # Full validation (restore + build + test + format check)
./eng/benchmark.sh # Run benchmarks (Release mode)
```

## Development

Requirements:
- .NET 10 SDK

```sh
git clone https://github.com/carlrabbit/dotnet-semantic-type-model
cd dotnet-semantic-type-model
./eng/restore.sh
./eng/build.sh
./eng/test.sh
```
