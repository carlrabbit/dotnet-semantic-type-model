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

## STM1008 — Envelope payload missing

**Severity:** Warning

**Cause:** An object type marked with `schema.envelope=true` does not declare a `schema.envelope.payload=true` member.

**Fix:** Mark exactly one payload member with `[SemanticEnvelopePayload]` or remove the envelope marker.

---

## STM1009 — Envelope payload duplicate

**Severity:** Warning

**Cause:** An envelope declares multiple payload members without an explicit policy that allows more than one payload.

**Fix:** Keep one payload member, or introduce explicit target policy outside the core semantic annotations.

---

## STM1010 — Envelope payload outside envelope

**Severity:** Warning

**Cause:** A member is marked as an envelope payload but the containing object is not marked as an envelope.

**Fix:** Add `[SemanticEnvelope]` to the containing type or remove `[SemanticEnvelopePayload]` from the member.

---

## STM1011 — Envelope metadata outside envelope

**Severity:** Warning

**Cause:** A member is marked as envelope metadata but the containing object is not marked as an envelope.

**Fix:** Add `[SemanticEnvelope]` to the containing type or remove `[SemanticEnvelopeMetadata]` from the member.

---

## STM1012 — Envelope payload type missing

**Severity:** Warning

**Cause:** An envelope payload member references a type that is not represented in the canonical model.

**Fix:** Include the payload type in extraction or update the payload member type reference.

---

## STM1013 — Envelope projection root ambiguous

**Severity:** Warning

**Cause:** An envelope selects both the envelope and the payload as projection roots without an explicit target policy.

**Fix:** Choose one root for the target projection policy or remove the conflicting root annotations.

---

## Related

- [diagnostics.md](../diagnostics.md)
- [stm0xxx.md](stm0xxx.md)
- [stm3xxx.md](stm3xxx.md)
- [stm5xxx.md](stm5xxx.md)
