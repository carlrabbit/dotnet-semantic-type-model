# Code-First JSON Schema Sample

## Scenario Goal

Use annotated C# domain model files with the packaged source generator, consume the generated provider directly, and export JSON Schema.

## Packages Used

- `SemanticTypeModel.DotNet`
- `SemanticTypeModel.Generators`
- `SemanticTypeModel.JsonSchema`

## Run Command

```sh
./eng/package.sh 0.0.0-samples
./eng/samples.sh
```

The sample project path is `samples/code-first-json-schema/code-first-json-schema.csproj`.

## Expected Output

The sample prints the generated root identifier and shape count, then writes `artifacts/samples/code-first-json-schema/customer.schema.json`.

## Consumer Pattern Demonstrated

A consumer references `SemanticTypeModel.Generators` as a package, lets MSBuild run the generator, calls `AppSemanticTypeModel.Create()`, and passes the generated semantic model to JSON Schema export APIs.

## Non-Goals

This sample does not manually create a Roslyn generator driver, compile C# source strings, use reflection over generated providers, or demonstrate UI-schema compatibility options.
