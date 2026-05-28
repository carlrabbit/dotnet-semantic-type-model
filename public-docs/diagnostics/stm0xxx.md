# STM0xxx — Model Validation Diagnostics

Emitted by `TypeSchemaModelValidator` in `SemanticTypeModel.Core` during the validation pipeline stage. All STM0xxx diagnostics report structural problems found in a `TypeSchemaModel` after construction.

Model paths in these diagnostics follow the form `/types/{TypeId}/...` as defined by `ModelPath` in `SemanticTypeModel.Abstractions`.

---

## STM0001 — Duplicate type ID

**Severity:** Error

**Message:** `Duplicate TypeId '{id}' found in the model.`

**Cause:** Two or more types in the model share the same `TypeId`. Type IDs must be unique across the entire model.

**Fix:** Assign a unique ID to each type. If using `.NET` extraction, check for naming collisions between types that resolve to the same semantic name.

---

## STM0002 — Unresolved type reference

**Severity:** Error

**Message:** `TypeRef '{ref}' on '{path}' could not be resolved.`

**Cause:** A property, key, or relationship references a type ID that does not exist in the model.

**Fix:** Ensure all referenced types are included in the model, or remove the reference.

---

## STM0003 — Duplicate property name

**Severity:** Error

**Message:** `Duplicate property name '{name}' on type '{typeId}'.`

**Cause:** Two or more properties within the same object type share the same name (case-insensitive comparison).

**Fix:** Rename one of the duplicate properties, or remove the duplicate.

---

## STM0004 — Duplicate key name

**Severity:** Error

**Message:** `Duplicate key name '{name}' on type '{typeId}'.`

**Cause:** Two or more keys within the same object type share the same name (case-insensitive).

**Fix:** Rename one of the duplicate keys.

---

## STM0005 — Key property reference missing

**Severity:** Error

**Message:** `Key '{keyName}' on type '{typeId}' references property '{propertyName}' which does not exist.`

**Cause:** A key definition refers to a property name that is not defined on the owning type.

**Fix:** Correct the key's property reference, or add the missing property.

---

## STM0006 — Relationship type missing

**Severity:** Error

**Message:** `Relationship endpoint on type '{typeId}' references type '{refId}' which is missing or is not an object type.`

**Cause:** A relationship definition references a type that does not exist in the model or is not an object type.

**Fix:** Ensure the target type is present in the model and is an object type.

---

## STM0007 — Relationship property reference missing

**Severity:** Error

**Message:** `Relationship on type '{typeId}' references property '{propertyName}' which does not exist.`

**Cause:** A relationship definition references a property that is not defined on its owning type.

**Fix:** Correct the property reference in the relationship definition.

---

## STM0008 — Invalid cardinality bounds

**Severity:** Error

**Message:** `Cardinality bounds on '{path}' are invalid.`

**Cause:** A cardinality constraint has a negative bound, or its minimum exceeds its maximum.

**Fix:** Set non-negative bounds where minimum ≤ maximum.

---

## STM0009 — Invalid string constraint bounds

**Severity:** Error

**Message:** `String constraint bounds on '{path}' are invalid.`

**Cause:** A string length constraint has a negative bound, or its minimum exceeds its maximum.

**Fix:** Set non-negative bounds where minimum ≤ maximum.

---

## STM0010 — Invalid numeric constraint bounds

**Severity:** Error

**Message:** `Numeric constraint bounds on '{path}' are invalid.`

**Cause:** A numeric range constraint has its minimum value exceeding its maximum value.

**Fix:** Ensure the minimum is less than or equal to the maximum.

---

## STM0011 — Malformed annotation key

**Severity:** Warning

**Message:** `Annotation key '{key}' on '{path}' is malformed or uses non-canonical reserved-namespace casing.`

**Cause:** An annotation key uses an invalid format or a reserved namespace (`stm:`) with incorrect casing.

**Fix:** Use lowercase-dot-separated annotation keys. Reserved keys must match their canonical lowercase form.

---

## STM0012 — Duplicate enum value name

**Severity:** Error

**Message:** `Enum type '{typeId}' contains duplicate value name '{name}'.`

**Cause:** Two enum members within the same type share the same name (case-insensitive).

**Fix:** Rename one of the duplicate enum members.

---

## STM0013 — Duplicate enum value payload

**Severity:** Error

**Message:** `Enum type '{typeId}' contains duplicate value payload '{payload}'.`

**Cause:** Two enum members within the same type share the same backing value.

**Fix:** Assign a unique backing value to each enum member.

---

## Related

- [diagnostics.md](../diagnostics.md)
- [stm3xxx.md](stm3xxx.md)
- [stm5xxx.md](stm5xxx.md)
