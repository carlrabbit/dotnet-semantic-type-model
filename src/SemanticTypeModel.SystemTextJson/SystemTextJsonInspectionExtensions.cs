#pragma warning disable IDE0058
using System.Globalization;
using System.Text;
using SemanticTypeModel.Core.Inspection;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Provides deterministic inspection output for System.Text.Json domain semantic models.
/// </summary>
public static class SystemTextJsonInspectionExtensions
{
    /// <summary>
    /// Produces deterministic text for a System.Text.Json domain semantic model.
    /// </summary>
    public static string ToSemanticText(this SystemTextJsonSemanticModel model, SemanticTextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= new SemanticTextOptions();

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"System.Text.Json model");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Types: {model.TypesById.Count}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"PropertyNameSource: {model.PropertyNameSource}");

        if (options.Detail != SemanticTextDetail.Summary)
        {
            builder.AppendLine();
            builder.AppendLine("Types:");
            foreach (SystemTextJsonTypeDefinition type in model.TypesById.Values.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  {type.Id.Value} ({type.Name})");
                foreach (SystemTextJsonPropertyDefinition property in type.Properties.OrderBy(static property => property.SemanticName, StringComparer.Ordinal))
                {
                    builder.Append(CultureInfo.InvariantCulture, $"    Property {property.SemanticName}");
                    if (!string.IsNullOrWhiteSpace(property.DotNetMemberName))
                    {
                        builder.Append(CultureInfo.InvariantCulture, $" member={property.DotNetMemberName}");
                    }

                    if (!string.IsNullOrWhiteSpace(property.SystemTextJsonPropertyName))
                    {
                        builder.Append(CultureInfo.InvariantCulture, $" jsonName={property.SystemTextJsonPropertyName}");
                    }

                    if (!string.IsNullOrWhiteSpace(property.ProjectedJsonName))
                    {
                        builder.Append(CultureInfo.InvariantCulture, $" projectedJsonName={property.ProjectedJsonName}");
                    }

                    if (property.IsExtensionData)
                    {
                        builder.Append(" extensionData");
                    }

                    builder.AppendLine();
                }
            }
        }

        if (options.IncludeDiagnostics && model.Diagnostics.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Diagnostics:");
            builder.Append(model.Diagnostics.ToDiagnosticText(new DiagnosticTextOptions { Detail = options.Detail }));
        }

        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
