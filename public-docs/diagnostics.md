# Diagnostics

SemanticTypeModel uses diagnostics to make unsupported, ambiguous, or lossy behavior visible instead of
silently guessing.

## Where diagnostics appear

- Source-generator diagnostics appear at compile time (`STM5xxx`, plus integration-specific codes where
  applicable).
- Runtime transformation/projection APIs return diagnostic collections with their results.
- Warnings can accompany usable output; they may still indicate lost target semantics.

## Diagnostic ranges

| Range | Area | Reference |
|---|---|---|
| `STM0xxx` | Canonical model validation | [STM0xxx](diagnostics/stm0xxx.md) |
| `STM1xxx` | Core transformations/semantic normalization | [STM1xxx](diagnostics/stm1xxx.md) |
| `STM3xxx` | JSON Schema runtime projection | [STM3xxx](diagnostics/stm3xxx.md) |
| `STM5xxx` | .NET extraction and source generators, including generated EF application | [STM5xxx](diagnostics/stm5xxx.md) |
| `TESTDATA_*` | Deterministic semantic test-data generation | [TestData](guides/test-data.md) |

Diagnostic message text is not an API contract. Prefer IDs/categories and documented behavior.

## Common generator diagnostics

| Diagnostic | What happened | Typical fix |
|---|---|---|
| `STM5008` | Unsupported discovery-mode configuration | Use a supported `DotNetTypeDiscoveryMode`; see [Configuration](configuration.md). |
| `STM5018` | Unsupported naming-policy configuration | Use a supported `DotNetNamingPolicy`. |
| `STM5019` | Generated provider name collides with an existing CLR type | Change generated namespace/provider name. |
| `STM5020` | Required technical description is missing | Add technical description/XML summary or change the requirement. |
| `STM5025` | A CLR member shape cannot be extracted safely | Change/annotate the member to a supported shape. |
| `STM5026`-`STM5036` | Conditional/typed literal metadata is invalid | Fix the source property, literal value/type, nullability, or enum member. |
| `STM5049` | Display Identity order is negative or ambiguous | Use non-negative, unique orders; the invalid group is omitted. |
| `STM5050` | Access Path name/order/membership is invalid or ambiguous | Use a valid name and unique non-negative orders for each path. |
| `STM5052` | Logical Type name, target, or model-wide scalar identity is invalid | Use a valid name on an ordinary scalar property and keep same-name mappings on one scalar type throughout the model. |

## System.Text.Json diagnostics

| Diagnostic | What happened | Typical fix |
|---|---|---|
| `STJ009` | Automatic semantic Entity polymorphism found an invalid or ambiguous CLR/model hierarchy. | Fix the modeled inheritance/ownership or provide a valid explicit application contract. |

## Generated EF diagnostics

| Diagnostic | What happened | Fix |
|---|---|---|
| `STM5037` | Selected assembly has no semantic manifest | Run/reference `SemanticTypeModel.Generators` in the model project and rebuild. |
| `STM5038` | Selected assembly exposes an ambiguous manifest | Remove duplicate/ambiguous manifest production. |
| `STM5039` | Manifest schema version is unsupported | Align all `SemanticTypeModel.*` package versions exactly. |
| `STM5040` | Selected manifest is invalid | Rebuild the model project and inspect model/generator diagnostics. |
| `STM5041` | Multiple selected models own the same CLR Entity | Select a single owning semantic model for that CLR Entity. |
| `STM5042` | Generated configuration type names collide | Change model/type naming so generated configuration names are unique. |
| `STM5043` | Generated registration names collide | Change selected model/provider naming so registration names are unique. |
| `STM5047` | Manifest producer and EF generator versions differ | Align every SemanticTypeModel package to the same exact version. |
| `STM5044` | Manifest CLR type cannot be resolved | Rebuild references; verify the selected assembly/type still matches the manifest. |
| `STM5045` | Manifest CLR member cannot be resolved | Rebuild the model project; verify the member was not renamed/removed. |
| `STM5046` | EF configuration projection failed | Use a supported semantic Entity/member/storage shape; see [EF Core](guides/ef-core.md). |

## Suppression

Compile-time diagnostics can use normal compiler suppression mechanisms such as `#pragma warning disable` or
`<NoWarn>`. Suppress only after confirming that the resulting behavior is acceptable.

Runtime diagnostics are returned as data; consumers may filter them, but errors should generally prevent use
of the affected projection output.

## Troubleshooting

If you have a symptom rather than a code, start with [Troubleshooting](troubleshooting.md).
