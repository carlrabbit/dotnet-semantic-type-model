using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.DotNet.Diagnostics;

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
        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider OptionsProvider)> generationInput =
            context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        context.RegisterSourceOutput(generationInput, static (productionContext, input) =>
        {
            (Compilation compilation, AnalyzerConfigOptionsProvider optionsProvider) = input;
            DotNetExtractionOptions options = ParseOptions(optionsProvider, compilation.Assembly.Locations.FirstOrDefault(), out IReadOnlyList<DotNetExtractionDiagnostic> optionDiagnostics);
            var extractor = new RoslynDotNetTypeExtractor();
            DotNetExtractionResult extraction = extractor.Extract(compilation, options);

            foreach (DotNetExtractionDiagnostic diagnostic in optionDiagnostics.Concat(extraction.Diagnostics))
            {
                productionContext.ReportDiagnostic(CreateDiagnostic(diagnostic));
            }

            if (extraction.TypesById.Count == 0 || extraction.RootTypeId is null)
            {
                return;
            }

            if (ProviderNameCollides(compilation, extraction.Options.GeneratedNamespace, extraction.Options.ProviderName))
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(
                    GeneratorDiagnosticDescriptors.GeneratedProviderNameCollision,
                    compilation.Assembly.Locations.FirstOrDefault(),
                    $"Generated provider name '{extraction.Options.GeneratedNamespace}.{extraction.Options.ProviderName}' collides with an existing type."));
                return;
            }

            string source = GenerateProviderSource(extraction);
            productionContext.AddSource("SemanticTypeModel.Generated.g.cs", source);

        });
    }

    private static DotNetExtractionOptions ParseOptions(
        AnalyzerConfigOptionsProvider optionsProvider,
        Location? location,
        out IReadOnlyList<DotNetExtractionDiagnostic> diagnostics)
    {
        var extractedDiagnostics = new List<DotNetExtractionDiagnostic>();
        DotNetExtractionOptions options = DotNetExtractionOptions.Default;
        AnalyzerConfigOptions globalOptions = optionsProvider.GlobalOptions;

        if (TryGetOption(globalOptions, "SemanticTypeModelGeneratedNamespace", out string? generatedNamespace))
        {
            options = options with { GeneratedNamespace = generatedNamespace! };
        }

        if (TryGetOption(globalOptions, "SemanticTypeModelGeneratedProviderName", out string? providerName))
        {
            options = options with { ProviderName = providerName! };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelIncludeInternalTypes", out bool includeInternalTypes))
        {
            options = options with { IncludeInternalTypes = includeInternalTypes };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelIncludeInternalMembers", out bool includeInternalMembers))
        {
            options = options with { IncludeInternalMembers = includeInternalMembers };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelInferKeys", out bool inferKeys))
        {
            options = options with { InferKeys = inferKeys };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelInferRelationships", out bool inferRelationships))
        {
            options = options with { InferRelationships = inferRelationships };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelIncludeXmlDocumentation", out bool includeXmlDocumentation))
        {
            options = options with { IncludeXmlDocumentation = includeXmlDocumentation };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelRequireXmlDocumentation", out bool requireXmlDocumentation))
        {
            options = options with { RequireXmlDocumentation = requireXmlDocumentation };
        }

        SystemTextJsonExtractionOptions systemTextJson = options.SystemTextJson;
        if (TryParseBoolOption(globalOptions, "SemanticTypeModelImportSystemTextJsonAttributes", out bool importSystemTextJsonAttributes))
        {
            systemTextJson = systemTextJson with { ImportAttributes = importSystemTextJsonAttributes };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelUseJsonPropertyNameAsSemanticName", out bool useJsonPropertyNameAsSemanticName))
        {
            systemTextJson = systemTextJson with { UseJsonPropertyNameAsSemanticName = useJsonPropertyNameAsSemanticName };
        }

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelGenerateSystemTextJsonContext", out bool generateSystemTextJsonContext)
            && generateSystemTextJsonContext)
        {
            extractedDiagnostics.Add(new DotNetExtractionDiagnostic(
                "STJ004",
                "Generated JsonSerializerContext support is removed in SemanticTypeModel 1.1.0; author a JsonSerializerContext and wrap it with SemanticTypeModel resolver customization instead.",
                location));
        }

        if (TryGetOption(globalOptions, "SemanticTypeModelSystemTextJsonContextName", out _))
        {
            extractedDiagnostics.Add(new DotNetExtractionDiagnostic(
                "STJ004",
                "SemanticTypeModelSystemTextJsonContextName is unsupported because SemanticTypeModel no longer generates JsonSerializerContext declarations.",
                location));
        }

        options = options with { SystemTextJson = systemTextJson };

        if (TryGetOption(globalOptions, "SemanticTypeModelIncludedNamespaces", out string? includedNamespaces))
        {
            options = options with { IncludedNamespaces = ParseDelimitedList(includedNamespaces!) };
        }

        if (TryGetOption(globalOptions, "SemanticTypeModelExcludedNamespaces", out string? excludedNamespaces))
        {
            options = options with { ExcludedNamespaces = ParseDelimitedList(excludedNamespaces!) };
        }

        if (TryGetOption(globalOptions, "SemanticTypeModelDiscoveryMode", out string? discoveryModeText))
        {
            if (Enum.TryParse(discoveryModeText, ignoreCase: true, out DotNetTypeDiscoveryMode discoveryMode))
            {
                options = options with { DiscoveryMode = discoveryMode };
            }
            else
            {
                extractedDiagnostics.Add(new DotNetExtractionDiagnostic(
                    DotNetExtractionDiagnosticIds.UnsupportedDiscoveryMode,
                    $"Discovery mode '{discoveryModeText}' is not supported.",
                    location));
            }
        }

        if (TryGetOption(globalOptions, "SemanticTypeModelNamingPolicy", out string? namingPolicyText))
        {
            if (Enum.TryParse(namingPolicyText, ignoreCase: true, out DotNetNamingPolicy namingPolicy))
            {
                options = options with { NamingPolicy = namingPolicy };
            }
            else
            {
                extractedDiagnostics.Add(new DotNetExtractionDiagnostic(
                    DotNetExtractionDiagnosticIds.UnsupportedNamingPolicy,
                    $"Naming policy '{namingPolicyText}' is not supported.",
                    location));
            }
        }

        diagnostics = extractedDiagnostics;
        return options;
    }

    private static bool TryGetOption(AnalyzerConfigOptions options, string optionName, out string? value)
    {
        if (options.TryGetValue("build_property." + optionName, out string? configuredValue)
            && !string.IsNullOrWhiteSpace(configuredValue))
        {
            value = configuredValue;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseBoolOption(AnalyzerConfigOptions options, string optionName, out bool value)
    {
        return TryGetOption(options, optionName, out string? configuredValue)
            ? bool.TryParse(configuredValue, out value)
            : SetFalse(out value);
    }

    private static bool SetFalse(out bool value)
    {
        value = false;
        return false;
    }

    private static string[] ParseDelimitedList(string value)
    {
        return
        [
            .. value
                .Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.Ordinal),
        ];
    }

    private static bool ProviderNameCollides(Compilation compilation, string generatedNamespace, string providerName)
    {
        INamespaceSymbol scope = compilation.Assembly.GlobalNamespace;
        foreach (string segment in generatedNamespace.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            INamespaceSymbol? next = scope.GetNamespaceMembers().FirstOrDefault(candidate => string.Equals(candidate.Name, segment, StringComparison.Ordinal));
            if (next is null)
            {
                return false;
            }

            scope = next;
        }

        return scope.GetTypeMembers(providerName).Length > 0;
    }

    private static Diagnostic CreateDiagnostic(DotNetExtractionDiagnostic diagnostic)
    {
        DiagnosticDescriptor descriptor = diagnostic.Code switch
        {
            DotNetExtractionDiagnosticIds.UnsupportedDiscoveryMode => GeneratorDiagnosticDescriptors.UnsupportedDiscoveryMode,
            DotNetExtractionDiagnosticIds.UnsupportedNamingPolicy => GeneratorDiagnosticDescriptors.UnsupportedNamingPolicy,
            DotNetExtractionDiagnosticIds.GeneratedProviderNameCollision => GeneratorDiagnosticDescriptors.GeneratedProviderNameCollision,
            _ => GeneratorDiagnosticDescriptors.ExtractionFallback(diagnostic.Code),
        };

        return Diagnostic.Create(descriptor, diagnostic.Location, diagnostic.Message);
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

        if (builder.Length == 0)
        {
            return "AppSemanticTypeModel";
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
