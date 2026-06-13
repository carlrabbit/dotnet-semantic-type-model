# Decision — Remove Old Model Compatibility and Transition Terminology

## Status

Accepted

## Context

The repository has completed the transition to a code-first canonical semantic model architecture. Current architecture defines a pipeline from annotated .NET code to the canonical semantic model, then through transformations into package-owned domain semantic models and domain functionality.

Some active source and documentation still preserve transition-era compatibility concepts:

- old model shape adapters;
- provider wrappers for old generated models;
- query overloads for old model shapes;
- JSON Schema import as an old canonical model source;
- `hardened` terminology for the current model;
- System.Text.Json resolver customization that consumes old model shapes directly instead of using a derived domain semantic model.

Keeping these concepts makes implementation ambiguous and encourages new features to support two model generations.

## Decision

Remove old model compatibility as supported behavior and use the current canonical semantic model surface as the only runtime and projection input model surface.

Remove active use of `hardened`, `hardening`, and current/legacy split terminology from source comments, active specs, public documentation, samples, and package docs.

Align `SemanticTypeModel.SystemTextJson` with the same domain projection pipeline as JSON Schema, EF Core, and Power BI by introducing a System.Text.Json domain semantic model and driving resolver customization from that model.

## Consequences

- Old model adapters and old model provider wrappers are removed.
- Public or internal APIs that exist only to accept old model shapes are removed.
- JSON Schema import no longer appears as a supported canonical model source.
- Query and inspection APIs target the current canonical model surface and domain semantic models.
- System.Text.Json resolver customization derives a package-owned domain semantic model before applying behavior.
- Tests and samples use code-first extraction or generated providers instead of manual old model construction.
- Consumers relying on old compatibility APIs must move to code-first extraction, generated providers, or persisted snapshots.

## Non-Consequences

- Historical milestone and decision files do not need to be renamed solely for terminology cleanup.
- System.Text.Json still composes with user-owned `JsonSerializerContext` declarations.
- SemanticTypeModel still does not generate `JsonSerializerContext` declarations.
- JSON Schema export remains supported.
- EF Core and Power BI domain projection boundaries remain unchanged except where old compatibility helpers are removed.

## Alternatives Considered

### Keep compatibility behind hidden adapters

Rejected. Hidden adapters keep two model generations alive and contradict the current code-first architecture.

### Mark old APIs obsolete and remove later

Rejected for this milestone. The user intent is to remove old compatibility now, and continued obsolete APIs would still influence samples, tests, and implementation decisions.

### Realign System.Text.Json separately

Rejected. System.Text.Json currently depends on the old model surface, so removing old compatibility and realigning System.Text.Json must be planned together.

## Validation

Implementation must prove the decision by running the milestone validation tiers and confirming:

- no active supported source path consumes old model shapes as a runtime/projection input;
- System.Text.Json has a derived domain semantic model;
- active docs no longer describe the current model as `hardened`;
- samples and tests use the current model methodology.
