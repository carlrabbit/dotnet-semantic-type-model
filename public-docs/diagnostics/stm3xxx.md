# STM3xxx — JSON Schema Runtime Projection Diagnostics

Emitted by `JsonSchemaRuntimeProjection` in `SemanticTypeModel.JsonSchema` during the projection pipeline stage. These diagnostics indicate approximations or losses of fidelity when projecting a `TypeSchemaModel` to a JSON Schema document via the runtime projection adapter.

All STM3xxx diagnostics have `Warning` severity. The projection continues and produces output; the diagnostic indicates that the output may not fully represent the input model's semantics.

---

## STM3201 — Root ID fallback applied

**Severity:** Warning

**Message:** `Runtime model id '{modelId}' did not match a type id. The JSON Schema runtime projection used '{fallback}' as the root type.`

**Cause:** The model's root `Id` did not match any type ID in the model. The projection selected a fallback root type instead.

**Fix:** Ensure the model's root ID matches the ID of the intended root type, or set the root explicitly before projecting.

---

## STM3202 — Semantic members skipped

**Severity:** Warning

**Message:** `Object type '{typeId}' contains semantic members that the current JSON Schema runtime projection does not project directly.`

**Cause:** The type contains keys, relationships, computed members, or allOf composition that the runtime adapter does not have a JSON Schema equivalent for. These are omitted from the output.

**Fix:** Use the full JSON Schema export path (`JsonSchemaExporter`) rather than the runtime projection adapter if you need these members in the output.

---

## STM3203 — Dictionary key metadata lost

**Severity:** Warning

**Message:** `Dictionary type '{typeId}' projects to a JSON object with string keys. Non-string key metadata is not represented.`

**Cause:** JSON Schema represents dictionaries as objects with string keys. If the model's dictionary has non-string key metadata (type constraints, annotations), that information is not preserved in the output.

**Fix:** This is a JSON Schema limitation. Accept the approximation, or document the key type constraint separately.

---

## STM3204 — Union type approximated as oneOf

**Severity:** Warning

**Message:** `Union type '{typeId}' includes discriminator or anyOf semantics that the current JSON Schema runtime projection approximates as oneOf.`

**Cause:** The model's union uses discriminator or anyOf-style semantics that do not have a direct JSON Schema Draft-07 equivalent. The projection uses `oneOf` as an approximation.

**Fix:** If you need accurate discriminator output, use the full JSON Schema export path which supports discriminator annotations.

---

## STM3205 — Intersection type approximated as union

**Severity:** Warning

**Message:** `Intersection type '{typeId}' is approximated as a union because the current legacy JSON Schema exporter has no allOf-aware runtime adapter.`

**Cause:** The runtime adapter does not support `allOf` composition. Intersection types are projected as if they were unions.

**Fix:** Use the full JSON Schema export path for accurate allOf output.

---

## STM3206 — Unsupported type kind

**Severity:** Warning

**Message:** `Type '{typeId}' of kind '{kind}' is not supported by the current JSON Schema runtime projection adapter.`

**Cause:** The type's kind (e.g., a future or extension kind) is not handled by the current runtime projection. It is approximated as `string`.

**Fix:** Open an issue if you believe this type kind should be supported.

---

## STM3207 — Non-string enum value serialized as string

**Severity:** Warning

**Message:** `Enum value '{valueName}' on type '{typeId}' is not a string value. The JSON Schema runtime projection serialized it using its string representation.`

**Cause:** The JSON Schema runtime adapter only supports string-valued enum members. Integer or other non-string enum payloads are converted to their string representation.

**Fix:** Use string enum payloads, or use the full JSON Schema export path which preserves numeric enum values.

---

## Related

- [diagnostics.md](../diagnostics.md)
- [stm0xxx.md](stm0xxx.md)
- [stm5xxx.md](stm5xxx.md)
