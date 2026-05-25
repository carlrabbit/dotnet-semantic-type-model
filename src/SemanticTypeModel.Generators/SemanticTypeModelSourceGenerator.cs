using System.Text;
using Microsoft.CodeAnalysis;
using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Generators;

/// <summary>
/// Incremental source generator that extracts C# type metadata and emits a deterministic semantic model provider.
/// </summary>
[Generator]
public sealed class SemanticTypeModelSourceGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (productionContext, compilation) =>
        {
            var extractor = new RoslynDotNetTypeExtractor();
            DotNetExtractionResult extraction = extractor.Extract(compilation);

            foreach (DotNetExtractionDiagnostic diagnostic in extraction.Diagnostics)
            {
                productionContext.ReportDiagnostic(CreateDiagnostic(diagnostic));
            }

            if (extraction.TypesById.Count == 0 || extraction.RootTypeId is null)
            {
                return;
            }

            string source = GenerateProviderSource(extraction);
            productionContext.AddSource("SemanticTypeModel.Generated.g.cs", source);
        });
    }

    private static Diagnostic CreateDiagnostic(DotNetExtractionDiagnostic diagnostic)
    {
        var descriptor = new DiagnosticDescriptor(
            diagnostic.Code,
            "SemanticTypeModel .NET extraction",
            diagnostic.Message,
            "SemanticTypeModel.Generators",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, diagnostic.Location);
    }

    private static string GenerateProviderSource(DotNetExtractionResult extraction)
    {
        var source = new StringBuilder();
        source.AppendLine("using global::SemanticTypeModel.Abstractions.Model;");
        source.AppendLine("using global::SemanticTypeModel.Core.Building;");
        source.AppendLine();
        source.AppendLine($"namespace {extraction.Options.GeneratedNamespace};");
        source.AppendLine();
        source.AppendLine($"public static partial class {SanitizeIdentifier(extraction.Options.ProviderName)}");
        source.AppendLine("{");
        source.AppendLine("    /// <summary>");
        source.AppendLine("    /// Creates the generated canonical semantic type model.");
        source.AppendLine("    /// </summary>");
        source.AppendLine("    public static global::SemanticTypeModel.Abstractions.Model.TypeSchemaModel Create()");
        source.AppendLine("    {");
        source.AppendLine("        var builder = new global::SemanticTypeModel.Core.Building.TypeSchemaModelBuilder();");

        foreach ((string typeId, DotNetTypeDescriptor descriptor) in extraction.TypesById.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            _ = typeId;
            AppendShape(source, descriptor);
        }

        source.AppendLine($"        builder.SetRoot(\"{EscapeString(extraction.RootTypeId!)}\");");
        source.AppendLine("        return builder.Build();");
        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void AppendShape(StringBuilder source, DotNetTypeDescriptor descriptor)
    {
        source.AppendLine($"        builder.AddShape(\"{EscapeString(descriptor.Id)}\",");
        switch (descriptor)
        {
            case DotNetObjectTypeDescriptor obj:
                AppendObjectShape(source, obj);
                break;
            case DotNetScalarTypeDescriptor scalar:
                AppendScalarShape(source, scalar);
                break;
            case DotNetEnumTypeDescriptor @enum:
                AppendEnumShape(source, @enum);
                break;
            case DotNetArrayTypeDescriptor array:
                source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.ArrayShape");
                source.AppendLine("            {");
                source.AppendLine($"                Items = global::SemanticTypeModel.Abstractions.Model.ShapeRef.FromIdentifier(\"{EscapeString(array.ItemTypeId)}\"),");
                AppendAnnotations(source, array.Annotations, 4);
                source.AppendLine("            });");
                break;
            case DotNetDictionaryTypeDescriptor dictionary:
                source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.DictionaryShape");
                source.AppendLine("            {");
                source.AppendLine($"                Values = global::SemanticTypeModel.Abstractions.Model.ShapeRef.FromIdentifier(\"{EscapeString(dictionary.ValueTypeId)}\"),");
                AppendAnnotations(source, dictionary.Annotations, 4);
                source.AppendLine("            });");
                break;
            default:
                source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.ScalarShape { Kind = global::SemanticTypeModel.Abstractions.Model.ScalarKind.String });");
                break;
        }
    }

    private static void AppendObjectShape(StringBuilder source, DotNetObjectTypeDescriptor descriptor)
    {
        source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.ObjectShape");
        source.AppendLine("            {");
        source.AppendLine("                Properties =");
        source.AppendLine("                [");
        foreach (DotNetPropertyDescriptor property in descriptor.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            source.AppendLine("                    new global::SemanticTypeModel.Abstractions.Model.PropertyShape");
            source.AppendLine("                    {");
            source.AppendLine($"                        Name = \"{EscapeString(property.Name)}\",");
            source.AppendLine($"                        IsRequired = {property.IsRequired.ToString().ToLowerInvariant()},");
            source.AppendLine($"                        IsNullable = {property.IsNullable.ToString().ToLowerInvariant()},");
            source.AppendLine($"                        Type = global::SemanticTypeModel.Abstractions.Model.ShapeRef.FromIdentifier(\"{EscapeString(property.TypeId)}\"),");
            AppendAnnotations(source, property.Annotations, 6);
            source.AppendLine("                    },");
        }

        source.AppendLine("                ],");
        source.AppendLine("                AdditionalPropertiesAllowed = false,");
        AppendAnnotations(source, descriptor.Annotations, 4);
        source.AppendLine("            });");
    }

    private static void AppendScalarShape(StringBuilder source, DotNetScalarTypeDescriptor descriptor)
    {
        string shapeKind = descriptor.ScalarKind switch
        {
            DotNetScalarKind.Boolean => "Boolean",
            DotNetScalarKind.Integer => "Integer",
            DotNetScalarKind.Number or DotNetScalarKind.Decimal => "Number",
            _ => "String",
        };

        var annotations = new Dictionary<string, string>(descriptor.Annotations, StringComparer.Ordinal)
        {
            ["dotnet.scalarKind"] = descriptor.ScalarKind.ToString(),
        };

        switch (descriptor.ScalarKind)
        {
            case DotNetScalarKind.Date:
                annotations["schema.format"] = "date";
                break;
            case DotNetScalarKind.Time:
                annotations["schema.format"] = "time";
                break;
            case DotNetScalarKind.DateTime:
            case DotNetScalarKind.DateTimeOffset:
                annotations["schema.format"] = "date-time";
                break;
            case DotNetScalarKind.Duration:
                annotations["schema.format"] = "duration";
                break;
            case DotNetScalarKind.Guid:
                annotations["schema.format"] = "uuid";
                break;
            case DotNetScalarKind.Binary:
                annotations["schema.format"] = "byte";
                break;
            default:
                break;
        }

        source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.ScalarShape");
        source.AppendLine("            {");
        source.AppendLine($"                Kind = global::SemanticTypeModel.Abstractions.Model.ScalarKind.{shapeKind},");
        source.AppendLine("                IsNullable = false,");
        AppendAnnotations(source, annotations, 4);
        source.AppendLine("            });");
    }

    private static void AppendEnumShape(StringBuilder source, DotNetEnumTypeDescriptor descriptor)
    {
        source.AppendLine("            new global::SemanticTypeModel.Abstractions.Model.EnumShape");
        source.AppendLine("            {");
        source.AppendLine("                Values =");
        source.AppendLine("                [");
        foreach (DotNetEnumValueDescriptor value in descriptor.Values)
        {
            source.AppendLine($"                    \"{EscapeString(value.Name)}\",");
        }

        source.AppendLine("                ],");
        var annotations = new Dictionary<string, string>(descriptor.Annotations, StringComparer.Ordinal)
        {
            ["dotnet.enumNumericValues"] = "[" + string.Join(",", descriptor.Values.Select(static value => value.NumericValue.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]",
        };
        AppendAnnotations(source, annotations, 4);
        source.AppendLine("            });");
    }

    private static void AppendAnnotations(StringBuilder source, IReadOnlyDictionary<string, string> annotations, int indentationLevel)
    {
        if (annotations.Count == 0)
        {
            return;
        }

        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}Annotations =");
        source.AppendLine($"{indent}[");
        foreach ((string key, string value) in annotations.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            source.AppendLine($"{indent}    new global::SemanticTypeModel.Abstractions.Model.SchemaAnnotation(\"{EscapeString(key)}\", \"{EscapeString(value)}\"),");
        }

        source.AppendLine($"{indent}],");
    }

    private static string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return "AppSemanticTypeModel";
        }

        var builder = new StringBuilder(identifier.Length);
        for (var i = 0; i < identifier.Length; i++)
        {
            char character = identifier[i];
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
                continue;
            }

            builder.Append('_');
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
