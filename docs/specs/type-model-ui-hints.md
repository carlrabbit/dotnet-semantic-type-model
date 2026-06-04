# Type Model UI Hints Specification

## Purpose

Define canonical UI/editor hint annotations and their JSON Schema projection behavior without coupling the core model to a specific UI runtime.

## Authority

This spec is authoritative for:

- generic `ui.*` hint keys and value expectations;
- downstream `jsonEditor.*` hint keys and compatibility scope;
- UI hint validation and normalization expectations;
- JSON Schema projection/export mapping behavior for UI hints;
- legacy/internal JSON Schema import mapping behavior when retained for compatibility.

## Generic UI Hint Vocabulary (`ui.*`)

- `ui.order`: integer ordering hint.
- `ui.category`: string category/group bucket.
- `ui.group`: string finer-grained grouping hint.
- `ui.title`: UI-specific display title.
- `ui.description`: UI-specific help text.
- `ui.placeholder`: input placeholder text.
- `ui.hidden`: boolean hidden-by-default hint.
- `ui.readOnly`: boolean read-only rendering hint.
- `ui.widget`: widget hint (`text`, `textarea`, `select`, `radio`, `checkbox`, `date`, `datetime`, `number`, `password`, `markdown`, `code`, `uri`).
- `ui.width`: free-form string width/layout hint.
- `ui.defaultExpanded`: boolean object/array expansion hint.
- `ui.enumLabels`: string array matching enum value cardinality.

When both `ui.category` and `ui.group` exist, `ui.group` is treated as the finer-grained grouping inside `ui.category`.

## Downstream JSON Editor Vocabulary (`jsonEditor.*`)

- `jsonEditor.propertyOrder`
- `jsonEditor.format`
- `jsonEditor.options`
- `jsonEditor.watch`
- `jsonEditor.template`

M0006 runtime export supports direct keyword mapping for `propertyOrder`, `options`, and `template` plus namespaced forms (`jsonEditor:*`) when JSON-editor compatibility output is explicitly enabled. Retained legacy/internal import may map those keywords plus `watch`. Other downstream behavior is annotation-preserved and may be diagnosed as deferred. SemanticTypeModel does not provide a standalone JSON editor runtime.

## Display Text Precedence

Exported schema `title` precedence:

1. `ui.title` (when `PreferUiTitleOverDisplayName` is enabled; default true)
2. `schema.title` / `title`
3. omitted

Exported schema `description` precedence:

1. `ui.description`
2. `schema.description` / `description`
3. omitted

## Ordering Rules

Property order precedence:

1. `jsonEditor.propertyOrder`
2. `ui.order`
3. canonical declaration order
4. stable name sort only as deterministic tie-breaker

Conflicting explicit values between `ui.order` and `jsonEditor.propertyOrder` produce a diagnostic.

## Enum Label Rules

`ui.enumLabels` is validated as a string array and must have one label per enum value.

Mismatch emits a diagnostic; labels remain annotation-preserved for downstream handling.

## Widget Inference Rules

Widget inference is optional and enabled by `UiHintOptions.InferWidgetHints`.

Baseline inference:

- enum -> `select`
- boolean -> `checkbox`
- number/integer -> `number`
- string + `format: date` -> `date`
- string + `format: date-time` -> `datetime`
- string + `format: uri` -> `uri`
- string (fallback) -> `text`
- array -> `array`

Inference does not overwrite explicit `ui.widget` unless `UiHintOptions.OverwriteExplicitWidgetHint` is enabled.

## Validation and Normalization

Validation/normalization checks:

- invalid `ui.order` / `jsonEditor.propertyOrder` value;
- invalid boolean value for boolean hints;
- invalid `ui.widget` value in strict mode;
- unknown `ui.*` or `jsonEditor.*` key in strict mode;
- enum label mismatch;
- conflicting explicit order hints.

Normalization behavior:

- order values normalized to invariant integer text;
- boolean values normalized to `true`/`false`;
- string hints normalized as JSON string literals for stable projection;
- optional widget inference;
- explicit hints retained unless overwrite options are enabled.

## JSON Schema Projection and Legacy Import

JSON Schema projection/export behavior:

- default mode emits standard JSON Schema keywords only;
- generic extension mode emits selected `ui:*` extension annotations when configured;
- JSON-editor-compatible mode emits configured downstream keywords;
- unrepresentable UI/downstream hints produce diagnostics and remain non-semantic hints.

Retained legacy/internal import behavior may map:

- standard `title`/`description` to canonical schema annotations;
- `ui:*` to `ui.*`;
- JSON-editor keywords (`propertyOrder`, `options`, `watch`, `template`, `jsonEditor:*`) to `jsonEditor.*`;
- unknown keywords according to the legacy unsupported keyword policy.

Legacy/internal import is not a supported canonical model authoring path.
