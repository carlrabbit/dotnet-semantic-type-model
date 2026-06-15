# SemanticTypeModel.JsonSchema

`SemanticTypeModel.JsonSchema` derives JSON Schema semantic models from code-first canonical SemanticTypeModel metadata and exports JSON Schema Draft 2020-12 documents.

```sh
dotnet add package SemanticTypeModel.JsonSchema --version 2.1.0
```

## What it does

- Derives a JSON Schema domain semantic model from the canonical semantic model.
- Exports deterministic JSON Schema Draft 2020-12 documents.
- Supports envelope projection policies, including envelope-root and payload-root views.
- Supports ownership, versioning, temporal-validity, lifecycle-state, and extension-data semantics through safe schema defaults and explicit projection policies.
- Provides JSON Editor compatibility as an export mode.

Annotated .NET code is the supported canonical authoring source. JSON Schema import is not the supported canonical model creation path for new consumers.

More details: `public-docs/guides/json-schema.md`.
