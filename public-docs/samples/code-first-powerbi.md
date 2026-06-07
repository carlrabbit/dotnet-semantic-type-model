# Code-First Power BI Sample

## Scenario Goal

Use an annotated C# domain model with the packaged source generator, derive a Power BI domain semantic model, and export deterministic local analytical metadata.

## Packages Used

- `SemanticTypeModel.Abstractions`
- `SemanticTypeModel.Core`
- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.PowerBI`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/code-first-powerbi/code-first-powerbi.csproj`.

## Expected Output

The sample prints generated model information, Power BI derivation diagnostics, projected table/relationship/measure counts, and local metadata output information.

## Consumer Pattern Demonstrated

A consumer can reuse the generated semantic model for Power BI analytical metadata, add explicit measures or calculated tables, inspect diagnostics, and produce local metadata without calling Power BI service APIs.

## Non-Goals

This sample does not publish to Power BI, require credentials, call cloud services, use XMLA, manage workspaces, schedule refresh, or generate PBIX files.
