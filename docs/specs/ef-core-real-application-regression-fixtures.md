# EF Core Real Application Regression Fixtures

## Status

Authoritative behavioral specification for M0054.

## Purpose

Define permanent real-application-shaped EF regression fixtures derived from anonymized source material provided by the user.

These fixtures exist because isolated unit tests have not captured the combined EF failure surface seen in realistic models.

## Fixture Policy

Fixtures must be anonymized and rewritten into repository sample terminology.

Do not copy private/business names from source ZIPs into public tests, public docs, or package samples.

Fixtures must preserve structure, not naming.

## Fixture A: Order Intake Specification Model

Required traits:

```text
records
abstract records
sealed records
abstract non-semantic base class
abstract semantic entity base
concrete semantic entity
generic static marker interface
inherited semantic members
JsonExtensionData
SemanticExtensionData
SemanticVersion
SemanticRequiredWhen
many owned value objects
optional owned value objects
owned collection
Uri
DateOnly
TimeOnly
TimeSpan
DateTimeOffset
Guid
```

This fixture validates that EF lineage ignores interface/framework/record infrastructure while preserving derived semantic members.

## Fixture B: Order Fulfillment Run State Model

Required traits:

```text
record struct identifiers
aggregate-like persisted snapshot
nested value objects
execution/failure/control-operation records
IReadOnlyList<T>
IReadOnlyDictionary<string,string>
ReadOnlyMemory<byte>
request DTOs
overview DTOs
repository abstraction
```

This fixture validates that EF projection distinguishes persistence-domain types from DTOs, interfaces, repository abstractions, opaque payloads, dictionaries, and binary values.

## Required Test Layers

```text
unit tests:
  projection and lineage mechanics

ModelBuilder tests:
  real CLR DbContext model construction

SQLite integration tests:
  provider-backed EnsureCreated and minimal persistence where supported
```

All three layers are required for EF compatibility confidence.
