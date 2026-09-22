# Decision: TestData Coordination State Is Invocation-Scoped

## Status

Accepted for M0084.

## Context

M0083 made TestData Profiles immutable runtime sampling configuration. M0084 adds features that require state while values are generated:

- sequence counters;
- shared/frozen values;
- scoped uniqueness sets;
- dependency ordering for derived values.

That state could technically be stored on the profile, the reusable `model.TestData()` facade, a static/global registry, or an individual generation invocation.

Profile/facade/global state would make repeated calls influence one another, complicate concurrency, make deterministic replay depend on call history, and blur the distinction between reusable configuration and one generation run.

## Decision

All mutable coordination state belongs to a **Generation Session** created for one public generation invocation.

```text
immutable TypeSchemaModel
+ immutable TestDataProfile
+ immutable/reusable facade configuration
        |
        v
Generate / GenerateMany
        |
        v
new Generation Session
        |
        +-- Batch coordination state
        |
        +-- Root state #0
        +-- Root state #1
        +-- ...
        |
        v
discard session
```

`Generate` is one batch containing one root.

`GenerateMany` is one batch containing all roots produced by that call.

A later call, even through the same configured facade instance, creates a new independent Generation Session.

Generation Session state must not be stored in:

- `TypeSchemaModel`;
- `TestDataProfile`;
- `SemanticTerminologyProfile`;
- reusable `SemanticTestDataFacade` configuration;
- static/process/thread-global state.

## Supported scopes

M0084 defines exactly:

```text
Root
Batch
```

Root state is shared across the full graph of one top-level root and resets before the next root.

Batch state spans all roots in the current invocation.

No process/global/caller-named/persistent scope is introduced.

## Concurrency consequence

A configured facade and immutable profile may be reused by concurrent generation calls without sharing coordination state between those calls.

M0084 does not require a facade instance itself to become a mutable session.

## Relationship to later datasets

Batch scope coordinates values inside one homogeneous `GenerateMany` invocation. It does not define a multi-root-type dataset, relationship graph, foreign-key binding model, or persisted dataset identity.

A future dataset capability may orchestrate multiple generation populations, but it must do so explicitly rather than treating M0084 Batch state as hidden application-global state.
