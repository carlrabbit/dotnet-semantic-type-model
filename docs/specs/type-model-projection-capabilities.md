# Type Model Projection Capabilities

## Purpose

Define projection support expectations for the hardened canonical model.

## Targets

1. JSON Schema Draft 2020-12
2. JSON editor schema/UI projection
3. EF Core 10 DbModel
4. Power BI TOM model

## Capability Matrix

Legend:

- **Direct**: canonical concept maps directly;
- **Annotation**: concept carried by namespaced annotation;
- **Transform**: requires transformation;
- **Diagnose**: unsupported/lossy, must emit diagnostics;
- **Deferred**: intentionally deferred.

| Concept | JSON Schema 2020-12 | JSON editor | EF Core 10 DbModel | Power BI TOM |
|---|---|---|---|---|
| Scalar types | Direct | Direct | Direct | Direct |
| Object types | Direct | Direct | Direct | Transform |
| Arrays | Direct | Direct | Transform | Transform |
| Dictionaries | Transform | Transform | Diagnose/Transform | Diagnose/Transform |
| Enums | Direct | Direct (with labels) | Direct/Annotation | Direct/Annotation |
| Requiredness | Direct | Direct | Direct | Direct |
| Nullability | Direct | Direct | Direct | Direct |
| Constraints | Direct (baseline) | Annotation | Transform/Annotation | Transform/Annotation |
| `oneOf`/`anyOf` unions | Direct | Direct | Diagnose/Transform | Diagnose/Transform |
| `allOf`/intersection/composition | Direct/Transform | Transform | Transform | Transform |
| Keys | Annotation | Annotation | Direct | Transform |
| Relationships | Annotation/Transform | Annotation | Direct | Direct |
| Computed members | Annotation/Deferred | Annotation | Transform | Direct/Annotation |
| Semantic roles | Annotation | Annotation | Transform/Annotation | Transform/Annotation |
| UI ordering/categories | Annotation (`ui.*`) | Direct (`ui.*`, `jsonEditor.*`) | Annotation | Annotation |
| Projection-specific naming | Annotation | Annotation | Annotation (`efCore.*`) | Annotation (`powerBi.*`, `tom.*`) |

## Diagnostic Expectations

Diagnostic codes should identify unsupported/lossy cases, including:

- union not directly projectable to EF Core;
- dictionary not directly projectable to Power BI;
- unsupported keyword handling during JSON Schema import/export;
- missing keys for relationship inference where required;
- invalid annotation value/type for a projection namespace.
