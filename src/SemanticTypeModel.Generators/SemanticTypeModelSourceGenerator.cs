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
        source.AppendLine($"namespace {extraction.Options.GeneratedNamespace};");
        source.AppendLine();
        source.AppendLine($"public static partial class {SanitizeIdentifier(extraction.Options.ProviderName)}");
        source.AppendLine("{");
        source.AppendLine("    /// <summary>");
        source.AppendLine("    /// Creates the generated canonical semantic type model.");
        source.AppendLine("    /// </summary>");
        source.AppendLine("    public static global::SemanticTypeModel.Abstractions.Model.TypeSchemaModel Create()");
        source.AppendLine("    {");
        source.AppendLine("        global::System.Collections.Generic.List<global::SemanticTypeModel.Abstractions.Model.TypeDefinition> types =");
        source.AppendLine("        [");

        foreach ((string _, DotNetTypeDescriptor descriptor) in extraction.TypesById.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            AppendTypeDefinition(source, descriptor, 3);
            source.AppendLine(",");
        }

        source.AppendLine("        ];");
        source.AppendLine();
        source.AppendLine("        return new global::SemanticTypeModel.Abstractions.Model.TypeSchemaModel");
        source.AppendLine("        {");
        source.AppendLine($"            Id = new global::SemanticTypeModel.Abstractions.Model.SchemaModelId(\"{EscapeString(extraction.RootTypeId!)}\"),");
        source.AppendLine("            Types = types,");
        source.AppendLine("            TypesById = types.ToDictionary(static type => type.Id, static type => type),");
        source.AppendLine("            Annotations = new global::SemanticTypeModel.Abstractions.Model.AnnotationBag(),");
        source.AppendLine("        };");
        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void AppendTypeDefinition(StringBuilder source, DotNetTypeDescriptor descriptor, int indentationLevel)
    {
        switch (descriptor)
        {
            case DotNetObjectTypeDescriptor obj:
                AppendObjectType(source, obj, indentationLevel);
                break;
            case DotNetScalarTypeDescriptor scalar:
                AppendScalarType(source, scalar, indentationLevel);
                break;
            case DotNetEnumTypeDescriptor @enum:
                AppendEnumType(source, @enum, indentationLevel);
                break;
            case DotNetArrayTypeDescriptor array:
                AppendArrayType(source, array, indentationLevel);
                break;
            case DotNetDictionaryTypeDescriptor dictionary:
                AppendDictionaryType(source, dictionary, indentationLevel);
                break;
            default:
                AppendFallbackScalarType(source, descriptor, indentationLevel);
                break;
        }
    }

    private static void AppendObjectType(StringBuilder source, DotNetObjectTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.ObjectTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Object", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    Properties =");
        source.AppendLine($"{indent}    [");
        foreach (DotNetPropertyDescriptor property in descriptor.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
        {
            source.AppendLine($"{indent}        new global::SemanticTypeModel.Abstractions.Model.PropertyDefinition");
            source.AppendLine($"{indent}        {{");
            source.AppendLine($"{indent}            Id = new global::SemanticTypeModel.Abstractions.Model.PropertyId(\"{EscapeString(descriptor.Id + "." + property.Name)}\"),");
            source.AppendLine($"{indent}            Name = \"{EscapeString(property.Name)}\",");
            source.AppendLine($"{indent}            Type = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(property.TypeId)}\")),");
            source.AppendLine($"{indent}            Cardinality = new global::SemanticTypeModel.Abstractions.Model.Cardinality {{ IsRequired = {property.IsRequired.ToString().ToLowerInvariant()}, AllowsNull = {property.IsNullable.ToString().ToLowerInvariant()} }},");
            source.AppendLine($"{indent}            Mutability = global::SemanticTypeModel.Abstractions.Model.Mutability.Mutable,");
            source.AppendLine($"{indent}            Constraints = new global::SemanticTypeModel.Abstractions.Model.ConstraintSet(),");
            AppendAnnotationBag(source, property.Annotations, indentationLevel + 3, "Annotations");
            source.AppendLine($"{indent}        }},");
        }
        source.AppendLine($"{indent}    ],");
        source.AppendLine($"{indent}    Keys = [],");
        source.AppendLine($"{indent}    Relationships = [],");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendScalarType(StringBuilder source, DotNetScalarTypeDescriptor descriptor, int indentationLevel)
    {
        string kind = descriptor.ScalarKind switch
        {
            DotNetScalarKind.Boolean => "Boolean",
            DotNetScalarKind.Integer => "Integer",
            DotNetScalarKind.Number => "Number",
            DotNetScalarKind.Decimal => "Decimal",
            DotNetScalarKind.Date => "Date",
            DotNetScalarKind.Time => "Time",
            DotNetScalarKind.DateTime => "DateTime",
            DotNetScalarKind.DateTimeOffset => "DateTimeOffset",
            DotNetScalarKind.Duration => "Duration",
            DotNetScalarKind.Guid => "Guid",
            DotNetScalarKind.Binary => "Binary",
            _ => "String",
        };
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.ScalarTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Scalar", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    ScalarKind = global::SemanticTypeModel.Abstractions.Model.ScalarKind.{kind},");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendEnumType(StringBuilder source, DotNetEnumTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.EnumTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Enum", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    StorageKind = global::SemanticTypeModel.Abstractions.Model.EnumStorageKind.String,");
        source.AppendLine($"{indent}    Values =");
        source.AppendLine($"{indent}    [");
        foreach (DotNetEnumValueDescriptor value in descriptor.Values)
        {
            source.AppendLine($"{indent}        new global::SemanticTypeModel.Abstractions.Model.EnumValueDefinition {{ Name = \"{EscapeString(value.Name)}\", Value = \"{EscapeString(value.Name)}\", Annotations = new global::SemanticTypeModel.Abstractions.Model.AnnotationBag() }},");
        }
        source.AppendLine($"{indent}    ],");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendArrayType(StringBuilder source, DotNetArrayTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.ArrayTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Array", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    ItemType = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(descriptor.ItemTypeId)}\")),");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendDictionaryType(StringBuilder source, DotNetDictionaryTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.DictionaryTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Dictionary", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    KeyType = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"global::System.String\")),");
        source.AppendLine($"{indent}    ValueType = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(descriptor.ValueTypeId)}\")),");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendFallbackScalarType(StringBuilder source, DotNetTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.ScalarTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "Scalar", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    ScalarKind = global::SemanticTypeModel.Abstractions.Model.ScalarKind.Unknown,");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendCommonTypeMembers(StringBuilder source, string id, string name, string kind, bool allowsNull, IReadOnlyDictionary<string, string> annotations, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}Id = new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(id)}\"),");
        source.AppendLine($"{indent}Name = \"{EscapeString(name)}\",");
        source.AppendLine($"{indent}Kind = global::SemanticTypeModel.Abstractions.Model.TypeKind.{kind},");
        source.AppendLine($"{indent}Nullability = global::SemanticTypeModel.Abstractions.Model.Nullability.{(allowsNull ? "Nullable" : "NonNullable")},");
        AppendAnnotationBag(source, annotations, indentationLevel, "Annotations");
    }

    private static void AppendAnnotationBag(StringBuilder source, IReadOnlyDictionary<string, string> annotations, int indentationLevel, string memberName)
    {
        string indent = new(' ', indentationLevel * 4);
        if (annotations.Count == 0)
        {
            source.AppendLine($"{indent}{memberName} = new global::SemanticTypeModel.Abstractions.Model.AnnotationBag(),");
            return;
        }

        source.AppendLine($"{indent}{memberName} = new global::SemanticTypeModel.Abstractions.Model.AnnotationBag");
        source.AppendLine($"{indent}{{");
        source.AppendLine($"{indent}    Items =");
        source.AppendLine($"{indent}    [");
        foreach ((string key, string value) in annotations.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            source.AppendLine($"{indent}        new global::SemanticTypeModel.Abstractions.Model.Annotation {{ Key = new global::SemanticTypeModel.Abstractions.Model.AnnotationKey(\"{EscapeString(key)}\"), Value = \"{EscapeString(value)}\", Scope = global::SemanticTypeModel.Abstractions.Model.AnnotationScope.Type, Source = global::SemanticTypeModel.Abstractions.Model.AnnotationSource.Generated }},");
        }
        source.AppendLine($"{indent}    ],");
        source.AppendLine($"{indent}}},");
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
