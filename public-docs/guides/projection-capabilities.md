# Projection Capabilities

Use this matrix to decide whether a semantic concept has direct target behavior, target-specific policy, or no
automatic behavior. Always inspect target diagnostics for the actual model.

| Semantic concept | JSON Schema | EF Core | Power BI | System.Text.Json | Configuration / Options |
|---|---|---|---|---|---|
| Entity | Object schema | Generated entity/table config | Analytical table | Existing JSON contract metadata | Not an options root by default |
| ValueObject / ownership | Nested schema | Owned structural value uses retained JSON storage policy; not standalone Entity | Target policy/diagnostics | Preserved by contract shape | Nested binding where supported |
| Required / nullable | `required` + null-capable schema | Explicit property requiredness | Metadata/analytical shape | Resolver contract where supported | Options validation/binding |
| Constraint | JSON Schema keyword when representable | Not general check-constraint generation | Metadata where supported | Usually not runtime validation | Validation when representable |
| `RequiredWhen` | Conditional schema when safely representable | No relationship/navigation behavior; unsupported target behavior is not inferred | Target-specific/diagnostic | Not general serializer validation | Conditional Options validation |
| Enum | Schema enum | String provider representation | Analytical categorical representation | Existing serializer contract | Binding/validation |
| `Uri` | string + `uri` format | String provider representation | Text-like analytical representation | Existing `Uri` contract | Binding where supported |
| Extension data | Additional-properties style behavior | JSON storage | Limited/diagnostic | Existing extension-data behavior | Target-specific/limited |
| Envelope | Target root/payload policy | Only retained supported storage semantics; no automatic navigation graph | Analytical target policy | Contract shape | Not generally applicable |
| User description | Schema description | Not a substitute for technical EF comments | User-facing report metadata | Not a naming source by default | Inspection/docs metadata |
| Technical description | Optional target-specific extension | Table/column comments where mapped | Not automatic user-facing description | Technical metadata only | Inspection/docs metadata |

## Important EF boundary

The current EF application contract is generated configuration for semantic Entities. It deliberately does not
infer arbitrary navigations, `OwnsOne`/`OwnsMany`, many-to-many relationships, or alternative TPH/TPC policies.
Owned structural values and extension data use the retained JSON storage policy.

## Diagnose capability loss

If a target cannot represent a semantic concept exactly:

1. read the target diagnostic;
2. check the target guide for a supported policy/customization hook;
3. if no target policy exists, keep the semantic meaning in the canonical model and handle the target boundary
   explicitly in application code rather than weakening the canonical semantics.

See the individual guides and [Diagnostics](../diagnostics.md).
