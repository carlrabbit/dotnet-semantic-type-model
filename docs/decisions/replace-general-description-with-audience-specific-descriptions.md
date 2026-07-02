# Decision: Replace General Description with Audience-Specific Descriptions

## Status

Accepted for M0047.

## Context

One undifferentiated description cannot correctly serve technical database documentation, developer documentation, user-facing JSON Schema editing help, analytical metadata, and configuration documentation at the same time.

The existing XML-summary convention also routes technical source documentation directly toward `schema.description`, which conflates authoring source, semantic meaning, and target representation.

The project does not require backward compatibility for the old description attribute or optional XML-summary inclusion behavior.

## Decision

The canonical model will expose exactly:

```text
TechnicalDescription
UserDescription
```

`SemanticDescriptionAttribute` and the general canonical `Description` are removed.

.NET authoring uses:

```text
SemanticTechnicalDescriptionAttribute
SemanticUserDescriptionAttribute
```

XML `<summary>` is always considered as the fallback source for technical description. An explicit technical-description attribute overrides XML summary.

Projection packages select and map descriptions according to audience:

```text
JSON Schema and Power BI -> user description by default
EF Core table/column comments -> technical description by default
inspection -> both
```

User-facing targets do not silently fall back to technical descriptions.

## Rationale

- Technical and user-facing text are simultaneously valid semantic information.
- Audience is semantic intent, not merely target formatting.
- Projections need explicit mapping rather than a global description guess.
- Removing the old API avoids preserving ambiguity.
- Always considering XML summary creates one predictable technical fallback.
- A semantic `RequireTechnicalDescription` policy is preferable to requiring one authoring mechanism.

## Consequences

- The change is intentionally breaking and belongs to the 2.4.0 line.
- Existing users must classify or split old description text manually.
- Canonical contracts, extraction, generator output, queries, inspection, projections, samples, and docs all change.
- EF Core gains meaningful table/column comment projection.
- JSON Schema editing examples can remain user-focused while technical descriptions remain available separately.

## Rejected Alternatives

### Keep `SemanticDescription` as a compatibility alias

Rejected because it preserves the ambiguity and forces an unsafe audience choice.

### Add user and technical descriptions while retaining general description

Rejected because target fallback becomes unclear and three-way precedence recreates the original problem.

### Keep XML summary optional

Rejected because technical fallback behavior should be deterministic and does not need backward compatibility.

### Map custom XML tags directly to target properties

Rejected for this milestone because the fundamental requirement is first-class audience-specific semantics, not arbitrary target injection.
