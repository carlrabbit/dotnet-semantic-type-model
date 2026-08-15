# Type Model UI Annotations

## Status

Normative living specification.

## Contract

Canonical annotations whose keys begin with `ui.` are open, namespaced UI metadata. The model preserves their JSON-compatible values and orders keys deterministically; it does not impose a closed widget vocabulary or silently reinterpret arbitrary keys.

Dedicated authoring attributes retain these mappings:

- `[SemanticDisplayName]` → `ui.title`;
- `[SemanticCategory]` → `ui.category`;
- `[SemanticOrder]` → `ui.order`.

Other keys such as `ui.widget` and `ui.customThing` are permitted without registration. Remaining dots are part of the key and do not imply nested objects.

JSON Schema exports `ui.*` annotations beneath `x-stm.ui` when semantic annotations are enabled, stripping only the leading `ui.` prefix. Values that cannot be represented as JSON produce `JSONSCHEMA_UI_VALUE_NOT_JSON_COMPATIBLE` rather than lossy stringification.

JSON Editor translation, widget inference, strict known-key validation, and editor runtime policy are not supported capabilities.
