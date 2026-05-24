# M0005 - Transformation Pipeline and Diagnostics Hardening

## Purpose

Harden the reusable transformation pipeline and diagnostics surface around the canonical hardened `TypeSchemaModel`.

## Delivered Runtime Surface

- `SchemaTransformationPipeline.Create().Use(...).RunAsync(...)` sequential runtime pipeline execution.
- `SchemaPipelineOptions` and `SchemaPipelineResult` for deterministic execution and structured result handling.
- `SchemaTransformContext`, `SchemaDiagnosticSink`, and `AnnotationPolicy` for reusable transform composition.
- Built-in transformations:
  - `NormalizeNamesTransformation`
  - `NormalizeAnnotationsTransformation`
  - `ValidateModelTransformation`
- Expanded `SchemaDiagnostic` contract with stage, pipeline stage, related model paths, and projection/source metadata.
- Stable `ModelPath` helpers covering types, properties, keys, relationships, computed members, enum values, and annotation locations.

## Mutability Model

- Pipeline input models are treated as immutable.
- `SchemaTransformationPipeline` clones the input model before running any transformation.
- Transformations receive a `TypeSchemaModelBuilder` wrapper around the working model and replace the working model explicitly.
- Pipeline output is a fresh immutable model snapshot.

## Execution Behavior

- Transformations run sequentially in configured order.
- Diagnostics are accumulated across all executed transformations.
- Warnings do not stop execution.
- By default, the pipeline stops before the next transformation after an error diagnostic is recorded.
- `SchemaPipelineOptions.ContinueOnError` allows continued execution after errors.
- `SchemaPipelineOptions.PromoteWarningsToErrors` upgrades warnings at sink time.
- Cancellation is honored between transformations and through transform `CancellationToken` parameters.
- M0005 does not introduce parallel transform execution.

## Diagnostic Model

`SchemaDiagnostic` entries now carry:

- severity;
- stable code;
- human-readable message;
- canonical model path;
- source path/span text when available;
- stage (`Import`, `Transformation`, `Validation`, `Export`, `Projection`);
- optional pipeline stage / transform id;
- optional projection target;
- optional related model paths.

## Model Path Format

- Paths use canonical slash-separated segments rooted at `/types/...`.
- Path segments escape `~` as `~0` and `/` as `~1`.
- Representative paths:
  - `/types/Customer`
  - `/types/Customer/properties/email`
  - `/types/Customer/keys/Primary`
  - `/types/Order/relationships/Customer`
  - `/types/SalesFact/computedMembers/TotalAmount`

## Built-in Validation Coverage

`ValidateModelTransformation` delegates to `TypeSchemaModelValidator`, which validates:

- duplicate `TypeId`;
- unresolved non-relationship `TypeRef`;
- duplicate property names;
- duplicate key names;
- key references to missing properties;
- relationship references to missing types or properties;
- invalid cardinality bounds;
- invalid string and numeric constraint ranges;
- malformed annotation keys and reserved-namespace casing violations;
- duplicate enum names and duplicate enum values.

## Annotation Merge Policy

M0005 defines the baseline annotation policy as:

- namespaced `AnnotationKey` values remain the canonical storage key;
- unknown namespaces are preserved;
- malformed keys are diagnosable and removed by default during normalization;
- reserved namespaces are normalized to canonical casing;
- duplicate keys use last-wins merge behavior by default;
- duplicate-key merges emit diagnostics, with reserved-namespace conflicts promoted to warnings;
- removal is explicit through normalization or a future dedicated transformation.

## Naming Normalization

`NormalizeNamesTransformation` provides deterministic type names by:

- retaining explicit legal names by default;
- normalizing blank or illegal names from display name or stable `TypeId`;
- replacing illegal characters with `_`;
- preserving stable `TypeId` values;
- diagnosing name collisions and applying deterministic suffixes when needed.

## Diagnostic Code Ranges

- `STM0xxx`: core model validation (`STM0001`-`STM0013` currently used)
- `STM1xxx`: annotation normalization (`STM1001`-`STM1004` currently used)
- `STM2xxx`: transformation pipeline and naming normalization (`STM2001`-`STM2002` currently used)
- `STM3xxx`: reserved for JSON Schema import/export follow-up alignment
- `STM4xxx`: reserved for projection-specific diagnostics

## JSON Schema Composition

M0005 keeps the core pipeline projection-independent.

A short-running JSON Schema composition test proves that the M0004 runtime importer/exporter can be composed with the M0005 pipeline through a test bridge without making `SemanticTypeModel.Core` depend on the JSON Schema package.

## Non-goals

- EF Core projection generation.
- Power BI / TOM projection generation.
- Automatic star-schema inference.
- Parallel transformation execution.
- Remote JSON Schema loading.
- Full `allOf` reduction.
