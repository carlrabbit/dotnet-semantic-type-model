# Projection Capabilities

Use this matrix to decide whether a semantic concept has direct target behavior, target-specific policy, or no
automatic behavior. Always inspect target diagnostics for the actual model.

| Semantic concept | JSON Schema | EF Core | Power BI | System.Text.Json |
|---|---|---|---|---|
| Entity | Object schema + optional `x-stm.role` | Generated entity/table config | Analytical table | Runtime options; automatic semantic Entity polymorphism |
| ValueObject / ownership | Nested schema | Owned structural value uses retained JSON storage policy; not standalone Entity | Target policy/diagnostics | Preserved by contract shape | Nested binding where supported |
| Key | Optional structured `x-stm.keys` metadata | Generated key configuration where supported | Analytical identity/key metadata where supported | No automatic behavior |
| Semantic mutability | Declared values preserved as `x-stm.mutability` | No automatic lifecycle enforcement | No automatic behavior | No automatic serializer enforcement |
| Required / nullable | `required` + null-capable schema | Explicit property requiredness | Metadata/analytical shape | Resolver contract where supported |
| Constraint | JSON Schema keyword when representable | Not general check-constraint generation | Metadata where supported | Usually not runtime validation |
| `RequiredWhen` | Conditional schema when safely representable | No navigation/relationship behavior; unsupported target behavior is not inferred | Target-specific/diagnostic | Not general serializer validation |
| Enum | Schema enum | String provider representation | Analytical categorical representation | Existing serializer contract |
| `Uri` | string + `uri` format | String provider representation | Text-like analytical representation | Existing `Uri` contract |
| Unit | Optional `x-stm.unit` | No automatic conversion | Target-specific/metadata where supported | No automatic behavior |
| Extension data | Additional-properties style behavior | JSON storage | Limited/diagnostic | Existing extension-data behavior |
| Envelope | Target root/payload policy | Only retained supported storage semantics; no automatic navigation graph | Analytical target policy | Contract shape |
| User description | Schema `description` | Not a substitute for technical EF comments | User-facing report metadata | Not a naming source by default |
| Technical description | Optional `x-stm.technicalDescription` | Table/column comments where mapped | Not automatic user-facing description | Technical metadata only |
| UI annotation | Optional `x-stm.ui` pass-through | No automatic behavior | Target-specific display metadata only where explicitly supported | No automatic behavior |

## Important relationship boundary

SemanticTypeModel no longer defines a general canonical relationship primitive. Object-valued properties,
collections, keys, and ownership remain distinct semantic/structural concepts.

Target applications configure relationships through target-native mechanisms. In particular, the current EF
application contract deliberately does not infer arbitrary navigations, `OwnsOne`/`OwnsMany`, many-to-many
relationships, or alternative TPH/TPC policies.

## Important EF boundary

The current EF application contract is generated configuration for semantic Entities. Generated configuration
owns only the STM-selected semantic entity configuration; the application owns its `DbContext`, unrelated
entities, and manual EF composition.

The compile-time semantic manifest is ephemeral internal transport and requires exact STM suite-version
alignment between producer and consumer generators.

## Diagnose capability loss

If a target cannot represent a semantic concept exactly:

1. read the target diagnostic;
2. check the target guide for a supported policy/customization hook;
3. if no target policy exists, keep the semantic meaning in the canonical model and handle the target boundary
   explicitly in application code rather than weakening the canonical semantics.

See the individual guides and [Diagnostics](../diagnostics.md).
