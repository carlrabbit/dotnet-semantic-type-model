# STM5xxx — .NET Type Extraction and Source Generator Diagnostics

Emitted by `RoslynDotNetTypeExtractor` in `SemanticTypeModel.DotNet` and `SemanticTypeModelSourceGenerator` in `SemanticTypeModel.Generators` during compile-time extraction and source generation.

STM5001–STM5017, STM5020–STM5025 are emitted by `RoslynDotNetTypeExtractor` at extraction time. STM5008, STM5018, and STM5019 are also emitted directly by `SemanticTypeModelSourceGenerator` when build-property configuration is invalid.

All STM5xxx diagnostics have `Warning` severity unless stated otherwise.

---

## STM5001 — Type resolution failed

**Severity:** Warning

**Message:** `Type '{typeName}' could not be resolved or processed during extraction.`

**Cause:** The extractor encountered a type symbol that could not be mapped to a semantic type. Common causes include partially-compiled code, unresolved generic parameters, or unsupported CLR types (pointers, ref structs).

**Fix:** Ensure the type is a valid, non-abstract class or record that .NET extraction supports. Check for compilation errors in your project.

---

## STM5002 — Member resolution failed

**Severity:** Warning

**Message:** `Member '{memberName}' on '{typeName}' could not be resolved or processed during extraction.`

**Cause:** A property or field on a semantic type could not be mapped. The member is excluded from the extracted model.

**Fix:** Ensure the member has a supported type and is a public property with a getter.

---

## STM5003 — Generic argument resolution failed

**Severity:** Warning

**Message:** `Generic type argument on '{typeName}' could not be resolved.`

**Cause:** A generic type argument (e.g., `T` in `List<T>`) could not be resolved to a concrete type during extraction.

**Fix:** Ensure the type is used with concrete type arguments, not unbound generics.

---

## STM5004 — Type argument binding skipped

**Severity:** Warning

**Message:** `Type argument binding on '{typeName}' was skipped or partially resolved.`

**Cause:** The extractor could not fully bind type arguments for a generic type. The type is included with reduced fidelity.

**Fix:** Check for unresolved generic constraints or unbound type parameters on the affected type.

---

## STM5006 — Annotation invalid

**Severity:** Warning

**Message:** `Annotation key or value on '{target}' was invalid and was ignored.`

**Cause:** An `[SemanticAnnotation]` attribute has a key that fails format validation, or a value that cannot be serialized.

**Fix:** Use lowercase dot-separated annotation keys. Ensure values are non-null strings.

---

## STM5007 — Type included without attribute

**Severity:** Warning

**Message:** `Type '{typeName}' was included via namespace discovery but has no [SemanticType] attribute.`

**Cause:** When using namespace-based discovery mode, the extractor includes types in the target namespace even if they lack `[SemanticType]`. This diagnostic marks such types.

**Fix:** Add `[SemanticType]` to the type to opt in explicitly, or use `AttributeOnly` discovery mode to exclude unannotated types.

---

## STM5008 — Unsupported discovery mode

**Severity:** Warning

**Message:** `The discovery mode value '{value}' specified in build properties is not supported. Using default discovery mode.`

**Cause:** The MSBuild property `SemanticTypeModelDiscoveryMode` is set to an unrecognized value. The generator falls back to the default mode.

**Invalid example:**
```xml
<SemanticTypeModelDiscoveryMode>InvalidMode</SemanticTypeModelDiscoveryMode>
```

**Fix:** Use a supported value: `AttributeOnly` or `NamespaceAndAttribute`.

```xml
<SemanticTypeModelDiscoveryMode>AttributeOnly</SemanticTypeModelDiscoveryMode>
```

---

## STM5009 — Root type not found

**Severity:** Warning

**Message:** `The root type could not be determined from the compilation.`

**Cause:** The extractor could not identify a root type for the model. This may occur when no types match the discovery criteria.

**Fix:** Ensure at least one type has `[SemanticType]` (or matches namespace criteria) in the compilation.

---

## STM5010 — Type excluded

**Severity:** Warning

**Message:** `Type '{typeName}' was excluded from extraction because it did not satisfy discovery criteria.`

**Cause:** A type was found in the compilation but excluded because it does not have `[SemanticType]` and namespace discovery is not enabled, or it was explicitly filtered out.

**Fix:** Add `[SemanticType]` to the type, or adjust the discovery mode.

---

## STM5011 — Property type mapping failed

**Severity:** Warning

**Message:** `Property type on '{memberPath}' could not be mapped to a known scalar or shape kind.`

**Cause:** The property's CLR type has no known mapping in the SemanticTypeModel type system. The property is excluded from the extracted model.

**Fix:** Use a supported property type (primitives, strings, `DateTimeOffset`, `Guid`, known collection types, or other `[SemanticType]`-annotated types).

---

## STM5012 — Type skipped (abstract or empty)

**Severity:** Warning

**Message:** `Type '{typeName}' was skipped because it is abstract or has no accessible members.`

**Cause:** An abstract class or a type with no public properties was encountered. These are not projectable.

**Fix:** Concrete, non-abstract types with at least one public property are supported.

---

## STM5013 — Key definition invalid

**Severity:** Warning

**Message:** `Key definition on type '{typeName}' was invalid or could not be applied.`

**Cause:** A `[SemanticKey]` or related attribute has an invalid configuration (e.g., references a property name that does not exist).

**Fix:** Ensure the key attribute references existing property names.

---

## STM5014 — Relationship definition invalid

**Severity:** Warning

**Message:** `Relationship definition on type '{typeName}' was invalid or could not be applied.`

**Cause:** A relationship attribute has an invalid configuration (missing target type, empty name, etc.).

**Fix:** Ensure relationship attributes have valid target types and non-empty names.

---

## STM5015 — Relationship endpoint unresolved

**Severity:** Warning

**Message:** `Relationship endpoint reference on '{typeName}' could not be resolved.`

**Cause:** A relationship attribute references a type or property that cannot be found in the current compilation.

**Fix:** Ensure the referenced type is included in the compilation and is a recognized semantic type.

---

## STM5016 — Enum member unsupported value

**Severity:** Warning

**Message:** `Enum member '{memberName}' on '{typeName}' has an unsupported backing value type.`

**Cause:** The enum member's underlying value type is not `int`, `long`, or `string`.

**Fix:** Use `int`- or `string`-backed enum types with `SemanticTypeModel`.

---

## STM5017 — Attribute argument invalid

**Severity:** Warning

**Message:** `Attribute argument on '{target}' was invalid or out of range.`

**Cause:** A SemanticTypeModel attribute (e.g., `[SemanticStringConstraint]`, `[SemanticNumericConstraint]`) has an argument value that fails validation (negative length, inverted range, etc.).

**Fix:** Check attribute argument values against the documented valid ranges.

---

## STM5018 — Unsupported naming policy

**Severity:** Warning

**Message:** `The naming policy value '{value}' specified in build properties is not supported. Using default naming policy.`

**Cause:** The MSBuild property `SemanticTypeModelNamingPolicy` is set to an unrecognized value. The generator falls back to the default naming policy.

**Invalid example:**
```xml
<SemanticTypeModelNamingPolicy>InvalidPolicy</SemanticTypeModelNamingPolicy>
```

**Fix:** Use a supported value: `Default` or `CamelCase`.

```xml
<SemanticTypeModelNamingPolicy>CamelCase</SemanticTypeModelNamingPolicy>
```

---

## STM5019 — Generated provider name collision

**Severity:** Warning

**Message:** `The generated provider type name '{name}' collides with an existing type in the compilation.`

**Cause:** The source generator's output type name (by default `AppSemanticTypeModel` in the `SemanticTypeModel.Generated` namespace) conflicts with a type already present in the compilation.

**Fix:** Rename the conflicting type, or configure `SemanticTypeModelProviderName` in your project to use a different generated type name.

---

## STM5020 — XML documentation missing

**Severity:** Warning

**Message:** `Type or member '{target}' is missing a required XML documentation summary.`

**Cause:** The extractor is configured to require XML documentation, but the type or member has no `<summary>` element.

**Fix:** Add an XML doc summary, or disable the XML doc requirement in extraction options.

---

## STM5021 — Constraint value invalid

**Severity:** Warning

**Message:** `Constraint value on '{target}' was invalid or inconsistent.`

**Cause:** A constraint attribute has a value that fails validation (e.g., negative minimum, pattern that cannot be compiled).

**Fix:** Review the constraint attribute's arguments and correct the invalid value.

---

## STM5022 — Constraint range invalid

**Severity:** Warning

**Message:** `Constraint range on '{target}' has minimum exceeding maximum.`

**Cause:** A range constraint (string length, numeric range) has `Minimum > Maximum`.

**Fix:** Ensure `Minimum <= Maximum` in the constraint attribute.

---

## STM5023 — Semantic name duplicate

**Severity:** Warning

**Message:** `Semantic name '{name}' on '{target}' duplicates another type or member name.`

**Cause:** Two types or members resolve to the same semantic name after applying the naming policy.

**Fix:** Assign an explicit `[SemanticName]` to one of the conflicting types or members to disambiguate.

---

## STM5024 — Annotation conflict

**Severity:** Warning

**Message:** `Annotation '{key}' on '{target}' conflicts with another annotation on the same target.`

**Cause:** Two annotation attributes on the same type or member set the same key to different values.

**Fix:** Remove the duplicate annotation, or ensure both annotations agree on the value.

---

## STM5025 — Member shape unsupported

**Severity:** Warning

**Message:** `Member '{memberName}' on '{typeName}' has a shape that is not supported by the current extraction path.`

**Cause:** The member's structure (e.g., an indexed property, a ref-returning getter, a pointer type) is not supported by the extractor.

**Fix:** Use supported member shapes: public get-only or init-only properties with supported types.

---

## Related

- [diagnostics.md](../diagnostics.md)
- [stm0xxx.md](stm0xxx.md)
- [stm3xxx.md](stm3xxx.md)
