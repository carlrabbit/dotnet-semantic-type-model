# SemanticTypeModel.PowerBI

`SemanticTypeModel.PowerBI` projects canonical models into Power BI tabular representations.

```sh
dotnet add package SemanticTypeModel.PowerBI --version 0.1.0-alpha
```

This is an initial prerelease. APIs may change before 1.0.

## Projection boundary

The package projects a `TypeSchemaModel` into repository-defined Power BI/tabular metadata. It does not publish datasets, authenticate with Power BI, create PBIX files, or manage Power BI service resources.

## Basic usage

```csharp
PowerBiProjectionModel powerBiModel = semanticModel.ToPowerBiModel(options =>
{
    options.DefaultTableRole = PowerBiTableRole.Dimension;
    options.HideTechnicalKeys = true;
    options.HideForeignKeys = true;
    options.DefaultNumericSummarization = PowerBiSummarization.Sum;
});
```

The output is inspectable without Power BI tooling and includes tables, columns, relationships, measures, and projection diagnostics.

## Projection annotations

Use `PowerBiAnnotationNames` constants for Power BI-specific annotations such as table role, table name, column name, measure expression, format string, summarization, hidden state, display folder, data category, relationship name, and relationship active state.
