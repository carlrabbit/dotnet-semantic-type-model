# EF Core Source Lineage Scope Filtering

## Status

Authoritative behavioral specification for M0054.

## Purpose

Define the scope used by EF source-lineage construction.

## Rule

EF source lineage is derived from EF projection/application scope, not from all object definitions in the canonical semantic model.

## Include

```text
root EF entity source types
owned/value-object source types reachable from projected EF mappings
declaring CLR types for included semantic members
explicit EF-applicable semantic types
```

## Exclude

```text
IEquatable<T>
generic marker/static interfaces
System.Xml helper types
System.Text.Json internals
Dictionary internals
StringComparer
compiler-generated record infrastructure
repository abstractions
DTOs not selected for EF projection
non-semantic base classes as root EF source types
```

## Non-Semantic Bases

A non-semantic base class may provide member declaring-type lineage for inherited members.

It must not become a root EF source type solely because it declares inherited semantic members.

## Diagnostics

Do not emit source-lineage diagnostics for excluded types.

If an excluded type reaches EF lineage scope unexpectedly, emit:

```text
EFCORE_SOURCE_LINEAGE_TYPE_OUT_OF_SCOPE
```

The diagnostic means the lineage scope is too broad or a non-semantic infrastructure type was explicitly configured as EF-applicable by mistake.
