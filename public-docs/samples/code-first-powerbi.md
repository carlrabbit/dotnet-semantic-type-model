# Code-First Power BI Sample

## Scenario Goal

Use an annotated C# domain model with the packaged source generator and project the generated semantic model into local Power BI metadata.

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

The sample prints the generated root identifier, adapter diagnostic count, projected table count, and projection diagnostic count.

## Consumer Pattern Demonstrated

A consumer can reuse the generated semantic model for Power BI projection metadata without calling Power BI service APIs.

## Non-Goals

This sample does not publish to Power BI, require credentials, call cloud services, or generate a full Tabular Object Model deployment.
