# Specifications

## Purpose

Specs define behavioral truth.

Specs are authoritative for:
- behavior;
- invariants;
- contracts;
- inputs and outputs;
- failure semantics;
- validation expectations.

## Rules

- Specs must use canonical terminology.
- Specs must define invariants explicitly.
- Specs must avoid implementation plans.
- Specs should exist before implementation whenever practical.

## Available Specs

| Spec | Purpose |
|---|---|
| specs/type-schema-model.md | Canonical semantic type model contracts, invariants, and traversal behavior |
| specs/json-schema-adapter.md | Supported JSON Schema import and export baseline behavior |
| specs/type-model-core.md | Hardened canonical type model layers, contracts, invariants, and diagnostics model |
| specs/type-model-annotations.md | Annotation namespace policy, preservation rules, and projection separation |
| specs/type-model-json-schema-mapping.md | JSON Schema Draft 2020-12 mapping baseline for the hardened canonical model |
| specs/type-model-ui-hints.md | Generic UI hint vocabulary, normalization, and JSON-editor compatibility mapping |
| specs/type-model-projection-capabilities.md | Projection capability matrix and diagnostic expectations across targets |
| specs/type-model-powerbi-tom-projection.md | Deterministic Power BI / TOM-like projection behavior over the hardened canonical model |
