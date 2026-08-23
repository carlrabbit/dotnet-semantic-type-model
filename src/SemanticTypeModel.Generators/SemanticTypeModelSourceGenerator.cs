using System.Reflection;
using System.Text;
using System.Text.Json;
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

            productionContext.AddSource("SemanticTypeModel.Manifest.g.cs", GenerateManifestSource(extraction));

        });
    }

    private static string GenerateManifestSource(DotNetExtractionResult extraction)
    {
        // AssemblyMetadataAttribute is deliberately used as the hand-off boundary. Roslyn exposes
        // it from a referenced assembly's metadata without loading or executing that assembly.
        var manifest = new SemanticManifest
        {
            Version = 2,
            SemanticTypeModelVersion = SuiteVersion.Current,
            ModelName = SanitizeIdentifier(extraction.Options.ProviderName),
            Types = [.. extraction.TypesById.Values.OrderBy(static type => type.Id, StringComparer.Ordinal).Select(ToManifestType)],
        };
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest)));
        return $"[assembly: global::System.Reflection.AssemblyMetadataAttribute(\"SemanticTypeModel.Manifest\", \"{EscapeString(payload)}\")]\n";
    }

    private static ManifestType ToManifestType(DotNetTypeDescriptor descriptor)
    {
        return new ManifestType
        {
            Id = descriptor.Id,
            Name = descriptor.Name,
            ClrName = Annotation(descriptor.Annotations, "dotnet.clrType") ?? descriptor.Id,
            BaseClrName = Annotation(descriptor.Annotations, "dotnet.baseType"),
            Kind = descriptor switch
            {
                DotNetObjectTypeDescriptor => "Object",
                DotNetEnumTypeDescriptor => "Enum",
                DotNetArrayTypeDescriptor => "Array",
                DotNetDictionaryTypeDescriptor => "Dictionary",
                DotNetScalarTypeDescriptor => "Scalar",
                DotNetStrongScalarTypeDescriptor => "StrongScalar",
                _ => "Unknown",
            },
            Role = Annotation(descriptor.Annotations, "schema.role"),
            ItemTypeId = (descriptor as DotNetArrayTypeDescriptor)?.ItemTypeId,
            ValueTypeId = (descriptor as DotNetStrongScalarTypeDescriptor)?.ValueTypeId,
            Properties = descriptor is DotNetObjectTypeDescriptor objectType
                ? [.. objectType.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal).Select(static property => new ManifestProperty
                {
                    Name = property.Name,
                    MemberName = Annotation(property.Annotations, "dotnet.memberName") ?? property.Name,
                    DeclaringClrName = Annotation(property.Annotations, "dotnet.declaringType"),
                    TypeId = property.TypeId,
                    IsRequired = property.IsRequired,
                    IsNullable = property.IsNullable,
                    IsPrimaryKey = string.Equals(Annotation(property.Annotations, "schema.key"), "true", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(Annotation(property.Annotations, "schema.key.kind") ?? "Primary", "Primary", StringComparison.OrdinalIgnoreCase),
                    KeyOrder = int.TryParse(Annotation(property.Annotations, "schema.key.order"), out int order) ? order : 0,
                    Ownership = string.Equals(Annotation(property.Annotations, "schema.ownedCollection"), "true", StringComparison.OrdinalIgnoreCase) ? "Collection"
                        : string.Equals(Annotation(property.Annotations, "schema.ownedObject"), "true", StringComparison.OrdinalIgnoreCase) ? "Object" : null,
                    IsExtensionData = string.Equals(Annotation(property.Annotations, "schema.extensionData"), "true", StringComparison.OrdinalIgnoreCase),
                })]
                : [],
        };
    }

    private static string? Annotation(IReadOnlyDictionary<string, string> annotations, string key)
    {
        return annotations.TryGetValue(key, out string? value) ? value : null;
    }

    private sealed class SemanticManifest
    {
        public int Version { get; set; }
        public string SemanticTypeModelVersion { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public IReadOnlyList<ManifestType> Types { get; set; } = [];
    }

    private static class SuiteVersion
    {
        internal static readonly string Current =
            typeof(SemanticTypeModelSourceGenerator).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                .Split('+')[0]
            ?? typeof(SemanticTypeModelSourceGenerator).Assembly.GetName().Version?.ToString(3)
            ?? "0.0.0";
    }

    private sealed class ManifestType
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClrName { get; set; } = string.Empty;
        public string? BaseClrName { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string? Role { get; set; }
        public string? ItemTypeId { get; set; }
        public string? ValueTypeId { get; set; }
        public IReadOnlyList<ManifestProperty> Properties { get; set; } = [];
    }

    private sealed class ManifestProperty
    {
        public string Name { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string? DeclaringClrName { get; set; }
        public string TypeId { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public int KeyOrder { get; set; }
        public string? Ownership { get; set; }
        public bool IsExtensionData { get; set; }
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

        if (TryParseBoolOption(globalOptions, "SemanticTypeModelRequireTechnicalDescription", out bool requireTechnicalDescription))
        {
            options = options with { RequireTechnicalDescription = requireTechnicalDescription };
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
            DotNetExtractionDiagnosticIds.TypedLiteralSourceNotFound => GeneratorDiagnosticDescriptors.TypedLiteralSourceNotFound,
            DotNetExtractionDiagnosticIds.TypedLiteralSourceTypeUnsupported => GeneratorDiagnosticDescriptors.TypedLiteralSourceTypeUnsupported,
            DotNetExtractionDiagnosticIds.TypedLiteralValueInvalid => GeneratorDiagnosticDescriptors.TypedLiteralValueInvalid,
            DotNetExtractionDiagnosticIds.TypedLiteralEnumMemberNotFound => GeneratorDiagnosticDescriptors.TypedLiteralEnumMemberNotFound,
            DotNetExtractionDiagnosticIds.TypedLiteralNumericFormatInvalid => GeneratorDiagnosticDescriptors.TypedLiteralNumericFormatInvalid,
            DotNetExtractionDiagnosticIds.TypedLiteralNumericOverflow => GeneratorDiagnosticDescriptors.TypedLiteralNumericOverflow,
            DotNetExtractionDiagnosticIds.TypedLiteralBooleanInvalid => GeneratorDiagnosticDescriptors.TypedLiteralBooleanInvalid,
            DotNetExtractionDiagnosticIds.TypedLiteralNullNotAllowed => GeneratorDiagnosticDescriptors.TypedLiteralNullNotAllowed,
            DotNetExtractionDiagnosticIds.ConditionalConstraintTargetInvalid => GeneratorDiagnosticDescriptors.ConditionalConstraintTargetInvalid,
            DotNetExtractionDiagnosticIds.ConditionalConstraintSourceInvalid => GeneratorDiagnosticDescriptors.ConditionalConstraintSourceInvalid,
            DotNetExtractionDiagnosticIds.ConditionalConstraintLiteralTypeMismatch => GeneratorDiagnosticDescriptors.ConditionalConstraintLiteralTypeMismatch,
            DotNetExtractionDiagnosticIds.DisplayIdentityDefinitionInvalid => GeneratorDiagnosticDescriptors.DisplayIdentityDefinitionInvalid,
            DotNetExtractionDiagnosticIds.AccessPathDefinitionInvalid => GeneratorDiagnosticDescriptors.AccessPathDefinitionInvalid,
            _ => GeneratorDiagnosticDescriptors.ExtractionFallback(diagnostic.Code),
        };

        return Diagnostic.Create(descriptor, diagnostic.Location, diagnostic.Message);
    }


    private static string GenerateProviderSource(DotNetExtractionResult extraction)
    {
        var source = new StringBuilder();
        source.AppendLine($"namespace {extraction.Options.GeneratedNamespace};");
        source.AppendLine();
        source.AppendLine("/// <summary>Provides the generated canonical semantic type model.</summary>");
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
            case DotNetStrongScalarTypeDescriptor strongScalar:
                AppendStrongScalarType(source, strongScalar, indentationLevel);
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
            source.AppendLine($"{indent}            Mutability = {MutabilityLiteral(property.Mutability)},");
            source.AppendLine($"{indent}            UserDescription = {Literal(GetAnnotationValue(property.Annotations, "schema.userDescription"))},");
            source.AppendLine($"{indent}            TechnicalDescription = {Literal(GetAnnotationValue(property.Annotations, "schema.technicalDescription"))},");
            AppendConstraints(source, property, indentationLevel + 3);
            AppendAnnotationBag(source, property.Annotations, indentationLevel + 3, "Annotations");
            source.AppendLine($"{indent}        }},");
        }
        source.AppendLine($"{indent}    ],");
        source.AppendLine($"{indent}    Keys = [],");
        source.AppendLine($"{indent}    Mutability = {MutabilityLiteral(descriptor.Mutability)},");
        source.AppendLine($"{indent}}}");
    }

    private static string MutabilityLiteral(Abstractions.Model.SemanticMutability? mutability)
    {
        return mutability is null ? "null" : $"global::SemanticTypeModel.Abstractions.Model.SemanticMutability.{mutability}";
    }

    private static void AppendConstraints(StringBuilder source, DotNetPropertyDescriptor property, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        string? minLength = IntegerLiteral(property.Annotations, "schema.minLength");
        string? maxLength = IntegerLiteral(property.Annotations, "schema.maxLength");
        string? pattern = GetAnnotationValue(property.Annotations, "schema.pattern");
        string? minimum = DecimalLiteral(property.Annotations, "schema.minimum");
        string? maximum = DecimalLiteral(property.Annotations, "schema.maximum");
        string? multipleOf = DecimalLiteral(property.Annotations, "schema.multipleOf");
        string? minItems = IntegerLiteral(property.Annotations, "schema.minItems");
        string? maxItems = IntegerLiteral(property.Annotations, "schema.maxItems");
        bool uniqueItems = string.Equals(GetAnnotationValue(property.Annotations, "schema.uniqueItems"), "true", StringComparison.OrdinalIgnoreCase);
        bool hasString = minLength is not null || maxLength is not null || pattern is not null;
        bool hasNumeric = minimum is not null || maximum is not null || multipleOf is not null
            || property.Annotations.ContainsKey("schema.exclusiveMinimum")
            || property.Annotations.ContainsKey("schema.exclusiveMaximum");
        bool hasArray = minItems is not null || maxItems is not null || uniqueItems;

        if (property.ConditionalConstraints.Count == 0 && !hasString && !hasNumeric && !hasArray)
        {
            source.AppendLine($"{indent}Constraints = new global::SemanticTypeModel.Abstractions.Model.ConstraintSet(),");
            return;
        }

        source.AppendLine($"{indent}Constraints = new global::SemanticTypeModel.Abstractions.Model.ConstraintSet");
        source.AppendLine($"{indent}{{");
        if (hasString)
        {
            source.AppendLine($"{indent}    String = new global::SemanticTypeModel.Abstractions.Model.StringConstraints {{ MinLength = {minLength ?? "null"}, MaxLength = {maxLength ?? "null"}, Pattern = {Literal(pattern)} }},");
        }
        if (hasNumeric)
        {
            source.AppendLine($"{indent}    Numeric = new global::SemanticTypeModel.Abstractions.Model.NumericConstraints {{ Minimum = {minimum ?? "null"}, Maximum = {maximum ?? "null"}, ExclusiveMinimum = {property.Annotations.ContainsKey("schema.exclusiveMinimum").ToString().ToLowerInvariant()}, ExclusiveMaximum = {property.Annotations.ContainsKey("schema.exclusiveMaximum").ToString().ToLowerInvariant()}, MultipleOf = {multipleOf ?? "null"} }},");
        }
        if (hasArray)
        {
            source.AppendLine($"{indent}    Array = new global::SemanticTypeModel.Abstractions.Model.ArrayConstraints {{ MinItems = {minItems ?? "null"}, MaxItems = {maxItems ?? "null"}, UniqueItems = {uniqueItems.ToString().ToLowerInvariant()} }},");
        }
        if (property.ConditionalConstraints.Count == 0)
        {
            source.AppendLine($"{indent}}},");
            return;
        }
        source.AppendLine($"{indent}    Conditional =");
        source.AppendLine($"{indent}    [");
        foreach (Abstractions.Model.ConditionalConstraint constraint in property.ConditionalConstraints)
        {
            Abstractions.Model.SemanticLiteral literal = constraint.Literal;
            string value = literal.Value switch
            {
                null => "null",
                bool boolean => boolean ? "true" : "false",
                string text => $"\"{EscapeString(text)}\"",
                decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
                IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => $"\"{EscapeString(Convert.ToString(literal.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty)}\"",
            };
            source.AppendLine($"{indent}        new global::SemanticTypeModel.Abstractions.Model.ConditionalConstraint");
            source.AppendLine($"{indent}        {{");
            source.AppendLine($"{indent}            TargetPropertyId = new global::SemanticTypeModel.Abstractions.Model.PropertyId(\"{EscapeString(constraint.TargetPropertyId.Value)}\"),");
            source.AppendLine($"{indent}            SourcePropertyName = \"{EscapeString(constraint.SourcePropertyName)}\",");
            source.AppendLine($"{indent}            SourcePropertyId = new global::SemanticTypeModel.Abstractions.Model.PropertyId(\"{EscapeString(constraint.SourcePropertyId.Value)}\"),");
            source.AppendLine($"{indent}            SourceTypeId = new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(constraint.SourceTypeId.Value)}\"),");
            source.AppendLine($"{indent}            Operator = global::SemanticTypeModel.Abstractions.Model.ConditionalConstraintOperator.{constraint.Operator},");
            source.AppendLine($"{indent}            Literal = new global::SemanticTypeModel.Abstractions.Model.SemanticLiteral");
            source.AppendLine($"{indent}            {{");
            source.AppendLine($"{indent}                Kind = global::SemanticTypeModel.Abstractions.Model.SemanticLiteralKind.{literal.Kind}, RawText = \"{EscapeString(literal.RawText)}\", NormalizedText = \"{EscapeString(literal.NormalizedText)}\",");
            source.AppendLine($"{indent}                TypeId = new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(literal.TypeId?.Value ?? string.Empty)}\"), ClrTypeName = \"{EscapeString(literal.ClrTypeName ?? string.Empty)}\", Value = {value}, IsNull = {literal.IsNull.ToString().ToLowerInvariant()},");
            if (literal.EnumTypeId is not null)
            {
                source.AppendLine($"{indent}                EnumTypeId = new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(literal.EnumTypeId.Value.Value)}\"), EnumMemberName = \"{EscapeString(literal.EnumMemberName!)}\",");
            }
            source.AppendLine($"{indent}            }},");
            source.AppendLine($"{indent}            Message = {Literal(constraint.Message)},");
            source.AppendLine($"{indent}        }},");
        }
        source.AppendLine($"{indent}    ],");
        source.AppendLine($"{indent}}},");
    }

    private static string? IntegerLiteral(IReadOnlyDictionary<string, string> annotations, string key)
    {
        return int.TryParse(GetAnnotationValue(annotations, key), out int value) ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
    }

    private static string? DecimalLiteral(IReadOnlyDictionary<string, string> annotations, string key)
    {
        return decimal.TryParse(GetAnnotationValue(annotations, key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decimal value)
            ? value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m"
            : null;
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
        source.AppendLine($"{indent}    Format = {Literal(descriptor.Format)},");
        source.AppendLine($"{indent}}}");
    }

    private static void AppendStrongScalarType(StringBuilder source, DotNetStrongScalarTypeDescriptor descriptor, int indentationLevel)
    {
        string indent = new(' ', indentationLevel * 4);
        source.AppendLine($"{indent}new global::SemanticTypeModel.Abstractions.Model.StrongScalarTypeDefinition");
        source.AppendLine($"{indent}{{");
        AppendCommonTypeMembers(source, descriptor.Id, descriptor.Name, "StrongScalar", false, descriptor.Annotations, indentationLevel + 1);
        source.AppendLine($"{indent}    ValueType = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(descriptor.ValueTypeId)}\")),");
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
            source.AppendLine($"{indent}        new global::SemanticTypeModel.Abstractions.Model.EnumValueDefinition {{ Name = \"{EscapeString(value.Name)}\", Value = \"{EscapeString(value.Name)}\", DisplayName = {Literal(value.DisplayName)}, UserDescription = {Literal(value.UserDescription)}, Annotations = new global::SemanticTypeModel.Abstractions.Model.AnnotationBag() }},");
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
        source.AppendLine($"{indent}    KeyType = new global::SemanticTypeModel.Abstractions.Model.TypeRef(new global::SemanticTypeModel.Abstractions.Model.TypeId(\"{EscapeString(descriptor.KeyTypeId)}\")),");
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
        source.AppendLine($"{indent}UserDescription = {Literal(GetAnnotationValue(annotations, "schema.userDescription"))},");
        source.AppendLine($"{indent}TechnicalDescription = {Literal(GetAnnotationValue(annotations, "schema.technicalDescription"))},");
        source.AppendLine($"{indent}Kind = global::SemanticTypeModel.Abstractions.Model.TypeKind.{kind},");
        source.AppendLine($"{indent}Nullability = global::SemanticTypeModel.Abstractions.Model.Nullability.{(allowsNull ? "Nullable" : "NonNullable")},");
        AppendAnnotationBag(source, annotations, indentationLevel, "Annotations");
    }

    private static string? GetAnnotationValue(IReadOnlyDictionary<string, string> annotations, string key)
    {
        return annotations.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }

    private static string Literal(string? value)
    {
        return value is null ? "null" : $"\"{EscapeString(value)}\"";
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
