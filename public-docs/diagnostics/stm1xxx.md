# STM1xxx — Core Transformation Diagnostics

Emitted by `SemanticTypeModel.Core` transformations during the transformation pipeline stage.

Model paths in these diagnostics follow the form `/types/{TypeId}/...` as defined by `ModelPath` in `SemanticTypeModel.Abstractions`.

---

## STM1004 — Semantic role alias invalid

**Severity:** Warning

**Cause:** A `schema.role` annotation contains a value that is not a supported core semantic role.

**Fix:** Use a supported core role such as `Entity` or `ValueObject`, or remove the role annotation.

---

## STM1005 — Semantic role alias conflict

**Severity:** Warning

**Cause:** A `schema.role` annotation conflicts with an already declared canonical role on the same object type.

**Fix:** Keep one authoritative role declaration for the type.

---

## STM1006 — Semantic key on non-entity

**Severity:** Warning

**Cause:** A property declares explicit `schema.key=true` metadata on an object type that is not marked as an entity.

**Fix:** Mark the owning object type as an entity when the key is canonical entity identity, or remove the key metadata.

---

## STM1007 — Multiple primary semantic keys

**Severity:** Warning

**Cause:** A type declares more than one primary semantic key.

**Fix:** Use one primary key definition, or mark additional keys as alternate, natural, surrogate, or external keys.

---

## Related

- [diagnostics.md](../diagnostics.md)
- [stm0xxx.md](stm0xxx.md)
- [stm3xxx.md](stm3xxx.md)
- [stm5xxx.md](stm5xxx.md)
