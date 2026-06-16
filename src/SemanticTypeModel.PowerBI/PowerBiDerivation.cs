using SemanticTypeModel.Core.Transformation;
using Model = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.PowerBI;

/// <summary>Power BI domain derivation entry points.</summary>
public static class PowerBiDerivationExtensions
{
    /// <summary>Derives a Power BI domain semantic model from a code-first canonical semantic model.</summary>
    public static SemanticDerivationResult<PowerBiSemanticModel> DerivePowerBiModel(
        this Model.TypeSchemaModel model,
        Action<PowerBiDerivationOptions>? configure = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        PowerBiDerivationOptions options = new();
        if (configure is null)
        {
            _ = options.UseDefaultTransformations();
        }
        else
        {
            configure(options);
        }

        SemanticModelTransformationResult transformed = options.Transformations.Run(model, options.PipelineOptions, cancellationToken);
        var projectionContext = new Model.SchemaProjectionContext { Diagnostics = [.. transformed.Diagnostics], Target = Model.ProjectionTarget.PowerBi };
        PowerBiProjectionOptions projectionOptions = options.Projection with { EnvelopePolicies = options.Envelopes.Policies };
        PowerBiProjectionModel projection = new PowerBiModelProjection(projectionOptions).Project(transformed.Model, projectionContext);
        PowerBiSemanticModel domainModel = projection.ToSemanticModel();
        domainModel = ApplyExplicitArtifacts(domainModel, options);
        foreach (Action<PowerBiSemanticModel> callback in options.ConfigureModelCallbacks)
        {
            callback(domainModel);
        }

        return new SemanticDerivationResult<PowerBiSemanticModel>
        {
            Model = domainModel,
            Diagnostics = domainModel.Diagnostics,
            Trace = transformed.Trace,
        };
    }

    private static PowerBiSemanticModel ApplyExplicitArtifacts(PowerBiSemanticModel model, PowerBiDerivationOptions options)
    {
        List<PowerBiTableDefinition> tables = [.. model.Tables];
        List<Model.SchemaDiagnostic> diagnostics = [.. model.Diagnostics];

        foreach (PowerBiExplicitMeasure explicitMeasure in options.Measures.Items)
        {
            var tableIndex = tables.FindIndex(table => string.Equals(table.Name, explicitMeasure.TableName, StringComparison.OrdinalIgnoreCase) || string.Equals(table.SourceTypeId?.Value, explicitMeasure.TableName, StringComparison.OrdinalIgnoreCase));
            if (tableIndex < 0)
            {
                diagnostics.Add(Diagnostic("POWERBI_EXPLICIT_MEASURE_TABLE_NOT_FOUND", $"Explicit measure '{explicitMeasure.Name}' targets table '{explicitMeasure.TableName}', but no projected table matched.", $"/powerBi/measures/{explicitMeasure.Name}"));
                continue;
            }

            var measure = new PowerBiMeasureDefinition { Name = explicitMeasure.Name, Expression = explicitMeasure.Expression };
            explicitMeasure.Configure?.Invoke(measure);
            if (!string.Equals(measure.ExpressionLanguage, "DAX", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Diagnostic("POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE", $"Explicit measure '{measure.Name}' uses unsupported expression language '{measure.ExpressionLanguage}'.", $"/powerBi/measures/{measure.Name}"));
            }

            PowerBiTableDefinition table = tables[tableIndex];
            tables[tableIndex] = table with { Measures = [.. table.Measures, measure] };
        }

        foreach (PowerBiCalculatedTableDefinition calculatedTable in options.CalculatedTables.Items)
        {
            if (!string.Equals(calculatedTable.ExpressionLanguage, "DAX", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add(Diagnostic("POWERBI_UNSUPPORTED_CALCULATED_TABLE_EXPRESSION_LANGUAGE", $"Calculated table '{calculatedTable.Name}' uses unsupported expression language '{calculatedTable.ExpressionLanguage}'.", $"/powerBi/calculatedTables/{calculatedTable.Name}"));
            }
        }

        return model with
        {
            Tables = tables,
            CalculatedTables = [.. model.CalculatedTables, .. options.CalculatedTables.Items],
            Diagnostics = diagnostics,
        };
    }

    private static Model.SchemaDiagnostic Diagnostic(string code, string message, string path)
    {
        return new Model.SchemaDiagnostic
        {
            Severity = Model.SchemaDiagnosticSeverity.Warning,
            Code = code,
            Message = message,
            Stage = Model.SchemaDiagnosticStage.Projection,
            ProjectionTarget = Model.ProjectionTarget.PowerBi,
            ModelPath = path,
        };
    }
}

/// <summary>Marks the default Power BI domain derivation point in the configurable transformation pipeline.</summary>
public sealed class DerivePowerBiTablesTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "powerBi.derive-tables";

    /// <inheritdoc />
    public string DisplayName => nameof(DerivePowerBiTablesTransformation);

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(Model.TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();
        return new SemanticModelTransformationStepResult { Model = model, ChangeSummary = ["Power BI table derivation boundary prepared"] };
    }
}
