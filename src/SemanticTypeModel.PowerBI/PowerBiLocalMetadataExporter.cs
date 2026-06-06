using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticTypeModel.PowerBI;

/// <summary>Exports deterministic local Power BI metadata without service, PBIX, XMLA, or credential dependencies.</summary>
public static class PowerBiLocalMetadataExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Exports the Power BI domain semantic model to deterministic neutral JSON.</summary>
    public static string ExportJson(PowerBiSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var document = new
        {
            model.Name,
            Tables = model.Tables.OrderBy(static table => table.Name, StringComparer.Ordinal).Select(table => new
            {
                table.Name,
                table.DisplayName,
                Role = table.Role.ToString(),
                table.IsHidden,
                table.Description,
                table.DisplayFolder,
                Columns = table.Columns.OrderBy(static column => column.Name, StringComparer.Ordinal).Select(column => new
                {
                    column.Name,
                    column.DisplayName,
                    DataType = column.DataType.ToString(),
                    column.IsNullable,
                    column.IsKey,
                    column.IsHidden,
                    column.Description,
                    Summarization = column.Summarization.ToString(),
                    column.DataCategory,
                    column.FormatString,
                    column.SortByColumn,
                }),
                Measures = table.Measures.OrderBy(static measure => measure.Name, StringComparer.Ordinal).Select(measure => new
                {
                    measure.Name,
                    measure.DisplayName,
                    measure.IsHidden,
                    measure.Expression,
                    measure.ExpressionLanguage,
                    measure.FormatString,
                    measure.DisplayFolder,
                    measure.Description,
                }),
                Hierarchies = table.Hierarchies.OrderBy(static hierarchy => hierarchy.Name, StringComparer.Ordinal).Select(hierarchy => new
                {
                    hierarchy.Name,
                    hierarchy.Description,
                    Levels = hierarchy.Levels.OrderBy(static level => level.Ordinal).Select(static level => new { level.Name, level.Column, level.Ordinal }),
                }),
            }),
            Relationships = model.Relationships.OrderBy(static relationship => relationship.Name, StringComparer.Ordinal).Select(relationship => new
            {
                relationship.Name,
                relationship.FromTable,
                relationship.FromColumn,
                relationship.ToTable,
                relationship.ToColumn,
                Cardinality = relationship.Cardinality.ToString(),
                relationship.IsActive,
                Direction = relationship.Direction.ToString(),
            }),
            CalculatedTables = model.CalculatedTables.OrderBy(static table => table.Name, StringComparer.Ordinal).Select(table => new
            {
                table.Name,
                table.Expression,
                table.ExpressionLanguage,
                table.Description,
                table.IsHidden,
                table.DisplayFolder,
            }),
            Diagnostics = model.Diagnostics.OrderBy(static diagnostic => diagnostic.Code, StringComparer.Ordinal).ThenBy(static diagnostic => diagnostic.ModelPath, StringComparer.Ordinal).Select(static diagnostic => new
            {
                diagnostic.Code,
                Severity = diagnostic.Severity.ToString(),
                diagnostic.ModelPath,
                diagnostic.Message,
                ProjectionTarget = diagnostic.ProjectionTarget.ToString(),
            }),
        };

        return JsonSerializer.Serialize(document, JsonOptions) + Environment.NewLine;
    }

    /// <summary>Writes deterministic neutral JSON metadata to a local file.</summary>
    public static void ExportJson(PowerBiSemanticModel model, string path)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllText(path, ExportJson(model), Encoding.UTF8);
    }

    /// <summary>Produces deterministic inspection text for console and snapshot tests.</summary>
    public static string Inspect(PowerBiSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var builder = new StringBuilder();
        _ = builder.AppendLine(CultureInfo.InvariantCulture, $"PowerBiSemanticModel: {model.Name}");
        foreach (PowerBiTableDefinition table in model.Tables.OrderBy(static table => table.Name, StringComparer.Ordinal))
        {
            _ = builder.AppendLine(CultureInfo.InvariantCulture, $"  Table: {table.Name} ({table.Role}) Hidden={table.IsHidden}");
            foreach (PowerBiColumnDefinition column in table.Columns.OrderBy(static column => column.Name, StringComparer.Ordinal))
            {
                _ = builder.AppendLine(CultureInfo.InvariantCulture, $"    Column: {column.Name} Type={column.DataType} Nullable={column.IsNullable} Key={column.IsKey} Hidden={column.IsHidden} SummarizeBy={column.Summarization} SortBy={column.SortByColumn ?? "<none>"}");
            }

            foreach (PowerBiMeasureDefinition measure in table.Measures.OrderBy(static measure => measure.Name, StringComparer.Ordinal))
            {
                _ = builder.AppendLine(CultureInfo.InvariantCulture, $"    Measure: {measure.Name} Language={measure.ExpressionLanguage} Hidden={measure.IsHidden}");
            }
        }

        foreach (PowerBiRelationshipDefinition relationship in model.Relationships.OrderBy(static relationship => relationship.Name, StringComparer.Ordinal))
        {
            _ = builder.AppendLine(CultureInfo.InvariantCulture, $"  Relationship: {relationship.Name} {relationship.FromTable}[{relationship.FromColumn}] -> {relationship.ToTable}[{relationship.ToColumn}] {relationship.Cardinality} Active={relationship.IsActive}");
        }

        foreach (PowerBiCalculatedTableDefinition table in model.CalculatedTables.OrderBy(static table => table.Name, StringComparer.Ordinal))
        {
            _ = builder.AppendLine(CultureInfo.InvariantCulture, $"  CalculatedTable: {table.Name} Language={table.ExpressionLanguage} Hidden={table.IsHidden}");
        }

        return builder.ToString();
    }
}
