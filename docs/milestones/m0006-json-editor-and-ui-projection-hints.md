# M0006 - JSON Editor and UI Projection Hints

## Purpose

Deliver the first UI-oriented projection hint layer over the canonical model while keeping `SemanticTypeModel` UI-runtime-neutral.

## Delivered Runtime Surface

- UI hint options:
  - `UiHintOptions`
  - `JsonSchemaUiExportOptions`
  - `JsonSchemaUiMode`
- JSON Schema export support for configurable UI modes:
  - none (default)
  - generic `ui:*` extensions
  - JSON-editor-compatible keyword emission
- UI hint validation/normalization for `ui.*` and `jsonEditor.*` annotations.
- JSON Schema import mapping for known UI/editor keywords into canonical annotations.

## Supported Generic UI Hints

- `ui.order`
- `ui.category`
- `ui.group`
- `ui.title`
- `ui.description`
- `ui.placeholder`
- `ui.hidden`
- `ui.readOnly`
- `ui.widget`
- `ui.width`
- `ui.defaultExpanded`
- `ui.enumLabels`

## Supported Downstream Hints (`jsonEditor.*`)

- `jsonEditor.propertyOrder`
- `jsonEditor.format`
- `jsonEditor.options`
- `jsonEditor.watch`
- `jsonEditor.template`

M0006 emits only the straightforward downstream subset (`propertyOrder`, `format`, `options`, `watch`, `template`) when JSON-editor-compatible mode is explicitly enabled.

## Display, Ordering, and Enum Behavior

- `title` export precedence: `ui.title` -> `schema.title`/`title` -> omitted.
- `description` export precedence: `ui.description` -> `schema.description`/`description` -> omitted.
- property ordering precedence: `jsonEditor.propertyOrder` -> `ui.order` -> declaration order (+ stable tie-breaks).
- enum label mismatch (`ui.enumLabels`) is diagnosable.

## Import / Export Behavior

- import preserves known UI/editor hints as canonical annotations.
- export keeps Draft 2020-12 validity while optionally adding extension/downstream hints.
- downstream keys are not emitted by default.
- unsupported or disabled-mode UI hints emit structured diagnostics.

## Diagnostics Coverage

M0006 adds diagnostics for:

- invalid UI hint key/value scenarios;
- conflicting `ui.order` and `jsonEditor.propertyOrder`;
- unsupported widget values in strict mode;
- enum labels/value count mismatch;
- UI/downstream hints not representable in selected export mode.

## Tests Added

- fixture coverage for basic form metadata, ordering/grouping, enum labels, compatibility mode export, and import preservation.
- short-running tests only.

## Non-goals

- browser runtime integration.
- JavaScript/TypeScript UI package.
- Playwright/browser automation.
- full dynamic watch/template execution semantics.
- EF Core, Power BI/TOM, and OpenAPI projection implementations.
