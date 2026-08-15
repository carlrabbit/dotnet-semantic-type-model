# Troubleshooting

Use this page when you know the symptom but not which API or diagnostic reference to search.

## The generated model class cannot be found

Check all of these:

1. `SemanticTypeModel.Generators` is referenced by the model project.
2. At least one configured semantic root is discovered.
3. The generated namespace/provider name matches your `using` and call site.
4. The build has completed successfully; generator errors can prevent provider emission.

Defaults are:

```text
namespace: SemanticTypeModel.Generated
provider:  AppSemanticTypeModel
```

See [Configuration](configuration.md).

## The generated model is in the wrong namespace

Set either:

```csharp
[assembly: SemanticTypeModelGeneratorOptions(
    generatedNamespace: "MyApplication.Semantics")]
```

or:

```xml
<SemanticTypeModelGeneratedNamespace>MyApplication.Semantics</SemanticTypeModelGeneratedNamespace>
```

Rebuild the model project. To confirm the result, enable `EmitCompilerGeneratedFiles` as described in the
configuration reference.

## A type is missing from the semantic model

Review:

- discovery mode;
- explicit `[SemanticType]` roots;
- included/excluded namespace filters;
- `IncludeInternalTypes` for internal types;
- reachability when using `ReachableFromRoots`.

Relevant diagnostics include `STM5007`-`STM5012`.

## A member is missing

Review member accessibility, `SemanticIgnore`, internal-member inclusion, and whether the CLR shape is supported.
`STM5002`, `STM5011`, and `STM5025` are common signals.

## The provider name collides with my code

`STM5019` means the generated provider's fully qualified type name already exists. Change the generated
namespace or provider name in [Configuration](configuration.md).

## I get an STM5xxx diagnostic

Use [Diagnostics](diagnostics.md) and the STM5xxx range page. The important families are:

- STM5001-STM5025: .NET extraction/generator configuration and shape problems;
- STM5026-STM5036: typed conditional-literal validation;
- STM5037-STM5047: generated EF manifest/configuration failures.

## EF Core generated no configuration for my type

The EF generator emits configurations for semantic **Entities**, not ValueKinds, enums, DTOs, or arbitrary
reachable types.

Check:

- the model project generated a semantic manifest;
- the persistence project references `SemanticTypeModel.EFCore.Generators`;
- the persistence assembly selects the model with `GenerateSemanticEfModel`;
- the type has the intended semantic role;
- the selected models do not claim the same CLR Entity.

See [EF Core](guides/ef-core.md).

## EF Core reports STM5037

The persistence project selected an assembly without a semantic manifest. Ensure the selected model project
runs `SemanticTypeModel.Generators` and rebuild it before the persistence project.

## EF Core reports STM5039

The model manifest schema version and EF generator do not agree. Align all `SemanticTypeModel.*` packages to
the same exact version, clean/rebuild, and retry.

## EF Core reports STM5041

Two selected semantic models claim the same CLR Entity. Select one owning model for that CLR Entity or change
the semantic model boundary.

## EF Core reports STM5046

A semantic Entity/member cannot be lowered to the supported generated EF mapping contract. Review the member
shape and the supported mapping table in [EF Core](guides/ef-core.md); do not expect navigation inference,
`OwnsOne`, or `OwnsMany` to repair an unsupported semantic shape.

## JSON Schema output is missing or changes a member

Check projection diagnostics first. Then review semantic requiredness/nullability, target naming/UI options,
extension-data shape, and unsupported projection semantics in [JSON Schema](guides/json-schema.md).

## System.Text.Json names are not what I expected

Semantic names and JSON contract names are separate by default. Check `PropertyNameSource` in the
System.Text.Json integration and whether `UseJsonPropertyNameAsSemanticName` was enabled during extraction.
See [System.Text.Json](guides/system-text-json.md).

## Configuration validation fails at startup

Startup validation is normal Options validation after binding. Check the selected section, required-section
policy, DataAnnotations, and `RequiredWhen` rules. See
[Configuration / Options](guides/configuration-options.md).

## Package behavior looks inconsistent across projects

Check package versions first. Every `SemanticTypeModel.*` package in the application must use the same exact
version. Mixed suite versions are unsupported.

## I need to see what the generator emitted

Add:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Rebuild and inspect the generated source. Do not commit generated files.
