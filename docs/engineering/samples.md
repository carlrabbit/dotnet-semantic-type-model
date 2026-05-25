# Samples Engineering

## Purpose

Define constraints and validation for runnable samples used as executable documentation.

## Validation Command

```sh
./eng/samples.sh
```

## Rules

- Samples live under `samples/`.
- Samples are runnable and deterministic.
- Samples do not replace tests.
- Samples must not introduce local README files.
- Samples must not require secrets.
- Sample documentation lives under `public-docs/samples/`.

## Required Sample Set (M0012)

- JSON Schema roundtrip
- .NET generator to JSON Schema
- Runtime DI usage
- Power BI projection
- EF Core projection

## Document Contract

When sample policy changes, review and update:
- `public-docs/samples.md`
- `public-docs/samples/*.md`
- `eng/samples.sh`
