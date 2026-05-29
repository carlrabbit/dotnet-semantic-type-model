using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace SemanticTypeModel.DotNet;

/// <summary>
/// Extracts .NET type-system information from Roslyn symbols into canonical type descriptors.
/// </summary>
public sealed class RoslynDotNetTypeExtractor
{
    private const string SemanticTypeAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticTypeAttribute";
    private const string SemanticIgnoreAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticIgnoreAttribute";
    private const string SemanticNameAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticNameAttribute";
    private const string SemanticDescriptionAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticDescriptionAttribute";
    private const string SemanticDisplayNameAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticDisplayNameAttribute";
    private const string SemanticCategoryAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticCategoryAttribute";
    private const string SemanticOrderAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticOrderAttribute";
    private const string SemanticRoleAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticRoleAttribute";
    private const string SemanticFormatAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticFormatAttribute";
    private const string SemanticStringConstraintsAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticStringConstraintsAttribute";
    private const string SemanticNumericConstraintsAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticNumericConstraintsAttribute";
    private const string SemanticCollectionConstraintsAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticCollectionConstraintsAttribute";
    private const string SemanticEnumValueAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticEnumValueAttribute";
    private const string SemanticAnnotationAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticAnnotationAttribute";
    private const string SemanticKeyAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticKeyAttribute";
    private const string SemanticRelationshipAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticRelationshipAttribute";
    private const string GeneratorOptionsAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticTypeModelGeneratorOptionsAttribute";
    private const string JsonPropertyNameAttributeMetadataName = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
    private const string JsonIgnoreAttributeMetadataName = "System.Text.Json.Serialization.JsonIgnoreAttribute";
    private const string JsonIncludeAttributeMetadataName = "System.Text.Json.Serialization.JsonIncludeAttribute";
    private const string JsonConverterAttributeMetadataName = "System.Text.Json.Serialization.JsonConverterAttribute";
    private const string JsonNumberHandlingAttributeMetadataName = "System.Text.Json.Serialization.JsonNumberHandlingAttribute";
    private const string JsonRequiredAttributeMetadataName = "System.Text.Json.Serialization.JsonRequiredAttribute";
    private const string JsonExtensionDataAttributeMetadataName = "System.Text.Json.Serialization.JsonExtensionDataAttribute";
    private const string JsonObjectCreationHandlingAttributeMetadataName = "System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute";
    private const string JsonUnmappedMemberHandlingAttributeMetadataName = "System.Text.Json.Serialization.JsonUnmappedMemberHandlingAttribute";
    private const string JsonPolymorphicAttributeMetadataName = "System.Text.Json.Serialization.JsonPolymorphicAttribute";
    private const string JsonDerivedTypeAttributeMetadataName = "System.Text.Json.Serialization.JsonDerivedTypeAttribute";

    private readonly Dictionary<string, DotNetTypeDescriptor> _types = new(StringComparer.Ordinal);
    private readonly List<DotNetExtractionDiagnostic> _diagnostics = [];
    private readonly HashSet<string> _inProgress = new(StringComparer.Ordinal);
    private DotNetExtractionOptions _options = DotNetExtractionOptions.Default;

    /// <summary>
    /// Extracts semantic type descriptors from a Roslyn compilation.
    /// </summary>
    /// <param name="compilation">The input compilation.</param>
    /// <param name="options">Optional extraction options.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The extraction result.</returns>
    public DotNetExtractionResult Extract(
        Compilation compilation,
        DotNetExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        options = MergeOptions(compilation, options ?? DotNetExtractionOptions.Default);
        _options = options;

        _types.Clear();
        _diagnostics.Clear();
        _inProgress.Clear();

        if (!Enum.IsDefined(options.NamingPolicy))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5018",
                $"Naming policy '{options.NamingPolicy}' is not supported.",
                compilation.Assembly.Locations.FirstOrDefault()));
        }

        INamedTypeSymbol? semanticTypeAttribute = compilation.GetTypeByMetadataName(SemanticTypeAttributeMetadataName);
        INamedTypeSymbol? semanticIgnoreAttribute = compilation.GetTypeByMetadataName(SemanticIgnoreAttributeMetadataName);

        if (semanticTypeAttribute is null)
        {
            return new DotNetExtractionResult
            {
                TypesById = _types,
                Diagnostics = _diagnostics,
                Options = options,
            };
        }

        if (options.DiscoveryMode == DotNetTypeDiscoveryMode.Namespace && options.IncludedNamespaces.Count == 0)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5009",
                "Namespace discovery mode requires at least one included namespace.",
                compilation.Assembly.Locations.FirstOrDefault()));
        }

        List<INamedTypeSymbol> roots = FindRootTypes(compilation.Assembly.GlobalNamespace, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);
        foreach (INamedTypeSymbol root in roots)
        {
            ExtractType(root, cancellationToken);
        }

        string? rootTypeId = null;
        foreach (INamedTypeSymbol root in roots)
        {
            string candidateId = GetTypeId(root);
            if (_types.ContainsKey(candidateId))
            {
                rootTypeId = candidateId;
                break;
            }
        }

        return new DotNetExtractionResult
        {
            TypesById = new Dictionary<string, DotNetTypeDescriptor>(_types, StringComparer.Ordinal),
            Diagnostics = [.. _diagnostics],
            RootTypeId = rootTypeId,
            Options = options,
        };
    }

    private DotNetExtractionOptions MergeOptions(Compilation compilation, DotNetExtractionOptions fallback)
    {
        INamedTypeSymbol? optionsAttribute = compilation.GetTypeByMetadataName(GeneratorOptionsAttributeMetadataName);
        if (optionsAttribute is null)
        {
            return fallback;
        }

        AttributeData? assemblyOptions = compilation.Assembly.GetAttributes().FirstOrDefault(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, optionsAttribute));
        if (assemblyOptions is null)
        {
            return fallback;
        }

        string? generatedNamespace = assemblyOptions.ConstructorArguments.Length > 0 ? assemblyOptions.ConstructorArguments[0].Value as string : null;
        string? providerName = assemblyOptions.ConstructorArguments.Length > 1 ? assemblyOptions.ConstructorArguments[1].Value as string : null;

        bool includeInternal = fallback.IncludeInternalTypes;
        bool includeInternalMembers = fallback.IncludeInternalMembers;
        bool requireXml = fallback.RequireXmlDocumentation;
        bool includeXml = fallback.IncludeXmlDocumentation;
        bool inferKeys = fallback.InferKeys;
        bool inferRelationships = fallback.InferRelationships;
        DotNetTypeDiscoveryMode discoveryMode = fallback.DiscoveryMode;
        DotNetNamingPolicy namingPolicy = fallback.NamingPolicy;
        IReadOnlyList<string> includedNamespaces = fallback.IncludedNamespaces;
        IReadOnlyList<string> excludedNamespaces = fallback.ExcludedNamespaces;
        SystemTextJsonExtractionOptions systemTextJson = fallback.SystemTextJson;

        foreach ((string? key, TypedConstant value) in assemblyOptions.NamedArguments)
        {
            if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.IncludeInternalTypes), StringComparison.Ordinal))
            {
                includeInternal = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.RequireXmlDocumentation), StringComparison.Ordinal))
            {
                requireXml = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.IncludeXmlDocumentation), StringComparison.Ordinal))
            {
                includeXml = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.InferKeys), StringComparison.Ordinal))
            {
                inferKeys = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.InferRelationships), StringComparison.Ordinal))
            {
                inferRelationships = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.IncludeInternalMembers), StringComparison.Ordinal))
            {
                includeInternalMembers = value.Value is bool boolValue && boolValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.ImportSystemTextJsonAttributes), StringComparison.Ordinal))
            {
                systemTextJson = systemTextJson with { ImportAttributes = value.Value is bool boolValue && boolValue };
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.UseJsonPropertyNameAsSemanticName), StringComparison.Ordinal))
            {
                systemTextJson = systemTextJson with { UseJsonPropertyNameAsSemanticName = value.Value is bool boolValue && boolValue };
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.GenerateSystemTextJsonContext), StringComparison.Ordinal))
            {
                systemTextJson = systemTextJson with { GenerateJsonSerializerContext = value.Value is bool boolValue && boolValue };
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.SystemTextJsonContextName), StringComparison.Ordinal)
                && value.Value is string contextName)
            {
                systemTextJson = systemTextJson with { GeneratedContextName = contextName };
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.DiscoveryMode), StringComparison.Ordinal)
                && value.Value is int discoveryModeValue
                && Enum.IsDefined(typeof(DotNetTypeDiscoveryMode), discoveryModeValue))
            {
                discoveryMode = (DotNetTypeDiscoveryMode)discoveryModeValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.NamingPolicy), StringComparison.Ordinal)
                && value.Value is int namingPolicyValue
                && Enum.IsDefined(typeof(DotNetNamingPolicy), namingPolicyValue))
            {
                namingPolicy = (DotNetNamingPolicy)namingPolicyValue;
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.IncludedNamespaces), StringComparison.Ordinal)
                && value.Value is string includeText)
            {
                includedNamespaces = ParseDelimitedList(includeText);
            }
            else if (string.Equals(key, nameof(SemanticTypeModelGeneratorOptionsAttribute.ExcludedNamespaces), StringComparison.Ordinal)
                && value.Value is string excludeText)
            {
                excludedNamespaces = ParseDelimitedList(excludeText);
            }
        }

        return fallback with
        {
            GeneratedNamespace = string.IsNullOrWhiteSpace(generatedNamespace) ? fallback.GeneratedNamespace : generatedNamespace!,
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? fallback.ProviderName : providerName!,
            IncludeInternalTypes = includeInternal,
            IncludeInternalMembers = includeInternalMembers,
            RequireXmlDocumentation = requireXml,
            IncludeXmlDocumentation = includeXml,
            InferKeys = inferKeys,
            InferRelationships = inferRelationships,
            DiscoveryMode = discoveryMode,
            NamingPolicy = namingPolicy,
            IncludedNamespaces = includedNamespaces,
            ExcludedNamespaces = excludedNamespaces,
            SystemTextJson = systemTextJson,
        };
    }

    private List<INamedTypeSymbol> FindRootTypes(
        INamespaceSymbol scope,
        INamedTypeSymbol semanticTypeAttribute,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        var roots = new List<INamedTypeSymbol>();
        switch (options.DiscoveryMode)
        {
            case DotNetTypeDiscoveryMode.ExplicitAttributes:
            case DotNetTypeDiscoveryMode.ReachableFromRoots:
                CollectTypes(scope, roots, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);
                break;
            case DotNetTypeDiscoveryMode.Namespace:
                CollectConventionTypes(scope, roots, semanticIgnoreAttribute, options, cancellationToken);
                break;
            case DotNetTypeDiscoveryMode.AssemblyPublicTypes:
                CollectAssemblyPublicTypes(scope, roots, semanticIgnoreAttribute, options, cancellationToken);
                break;
            default:
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5008",
                    $"Discovery mode '{options.DiscoveryMode}' is not supported.",
                    scope.Locations.FirstOrDefault()));
                break;
        }

        roots.Sort(static (left, right) => string.CompareOrdinal(
            left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return roots;
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

    private static void CollectTypes(
        INamespaceSymbol scope,
        List<INamedTypeSymbol> roots,
        INamedTypeSymbol semanticTypeAttribute,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (INamedTypeSymbol type in scope.GetTypeMembers())
        {
            if (HasAttribute(type, semanticTypeAttribute)
                && !HasAttribute(type, semanticIgnoreAttribute)
                && (type.DeclaredAccessibility == Accessibility.Public || (options.IncludeInternalTypes && type.DeclaredAccessibility == Accessibility.Internal)))
            {
                roots.Add(type);
            }

            CollectNestedTypes(type, roots, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);
        }

        foreach (INamespaceSymbol child in scope.GetNamespaceMembers())
        {
            CollectTypes(child, roots, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);
        }
    }

    private static void CollectNestedTypes(
        INamedTypeSymbol parent,
        List<INamedTypeSymbol> roots,
        INamedTypeSymbol semanticTypeAttribute,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (INamedTypeSymbol nested in parent.GetTypeMembers())
        {
            if (HasAttribute(nested, semanticTypeAttribute)
                && !HasAttribute(nested, semanticIgnoreAttribute)
                && (nested.DeclaredAccessibility == Accessibility.Public || (options.IncludeInternalTypes && nested.DeclaredAccessibility == Accessibility.Internal)))
            {
                roots.Add(nested);
            }

            CollectNestedTypes(nested, roots, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);
        }
    }

    private void CollectConventionTypes(
        INamespaceSymbol scope,
        List<INamedTypeSymbol> roots,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (INamedTypeSymbol type in scope.GetTypeMembers())
        {
            TryCollectConventionType(type, roots, semanticIgnoreAttribute, options, cancellationToken);
        }

        foreach (INamespaceSymbol child in scope.GetNamespaceMembers())
        {
            CollectConventionTypes(child, roots, semanticIgnoreAttribute, options, cancellationToken);
        }
    }

    private void CollectAssemblyPublicTypes(
        INamespaceSymbol scope,
        List<INamedTypeSymbol> roots,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (INamedTypeSymbol type in scope.GetTypeMembers())
        {
            if (!IsSupportedRootType(type))
            {
                continue;
            }

            if (HasAttribute(type, semanticIgnoreAttribute))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5010",
                    $"Type '{type.ToDisplayString()}' was discovered by convention but ignored by [SemanticIgnore].",
                    type.Locations.FirstOrDefault()));
                continue;
            }

            if (type.DeclaredAccessibility == Accessibility.Public || (options.IncludeInternalTypes && type.DeclaredAccessibility == Accessibility.Internal))
            {
                roots.Add(type);
            }
        }

        foreach (INamespaceSymbol child in scope.GetNamespaceMembers())
        {
            CollectAssemblyPublicTypes(child, roots, semanticIgnoreAttribute, options, cancellationToken);
        }
    }

    private void TryCollectConventionType(
        INamedTypeSymbol type,
        List<INamedTypeSymbol> roots,
        INamedTypeSymbol? semanticIgnoreAttribute,
        DotNetExtractionOptions options,
        CancellationToken cancellationToken)
    {
        if (!IsSupportedRootType(type))
        {
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                TryCollectConventionType(nested, roots, semanticIgnoreAttribute, options, cancellationToken);
            }

            return;
        }

        if (!IsNamespaceIncluded(type.ContainingNamespace, options))
        {
            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            {
                TryCollectConventionType(nested, roots, semanticIgnoreAttribute, options, cancellationToken);
            }

            return;
        }

        if (HasAttribute(type, semanticIgnoreAttribute))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5010",
                $"Type '{type.ToDisplayString()}' was discovered by convention but ignored by [SemanticIgnore].",
                type.Locations.FirstOrDefault()));
            return;
        }

        if (type.DeclaredAccessibility == Accessibility.Public || (options.IncludeInternalTypes && type.DeclaredAccessibility == Accessibility.Internal))
        {
            roots.Add(type);
        }

        foreach (INamedTypeSymbol nested in type.GetTypeMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryCollectConventionType(nested, roots, semanticIgnoreAttribute, options, cancellationToken);
        }
    }

    private static bool IsSupportedRootType(INamedTypeSymbol type)
    {
        return type.TypeKind is TypeKind.Class or TypeKind.Struct or TypeKind.Enum;
    }

    private static bool IsNamespaceIncluded(INamespaceSymbol? namespaceSymbol, DotNetExtractionOptions options)
    {
        string namespaceName = namespaceSymbol?.ToDisplayString() ?? string.Empty;
        bool included = options.IncludedNamespaces.Any(prefix =>
            string.Equals(namespaceName, prefix, StringComparison.Ordinal)
            || namespaceName.StartsWith(prefix + ".", StringComparison.Ordinal));

        if (!included)
        {
            return false;
        }

        bool excluded = options.ExcludedNamespaces.Any(prefix =>
            string.Equals(namespaceName, prefix, StringComparison.Ordinal)
            || namespaceName.StartsWith(prefix + ".", StringComparison.Ordinal));

        return !excluded;
    }

    private void ExtractType(ITypeSymbol symbol, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        (ITypeSymbol normalizedType, _) = NormalizeNullability(symbol);
        string id = GetTypeId(normalizedType);
        if (_types.ContainsKey(id) || !_inProgress.Add(id))
        {
            return;
        }

        try
        {
            switch (normalizedType)
            {
                case IArrayTypeSymbol arrayType:
                    if (arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                    {
                        _types[id] = new DotNetScalarTypeDescriptor
                        {
                            Id = id,
                            Name = "ByteArray",
                            ScalarKind = DotNetScalarKind.Binary,
                        };
                        break;
                    }

                    string itemTypeId = GetNormalizedTypeId(arrayType.ElementType, out _);
                    ExtractType(arrayType.ElementType, cancellationToken);
                    _types[id] = new DotNetArrayTypeDescriptor { Id = id, Name = id, ItemTypeId = itemTypeId };
                    break;
                case INamedTypeSymbol namedType:
                    if (TryExtractScalar(namedType, id, out DotNetTypeDescriptor? scalar))
                    {
                        _types[id] = scalar!;
                        break;
                    }

                    if (TryExtractDictionary(namedType, id, cancellationToken, out DotNetDictionaryTypeDescriptor? dictionary))
                    {
                        _types[id] = dictionary!;
                        break;
                    }

                    if (TryExtractCollection(namedType, id, cancellationToken, out DotNetArrayTypeDescriptor? collection))
                    {
                        _types[id] = collection!;
                        break;
                    }

                    if (namedType.TypeKind == TypeKind.Enum)
                    {
                        _types[id] = ExtractEnum(namedType, id);
                        break;
                    }

                    if (namedType.IsGenericType && namedType.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter))
                    {
                        _diagnostics.Add(new DotNetExtractionDiagnostic(
                            "STM5004",
                            $"Open generic type '{namedType.ToDisplayString()}' is not supported.",
                            namedType.Locations.FirstOrDefault()));
                        break;
                    }

                    _types[id] = ExtractObject(namedType, id, cancellationToken);
                    break;
                default:
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5003",
                        $"Unsupported type shape '{normalizedType.TypeKind}' for '{normalizedType.ToDisplayString()}'.",
                        normalizedType.Locations.FirstOrDefault()));
                    break;
            }
        }
        finally
        {
            _ = _inProgress.Remove(id);
        }
    }

    private DotNetObjectTypeDescriptor ExtractObject(INamedTypeSymbol type, string id, CancellationToken cancellationToken)
    {
        var properties = new List<DotNetPropertyDescriptor>();
        var annotations = new Dictionary<string, string>(StringComparer.Ordinal);
        ImmutableArray<AttributeData> typeAttributes = type.GetAttributes();

        TryAddNameAndDescriptionAnnotations(typeAttributes, annotations, type);
        TryAddDisplayCategoryOrderAnnotations(typeAttributes, annotations, type);
        TryAddCustomAnnotations(typeAttributes, annotations, type);
        TryAddXmlDescriptionAnnotation(type, typeAttributes, annotations);
        TryAddSemanticTypeOverrides(typeAttributes, annotations);
        TryAddSystemTextJsonTypeAnnotations(typeAttributes, annotations, type);
        TryAddRoleAnnotation(typeAttributes, annotations, type.Locations.FirstOrDefault());
        ValidateTypeAttributeUsage(typeAttributes, type);
        AddInheritanceAnnotations(type, annotations, cancellationToken);
        DiagnoseMissingXmlDocumentationIfRequired(type, type.Locations.FirstOrDefault());

        string expectedPrimaryKeyName = type.Name + "Id";
        var conventionPrimaryKeyCandidates = new List<string>();
        var relationshipCandidates = new List<(string PropertyName, string TargetTypeId)>();
        var seenMemberNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var compositeKeyGroups = new Dictionary<string, List<(string PropertyName, int? Order)>>(StringComparer.Ordinal);

        foreach (ISymbol member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IPropertySymbol property || !ShouldIncludeProperty(property, _options))
            {
                continue;
            }

            (ITypeSymbol memberType, bool allowsNull) = NormalizeNullability(property.Type, property.NullableAnnotation);
            string typeId = GetTypeId(memberType);
            ExtractType(memberType, cancellationToken);

            var memberAnnotations = new Dictionary<string, string>(StringComparer.Ordinal);
            ImmutableArray<AttributeData> memberAttributes = property.GetAttributes();
            string propertyName = GetPropertyName(property);
            TryAddSystemTextJsonAnnotations(memberAttributes, memberAnnotations, property, ref propertyName);
            TryAddNameAndDescriptionAnnotations(memberAttributes, memberAnnotations, property);
            TryAddDisplayCategoryOrderAnnotations(memberAttributes, memberAnnotations, property);
            TryAddCustomAnnotations(memberAttributes, memberAnnotations, property);
            TryAddFormatAndConstraintAnnotations(memberAttributes, memberType, memberAnnotations, property);
            TryAddXmlDescriptionAnnotation(property, memberAttributes, memberAnnotations);
            ValidateMemberAttributeUsage(memberAttributes, property);
            DiagnoseMissingXmlDocumentationIfRequired(property, property.Locations.FirstOrDefault());

            if (!TryAddKeyAnnotations(memberAttributes, property, memberAnnotations, compositeKeyGroups))
            {
                if (_options.InferKeys
                    && (string.Equals(property.Name, "Id", StringComparison.Ordinal)
                        || string.Equals(property.Name, expectedPrimaryKeyName, StringComparison.Ordinal)))
                {
                    conventionPrimaryKeyCandidates.Add(propertyName);
                }
            }

            if (!TryAddRelationshipAnnotations(memberAttributes, memberType, memberAnnotations))
            {
                string? targetTypeId = null;
                bool inferredRelationship = _options.InferRelationships && TryInferRelationship(property, memberType, memberAnnotations, out targetTypeId);
                if (inferredRelationship && targetTypeId is not null)
                {
                    relationshipCandidates.Add((property.Name, targetTypeId));
                }
            }

            if (!seenMemberNames.TryAdd(propertyName, property.Name))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5006",
                    $"Duplicate semantic property name '{propertyName}' detected on type '{type.ToDisplayString()}'.",
                    property.Locations.FirstOrDefault()));
            }

            properties.Add(new DotNetPropertyDescriptor
            {
                Name = propertyName,
                TypeId = typeId,
                IsRequired = property.IsRequired,
                IsNullable = allowsNull,
                Annotations = memberAnnotations,
            });
        }

        if (_options.InferKeys && !properties.Any(static property => property.Annotations.ContainsKey("schema.key")))
        {
            if (conventionPrimaryKeyCandidates.Count == 1)
            {
                DotNetPropertyDescriptor property = properties.Single(candidate => string.Equals(candidate.Name, conventionPrimaryKeyCandidates[0], StringComparison.Ordinal));
                if (property.Annotations is Dictionary<string, string> dictionary)
                {
                    dictionary["schema.key"] = "true";
                    dictionary["schema.key.kind"] = KeyKind.Primary.ToString();
                }
            }
            else if (conventionPrimaryKeyCandidates.Count > 1)
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5013",
                    $"Type '{type.ToDisplayString()}' has ambiguous key inference candidates: {string.Join(", ", conventionPrimaryKeyCandidates.OrderBy(static value => value, StringComparer.Ordinal))}.",
                    type.Locations.FirstOrDefault()));
            }
        }

        if (_options.InferRelationships && relationshipCandidates.Count > 1)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5014",
                $"Type '{type.ToDisplayString()}' has ambiguous relationship inference candidates: {string.Join(", ", relationshipCandidates.Select(static candidate => candidate.PropertyName).OrderBy(static value => value, StringComparer.Ordinal))}.",
                type.Locations.FirstOrDefault()));
        }

        foreach ((string _, string targetTypeId) in relationshipCandidates)
        {
            if (!_types.ContainsKey(targetTypeId))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5015",
                    $"Relationship target '{targetTypeId}' is not included in the extracted model.",
                    type.Locations.FirstOrDefault()));
            }
        }

        foreach (DotNetPropertyDescriptor property in properties)
        {
            if (property.Annotations.TryGetValue("schema.relationship.target", out string? explicitTarget)
                    && !_types.ContainsKey(explicitTarget))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5015",
                    $"Relationship target '{explicitTarget}' is not included in the extracted model.",
                    type.Locations.FirstOrDefault()));
            }
        }

        ValidateCompositeKeyGroups(type, compositeKeyGroups);

        properties.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        return new DotNetObjectTypeDescriptor
        {
            Id = id,
            Name = GetTypeDisplayName(type),
            Properties = properties,
            Annotations = annotations,
        };
    }


    private void TryAddSystemTextJsonTypeAnnotations(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations, INamedTypeSymbol type)
    {
        if (!_options.SystemTextJson.ImportAttributes)
        {
            return;
        }

        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, JsonConverterAttributeMetadataName, StringComparison.Ordinal)
                && _options.SystemTextJson.PreserveUnsupportedConverterMetadata)
            {
                annotations["systemTextJson.converter"] = GetAttributeTypeArgument(attribute) ?? attribute.AttributeClass?.ToDisplayString() ?? string.Empty;
            }
            else if (string.Equals(metadataName, JsonNumberHandlingAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.numberHandling"] = GetFirstConstructorArgument(attribute);
            }
            else if (string.Equals(metadataName, JsonObjectCreationHandlingAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.objectCreationHandling"] = GetFirstConstructorArgument(attribute);
            }
            else if (string.Equals(metadataName, JsonUnmappedMemberHandlingAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.unmappedMemberHandling"] = GetFirstConstructorArgument(attribute);
            }
            else if (string.Equals(metadataName, JsonPolymorphicAttributeMetadataName, StringComparison.Ordinal)
                || string.Equals(metadataName, JsonDerivedTypeAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.polymorphism"] = "true";
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STJ008",
                    $"System.Text.Json polymorphism metadata on type '{type.ToDisplayString()}' is preserved but cannot be represented in the canonical semantic model.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault()));
            }
        }
    }

    private void TryAddSystemTextJsonAnnotations(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations, IPropertySymbol property, ref string propertyName)
    {
        if (!_options.SystemTextJson.ImportAttributes)
        {
            return;
        }

        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, JsonPropertyNameAttributeMetadataName, StringComparison.Ordinal))
            {
                string jsonName = GetFirstConstructorArgument(attribute);
                if (_options.SystemTextJson.UseJsonPropertyNameAsSerializationName)
                {
                    annotations["systemTextJson.propertyName"] = jsonName;
                }

                if (_options.SystemTextJson.UseJsonPropertyNameAsSemanticName)
                {
                    if (annotations.TryGetValue("schema.title", out string? semanticName)
                        && !string.Equals(semanticName, jsonName, StringComparison.Ordinal))
                    {
                        _diagnostics.Add(new DotNetExtractionDiagnostic(
                            "STJ001",
                            $"JsonPropertyName '{jsonName}' conflicts with explicit semantic name '{semanticName}' on member '{property.Name}'.",
                            attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
                    }

                    propertyName = jsonName;
                }
            }
            else if (string.Equals(metadataName, JsonIgnoreAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.ignore"] = "true";
                if (TryGetNamedArgument(attribute, "Condition", out string? condition))
                {
                    annotations["systemTextJson.ignoreCondition"] = condition ?? string.Empty;
                }
                else if (attribute.ConstructorArguments.Length > 0)
                {
                    annotations["systemTextJson.ignoreCondition"] = GetFirstConstructorArgument(attribute);
                }

                if (property.IsRequired)
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STJ003",
                        $"JsonIgnore conflicts with required semantic member '{property.Name}'.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
                }
            }
            else if (string.Equals(metadataName, JsonIncludeAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.include"] = "true";
            }
            else if (string.Equals(metadataName, JsonConverterAttributeMetadataName, StringComparison.Ordinal))
            {
                if (_options.SystemTextJson.PreserveUnsupportedConverterMetadata)
                {
                    annotations["systemTextJson.converter"] = GetAttributeTypeArgument(attribute) ?? attribute.AttributeClass?.ToDisplayString() ?? string.Empty;
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STJ002",
                        $"JsonConverter metadata on member '{property.Name}' cannot be represented without preserving converter metadata.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
                }
            }
            else if (string.Equals(metadataName, JsonNumberHandlingAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.numberHandling"] = GetFirstConstructorArgument(attribute);
            }
            else if (string.Equals(metadataName, JsonRequiredAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.required"] = "true";
                if (!property.IsRequired || property.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STJ006",
                        $"JsonRequired conflicts with optional or nullable semantic member '{property.Name}'.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
                }
            }
            else if (string.Equals(metadataName, JsonExtensionDataAttributeMetadataName, StringComparison.Ordinal))
            {
                annotations["systemTextJson.extensionData"] = "true";
                if (!IsSupportedJsonExtensionDataType(property.Type))
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STJ007",
                        $"JsonExtensionData member '{property.Name}' must be assignable to IDictionary<string, JsonElement> or IDictionary<string, object>.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
                }
            }
        }
    }

    private static bool IsSupportedJsonExtensionDataType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        INamedTypeSymbol? dictionary = FindImplementedDictionary(namedType);
        if (dictionary is null || dictionary.TypeArguments.Length != 2)
        {
            return false;
        }

        return dictionary.TypeArguments[0].SpecialType == SpecialType.System_String
            && (dictionary.TypeArguments[1].SpecialType == SpecialType.System_Object
                || string.Equals(dictionary.TypeArguments[1].ToDisplayString(), "System.Text.Json.JsonElement", StringComparison.Ordinal));
    }

    private static string GetFirstConstructorArgument(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return string.Empty;
        }

        object? value = attribute.ConstructorArguments[0].Value;
        return value switch
        {
            null => string.Empty,
            ITypeSymbol type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }

    private static string? GetAttributeTypeArgument(AttributeData attribute)
    {
        foreach (TypedConstant argument in attribute.ConstructorArguments)
        {
            if (argument.Value is ITypeSymbol type)
            {
                return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return null;
    }

    private static bool TryGetNamedArgument(AttributeData attribute, string name, out string? value)
    {
        foreach ((string? key, TypedConstant constant) in attribute.NamedArguments)
        {
            if (string.Equals(key, name, StringComparison.Ordinal))
            {
                value = Convert.ToString(constant.Value, CultureInfo.InvariantCulture);
                return true;
            }
        }

        value = null;
        return false;
    }

    private void AddInheritanceAnnotations(INamedTypeSymbol type, Dictionary<string, string> annotations, CancellationToken cancellationToken)
    {
        if (type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
        {
            annotations["dotnet.baseType"] = GetTypeId(baseType);
            ExtractType(baseType, cancellationToken);
        }

        if (type.Interfaces.Length == 0)
        {
            return;
        }

        string[] interfaces =
        [
            .. type.Interfaces
                .Select(GetTypeId)
                .OrderBy(static i => i, StringComparer.Ordinal),
        ];
        annotations["dotnet.interfaces"] = "[" + string.Join(",", interfaces.Select(static i => $"\"{EscapeString(i)}\"")) + "]";

        foreach (INamedTypeSymbol @interface in type.Interfaces)
        {
            ExtractType(@interface, cancellationToken);
        }
    }

    private DotNetEnumTypeDescriptor ExtractEnum(INamedTypeSymbol enumType, string id)
    {
        var values = new List<DotNetEnumValueDescriptor>();
        var seenNumeric = new Dictionary<long, string>();
        var annotations = new Dictionary<string, string>(StringComparer.Ordinal);
        ImmutableArray<AttributeData> typeAttributes = enumType.GetAttributes();
        TryAddNameAndDescriptionAnnotations(typeAttributes, annotations, enumType);
        TryAddDisplayCategoryOrderAnnotations(typeAttributes, annotations, enumType);
        TryAddCustomAnnotations(typeAttributes, annotations, enumType);
        TryAddXmlDescriptionAnnotation(enumType, typeAttributes, annotations);
        TryAddSystemTextJsonTypeAnnotations(typeAttributes, annotations, enumType);
        ValidateTypeAttributeUsage(typeAttributes, enumType);
        DiagnoseMissingXmlDocumentationIfRequired(enumType, enumType.Locations.FirstOrDefault());

        var enumDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var enumDescriptions = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>().Where(static f => f.HasConstantValue))
        {
            if (field.ConstantValue is null)
            {
                continue;
            }

            long numericValue = Convert.ToInt64(field.ConstantValue, CultureInfo.InvariantCulture);
            string valueName = GetEnumValueName(field);
            TryAddEnumValueMetadata(field, valueName, enumDisplayNames, enumDescriptions);
            values.Add(new DotNetEnumValueDescriptor { Name = valueName, NumericValue = numericValue });

            if (seenNumeric.TryGetValue(numericValue, out string? firstName))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5011",
                    $"Enum '{enumType.Name}' has duplicate numeric value '{numericValue}' for '{firstName}' and '{valueName}'.",
                    field.Locations.FirstOrDefault()));
            }
            else
            {
                seenNumeric[numericValue] = valueName;
            }
        }

        values.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));
        AddEnumValueMetadataAnnotations(annotations, enumDisplayNames, enumDescriptions);

        return new DotNetEnumTypeDescriptor
        {
            Id = id,
            Name = GetTypeDisplayName(enumType),
            Values = values,
            Annotations = annotations,
        };
    }

    private bool TryExtractScalar(INamedTypeSymbol type, string id, out DotNetTypeDescriptor? descriptor)
    {
        descriptor = null;

        DotNetScalarKind? scalarKind = type.SpecialType switch
        {
            SpecialType.System_Boolean => DotNetScalarKind.Boolean,
            SpecialType.System_String => DotNetScalarKind.String,
            SpecialType.System_Byte => DotNetScalarKind.Integer,
            SpecialType.System_SByte => DotNetScalarKind.Integer,
            SpecialType.System_Int16 => DotNetScalarKind.Integer,
            SpecialType.System_UInt16 => DotNetScalarKind.Integer,
            SpecialType.System_Int32 => DotNetScalarKind.Integer,
            SpecialType.System_UInt32 => DotNetScalarKind.Integer,
            SpecialType.System_Int64 => DotNetScalarKind.Integer,
            SpecialType.System_UInt64 => DotNetScalarKind.Integer,
            SpecialType.System_Single => DotNetScalarKind.Number,
            SpecialType.System_Double => DotNetScalarKind.Number,
            SpecialType.System_Decimal => DotNetScalarKind.Decimal,
            _ => null,
        };

        if (scalarKind is null)
        {
            scalarKind = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) switch
            {
                "DateOnly" => DotNetScalarKind.Date,
                "TimeOnly" => DotNetScalarKind.Time,
                "DateTime" => DotNetScalarKind.DateTime,
                "DateTimeOffset" => DotNetScalarKind.DateTimeOffset,
                "TimeSpan" => DotNetScalarKind.Duration,
                "Guid" => DotNetScalarKind.Guid,
                "JsonDocument" or "JsonElement" or "JsonNode" => DotNetScalarKind.Json,
                _ => null,
            };
        }

        if (scalarKind is null)
        {
            return false;
        }

        descriptor = new DotNetScalarTypeDescriptor
        {
            Id = id,
            Name = type.Name,
            ScalarKind = scalarKind.Value,
        };
        return true;
    }

    private bool TryExtractCollection(INamedTypeSymbol type, string id, CancellationToken cancellationToken, out DotNetArrayTypeDescriptor? descriptor)
    {
        descriptor = null;
        if (type.SpecialType == SpecialType.System_String || !type.IsGenericType || type.TypeArguments.Length != 1)
        {
            return false;
        }

        if (!ImplementsAny(
                type,
                "System.Collections.Generic.IEnumerable<T>",
                "System.Collections.Generic.IReadOnlyCollection<T>",
                "System.Collections.Generic.IReadOnlyList<T>",
                "System.Collections.Generic.ICollection<T>",
                "System.Collections.Generic.IList<T>",
                "System.Collections.Generic.List<T>",
                "System.Collections.Generic.HashSet<T>"))
        {
            return false;
        }

        ITypeSymbol itemType = type.TypeArguments[0];
        string itemTypeId = GetNormalizedTypeId(itemType, out _);
        ExtractType(itemType, cancellationToken);
        descriptor = new DotNetArrayTypeDescriptor { Id = id, Name = type.Name, ItemTypeId = itemTypeId };
        return true;
    }

    private bool TryExtractDictionary(INamedTypeSymbol type, string id, CancellationToken cancellationToken, out DotNetDictionaryTypeDescriptor? descriptor)
    {
        descriptor = null;
        INamedTypeSymbol? dictionaryType = FindImplementedDictionary(type);
        if (dictionaryType is null)
        {
            return false;
        }

        ITypeSymbol keyType = dictionaryType.TypeArguments[0];
        ITypeSymbol valueType = dictionaryType.TypeArguments[1];

        if (!IsSupportedDictionaryKey(keyType))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5007",
                $"Dictionary key type '{keyType.ToDisplayString()}' is not supported. Use string, integer, or Guid keys.",
                type.Locations.FirstOrDefault()));
        }

        string valueTypeId = GetNormalizedTypeId(valueType, out _);
        ExtractType(valueType, cancellationToken);
        descriptor = new DotNetDictionaryTypeDescriptor { Id = id, Name = type.Name, ValueTypeId = valueTypeId };
        return true;
    }

    private void ValidateMemberAttributeUsage(ImmutableArray<AttributeData> attributes, IPropertySymbol property)
    {
        var roleCount = 0;
        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticRoleAttributeMetadataName, StringComparison.Ordinal))
            {
                roleCount++;
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5001",
                    $"[SemanticRole] is not valid on property '{property.Name}'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
            }
        }

        if (roleCount > 1)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5002",
                $"Property '{property.ToDisplayString()}' has conflicting semantic role attributes.",
                property.Locations.FirstOrDefault()));
        }
    }

    private void ValidateTypeAttributeUsage(ImmutableArray<AttributeData> attributes, INamedTypeSymbol type)
    {
        var semanticTypeCount = 0;
        var semanticRoleCount = 0;
        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticKeyAttributeMetadataName, StringComparison.Ordinal)
                || string.Equals(metadataName, SemanticRelationshipAttributeMetadataName, StringComparison.Ordinal))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5001",
                    $"Attribute '{attribute.AttributeClass?.Name}' is not valid on type '{type.Name}'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault()));
            }
            else if (string.Equals(metadataName, SemanticFormatAttributeMetadataName, StringComparison.Ordinal)
                     || string.Equals(metadataName, SemanticStringConstraintsAttributeMetadataName, StringComparison.Ordinal)
                     || string.Equals(metadataName, SemanticNumericConstraintsAttributeMetadataName, StringComparison.Ordinal)
                     || string.Equals(metadataName, SemanticCollectionConstraintsAttributeMetadataName, StringComparison.Ordinal)
                     || string.Equals(metadataName, SemanticEnumValueAttributeMetadataName, StringComparison.Ordinal))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5001",
                    $"Attribute '{attribute.AttributeClass?.Name}' is not valid on type '{type.Name}'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? type.Locations.FirstOrDefault()));
            }
            else if (string.Equals(metadataName, SemanticTypeAttributeMetadataName, StringComparison.Ordinal))
            {
                semanticTypeCount++;
            }
            else if (string.Equals(metadataName, SemanticRoleAttributeMetadataName, StringComparison.Ordinal))
            {
                semanticRoleCount++;
            }
        }

        if (semanticTypeCount > 1 || semanticRoleCount > 1)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5002",
                $"Type '{type.ToDisplayString()}' has conflicting semantic attributes.",
                type.Locations.FirstOrDefault()));
        }
    }

    private void TryAddFormatAnnotation(
        AttributeData attribute,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        string? format = attribute.ConstructorArguments.Length == 1
            ? GetSemanticFormatValue(attribute.ConstructorArguments[0].Value)
            : null;

        if (string.IsNullOrWhiteSpace(format))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5017",
                $"[SemanticFormat] on '{symbol.ToDisplayString()}' requires a non-empty format argument.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        (ITypeSymbol normalizedType, _) = NormalizeNullability(memberType);
        if (!IsFormatCompatibleType(normalizedType))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5025",
                $"[SemanticFormat] is not supported on '{symbol.ToDisplayString()}'. Apply it to string-like scalar members only.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        annotations["schema.format"] = format;
    }

    private void TryAddStringConstraintAnnotations(
        AttributeData attribute,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        (ITypeSymbol normalizedType, _) = NormalizeNullability(memberType);
        if (normalizedType.SpecialType != SpecialType.System_String)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5021",
                $"[SemanticStringConstraints] is only supported on string members such as '{symbol.ToDisplayString()}'.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        int minLength = -1;
        int maxLength = -1;
        string? pattern = null;
        foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
        {
            if (string.Equals(key, nameof(SemanticStringConstraintsAttribute.MinLength), StringComparison.Ordinal) && value.Value is int min)
            {
                minLength = min;
            }
            else if (string.Equals(key, nameof(SemanticStringConstraintsAttribute.MaxLength), StringComparison.Ordinal) && value.Value is int max)
            {
                maxLength = max;
            }
            else if (string.Equals(key, nameof(SemanticStringConstraintsAttribute.Pattern), StringComparison.Ordinal))
            {
                pattern = value.Value as string;
            }
        }

        if (minLength < -1 || maxLength < -1 || (minLength >= 0 && maxLength >= 0 && minLength > maxLength))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5022",
                $"[SemanticStringConstraints] on '{symbol.ToDisplayString()}' has an invalid range.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        if (minLength >= 0)
        {
            annotations["schema.minLength"] = minLength.ToString(CultureInfo.InvariantCulture);
        }

        if (maxLength >= 0)
        {
            annotations["schema.maxLength"] = maxLength.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(pattern))
        {
            annotations["schema.pattern"] = pattern!;
        }
    }

    private void TryAddNumericConstraintAnnotations(
        AttributeData attribute,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        (ITypeSymbol normalizedType, _) = NormalizeNullability(memberType);
        if (!IsNumericType(normalizedType))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5021",
                $"[SemanticNumericConstraints] is only supported on numeric members such as '{symbol.ToDisplayString()}'.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        double minimum = double.NaN;
        double maximum = double.NaN;
        double exclusiveMinimum = double.NaN;
        double exclusiveMaximum = double.NaN;
        double multipleOf = double.NaN;
        foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
        {
            if (string.Equals(key, nameof(SemanticNumericConstraintsAttribute.Minimum), StringComparison.Ordinal) && value.Value is double min)
            {
                minimum = min;
            }
            else if (string.Equals(key, nameof(SemanticNumericConstraintsAttribute.Maximum), StringComparison.Ordinal) && value.Value is double max)
            {
                maximum = max;
            }
            else if (string.Equals(key, nameof(SemanticNumericConstraintsAttribute.ExclusiveMinimum), StringComparison.Ordinal) && value.Value is double exclusiveMin)
            {
                exclusiveMinimum = exclusiveMin;
            }
            else if (string.Equals(key, nameof(SemanticNumericConstraintsAttribute.ExclusiveMaximum), StringComparison.Ordinal) && value.Value is double exclusiveMax)
            {
                exclusiveMaximum = exclusiveMax;
            }
            else if (string.Equals(key, nameof(SemanticNumericConstraintsAttribute.MultipleOf), StringComparison.Ordinal) && value.Value is double multiple)
            {
                multipleOf = multiple;
            }
        }

        if ((!double.IsNaN(minimum) && !double.IsNaN(maximum) && minimum > maximum)
            || (!double.IsNaN(exclusiveMinimum) && !double.IsNaN(exclusiveMaximum) && exclusiveMinimum >= exclusiveMaximum)
            || (!double.IsNaN(exclusiveMinimum) && !double.IsNaN(maximum) && exclusiveMinimum >= maximum)
            || (!double.IsNaN(minimum) && !double.IsNaN(exclusiveMaximum) && minimum >= exclusiveMaximum)
            || (!double.IsNaN(multipleOf) && multipleOf <= 0))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5023",
                $"[SemanticNumericConstraints] on '{symbol.ToDisplayString()}' has an invalid range.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        AddDoubleAnnotation(annotations, "schema.minimum", minimum);
        AddDoubleAnnotation(annotations, "schema.maximum", maximum);
        AddDoubleAnnotation(annotations, "schema.exclusiveMinimum", exclusiveMinimum);
        AddDoubleAnnotation(annotations, "schema.exclusiveMaximum", exclusiveMaximum);
        AddDoubleAnnotation(annotations, "schema.multipleOf", multipleOf);
    }

    private void TryAddCollectionConstraintAnnotations(
        AttributeData attribute,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        if (!TryGetCollectionItemType(memberType, out _))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5021",
                $"[SemanticCollectionConstraints] is only supported on collection members such as '{symbol.ToDisplayString()}'.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        int minItems = -1;
        int maxItems = -1;
        bool uniqueItems = false;
        foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
        {
            if (string.Equals(key, nameof(SemanticCollectionConstraintsAttribute.MinItems), StringComparison.Ordinal) && value.Value is int min)
            {
                minItems = min;
            }
            else if (string.Equals(key, nameof(SemanticCollectionConstraintsAttribute.MaxItems), StringComparison.Ordinal) && value.Value is int max)
            {
                maxItems = max;
            }
            else if (string.Equals(key, nameof(SemanticCollectionConstraintsAttribute.UniqueItems), StringComparison.Ordinal) && value.Value is bool unique)
            {
                uniqueItems = unique;
            }
        }

        if (minItems < -1 || maxItems < -1 || (minItems >= 0 && maxItems >= 0 && minItems > maxItems))
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5024",
                $"[SemanticCollectionConstraints] on '{symbol.ToDisplayString()}' has an invalid range.",
                attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
            return;
        }

        if (minItems >= 0)
        {
            annotations["schema.minItems"] = minItems.ToString(CultureInfo.InvariantCulture);
        }

        if (maxItems >= 0)
        {
            annotations["schema.maxItems"] = maxItems.ToString(CultureInfo.InvariantCulture);
        }

        if (uniqueItems)
        {
            annotations["schema.uniqueItems"] = "true";
        }
    }

    private static void AddDoubleAnnotation(Dictionary<string, string> annotations, string key, double value)
    {
        if (!double.IsNaN(value))
        {
            annotations[key] = value.ToString("G", CultureInfo.InvariantCulture);
        }
    }

    private void TryAddEnumValueMetadata(
        IFieldSymbol field,
        string valueName,
        Dictionary<string, string> displayNames,
        Dictionary<string, string> descriptions)
    {
        foreach (AttributeData attribute in field.GetAttributes())
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticEnumValueAttributeMetadataName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
            {
                if (string.Equals(key, nameof(SemanticEnumValueAttribute.DisplayName), StringComparison.Ordinal)
                    && value.Value is string displayName
                    && !string.IsNullOrWhiteSpace(displayName))
                {
                    displayNames[valueName] = displayName;
                }
                else if (string.Equals(key, nameof(SemanticEnumValueAttribute.Description), StringComparison.Ordinal)
                         && value.Value is string description
                         && !string.IsNullOrWhiteSpace(description))
                {
                    descriptions[valueName] = description;
                }
            }
        }
    }

    private static void AddEnumValueMetadataAnnotations(
        Dictionary<string, string> annotations,
        Dictionary<string, string> displayNames,
        Dictionary<string, string> descriptions)
    {
        if (displayNames.Count > 0)
        {
            annotations["dotnet.enumDisplayNames"] = JsonSerializer.Serialize(displayNames);
        }

        if (descriptions.Count > 0)
        {
            annotations["dotnet.enumDescriptions"] = JsonSerializer.Serialize(descriptions);
        }
    }

    private static bool IsValidAnnotationKey(string value)
    {
        int separatorIndex = value.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return false;
        }

        string namespacePart = value[..separatorIndex];
        string localName = value[(separatorIndex + 1)..];
        return char.IsLetter(namespacePart[0])
            && namespacePart.All(static character => char.IsLetterOrDigit(character))
            && !string.IsNullOrWhiteSpace(localName)
            && !localName.Any(char.IsWhiteSpace);
    }

    private static bool IsNumericType(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64
            or SpecialType.System_Single
            or SpecialType.System_Double
            or SpecialType.System_Decimal;
    }

    private static bool IsFormatCompatibleType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        string containingNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (!string.Equals(containingNamespace, "System", StringComparison.Ordinal))
        {
            return false;
        }

        return type.Name is "Guid" or "DateOnly" or "TimeOnly" or "DateTime" or "DateTimeOffset" or "TimeSpan";
    }

    private static string? GetSemanticFormatValue(object? value)
    {
        if (value is string text)
        {
            return text;
        }

        if (value is int enumValue && Enum.IsDefined(typeof(SemanticScalarFormat), enumValue))
        {
            return (SemanticScalarFormat)enumValue switch
            {
                SemanticScalarFormat.Email => "email",
                SemanticScalarFormat.Uri => "uri",
                SemanticScalarFormat.Hostname => "hostname",
                SemanticScalarFormat.Ipv4 => "ipv4",
                SemanticScalarFormat.Ipv6 => "ipv6",
                SemanticScalarFormat.Date => "date",
                SemanticScalarFormat.Time => "time",
                SemanticScalarFormat.DateTime => "date-time",
                SemanticScalarFormat.Duration => "duration",
                SemanticScalarFormat.Uuid => "uuid",
                _ => null,
            };
        }

        return null;
    }

    private void TryAddRoleAnnotation(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations, Location? location)
    {
        foreach (AttributeData attribute in attributes)
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticRoleAttributeMetadataName, StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string role
                && !string.IsNullOrWhiteSpace(role))
            {
                annotations["schema.role"] = role;
            }
            else if (attribute.ConstructorArguments.Length == 1
                     && attribute.ConstructorArguments[0].Value is int roleValue
                     && Enum.IsDefined(typeof(SemanticTypeRole), roleValue))
            {
                annotations["schema.role"] = ((SemanticTypeRole)roleValue).ToString();
            }
            else
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5001",
                    "[SemanticRole] requires a non-empty role argument.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? location));
            }
        }
    }

    private void TryAddNameAndDescriptionAnnotations(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations, ISymbol symbol)
    {
        var nameCount = 0;
        var descriptionCount = 0;

        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1)
            {
                nameCount++;
                if (attribute.ConstructorArguments[0].Value is string title && !string.IsNullOrWhiteSpace(title))
                {
                    annotations["schema.title"] = title;
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5017",
                        $"[SemanticName] on '{symbol.ToDisplayString()}' requires a non-empty string argument.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                }
            }

            if (string.Equals(metadataName, SemanticDescriptionAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1)
            {
                descriptionCount++;
                if (attribute.ConstructorArguments[0].Value is string description && !string.IsNullOrWhiteSpace(description))
                {
                    annotations["schema.description"] = description;
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5017",
                        $"[SemanticDescription] on '{symbol.ToDisplayString()}' requires a non-empty string argument.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                }
            }
        }

        if (nameCount > 1 || descriptionCount > 1)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5002",
                $"Symbol '{symbol.ToDisplayString()}' has conflicting semantic name/description attributes.",
                symbol.Locations.FirstOrDefault()));
        }
    }

    private void TryAddDisplayCategoryOrderAnnotations(
        ImmutableArray<AttributeData> attributes,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        var displayNameCount = 0;
        var categoryCount = 0;
        var orderCount = 0;

        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticDisplayNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1)
            {
                displayNameCount++;
                if (attribute.ConstructorArguments[0].Value is string displayName && !string.IsNullOrWhiteSpace(displayName))
                {
                    annotations["ui.title"] = displayName;
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5017",
                        $"[SemanticDisplayName] on '{symbol.ToDisplayString()}' requires a non-empty string argument.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                }
            }
            else if (string.Equals(metadataName, SemanticCategoryAttributeMetadataName, StringComparison.Ordinal)
                     && attribute.ConstructorArguments.Length == 1)
            {
                categoryCount++;
                if (attribute.ConstructorArguments[0].Value is string category && !string.IsNullOrWhiteSpace(category))
                {
                    annotations["ui.category"] = category;
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5017",
                        $"[SemanticCategory] on '{symbol.ToDisplayString()}' requires a non-empty string argument.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                }
            }
            else if (string.Equals(metadataName, SemanticOrderAttributeMetadataName, StringComparison.Ordinal)
                     && attribute.ConstructorArguments.Length == 1)
            {
                orderCount++;
                if (attribute.ConstructorArguments[0].Value is int order)
                {
                    annotations["ui.order"] = order.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5021",
                        $"[SemanticOrder] on '{symbol.ToDisplayString()}' requires an integer order argument.",
                        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                }
            }
        }

        if (displayNameCount > 1 || categoryCount > 1 || orderCount > 1)
        {
            _diagnostics.Add(new DotNetExtractionDiagnostic(
                "STM5002",
                $"Symbol '{symbol.ToDisplayString()}' has conflicting semantic display/category/order attributes.",
                symbol.Locations.FirstOrDefault()));
        }
    }

    private void TryAddCustomAnnotations(
        ImmutableArray<AttributeData> attributes,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        foreach (AttributeData attribute in attributes)
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticAnnotationAttributeMetadataName, StringComparison.Ordinal)
                || attribute.ConstructorArguments.Length != 2)
            {
                continue;
            }

            if (attribute.ConstructorArguments[0].Value is not string key
                || string.IsNullOrWhiteSpace(key)
                || !IsValidAnnotationKey(key))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5020",
                    $"[SemanticAnnotation] on '{symbol.ToDisplayString()}' requires a namespaced key in the form 'namespace.name'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                continue;
            }

            if (attribute.ConstructorArguments[1].Value is not string value || string.IsNullOrWhiteSpace(value))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5017",
                    $"[SemanticAnnotation] on '{symbol.ToDisplayString()}' requires a non-empty string value.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                continue;
            }

            if (annotations.TryGetValue(key, out string? existingValue) && !string.Equals(existingValue, value, StringComparison.Ordinal))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5002",
                    $"Symbol '{symbol.ToDisplayString()}' has conflicting values for semantic annotation '{key}'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? symbol.Locations.FirstOrDefault()));
                continue;
            }

            annotations[key] = value;
        }
    }

    private void TryAddFormatAndConstraintAnnotations(
        ImmutableArray<AttributeData> attributes,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations,
        ISymbol symbol)
    {
        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticFormatAttributeMetadataName, StringComparison.Ordinal))
            {
                TryAddFormatAnnotation(attribute, memberType, annotations, symbol);
            }
            else if (string.Equals(metadataName, SemanticStringConstraintsAttributeMetadataName, StringComparison.Ordinal))
            {
                TryAddStringConstraintAnnotations(attribute, memberType, annotations, symbol);
            }
            else if (string.Equals(metadataName, SemanticNumericConstraintsAttributeMetadataName, StringComparison.Ordinal))
            {
                TryAddNumericConstraintAnnotations(attribute, memberType, annotations, symbol);
            }
            else if (string.Equals(metadataName, SemanticCollectionConstraintsAttributeMetadataName, StringComparison.Ordinal))
            {
                TryAddCollectionConstraintAnnotations(attribute, memberType, annotations, symbol);
            }
        }
    }

    private void TryAddSemanticTypeOverrides(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations)
    {
        foreach (AttributeData attribute in attributes)
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticTypeAttributeMetadataName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
            {
                if (string.Equals(key, nameof(SemanticTypeAttribute.Name), StringComparison.Ordinal)
                    && value.Value is string name
                    && !string.IsNullOrWhiteSpace(name))
                {
                    _ = annotations.TryAdd("schema.title", name);
                }
                else if (string.Equals(key, nameof(SemanticTypeAttribute.Role), StringComparison.Ordinal)
                    && value.Value is string role
                    && !string.IsNullOrWhiteSpace(role))
                {
                    _ = annotations.TryAdd("schema.role", role);
                }
            }

            if (!annotations.ContainsKey("schema.role")
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is int roleValue
                && Enum.IsDefined(typeof(SemanticTypeRole), roleValue))
            {
                _ = annotations.TryAdd("schema.role", ((SemanticTypeRole)roleValue).ToString());
            }
        }
    }

    private void TryAddXmlDescriptionAnnotation(ISymbol symbol, ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations)
    {
        if (HasAttribute(attributes, SemanticDescriptionAttributeMetadataName))
        {
            return;
        }

        if (!_options.IncludeXmlDocumentation)
        {
            return;
        }

        string? summary = GetXmlSummary(symbol.GetDocumentationCommentXml());
        if (!string.IsNullOrWhiteSpace(summary))
        {
            annotations["schema.description"] = summary!;
        }
    }

    private string GetPropertyName(IPropertySymbol property)
    {
        foreach (AttributeData attribute in property.GetAttributes())
        {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string name
                && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return ApplyNamingPolicy(property.Name);
    }

    private bool TryAddKeyAnnotations(
        ImmutableArray<AttributeData> attributes,
        IPropertySymbol property,
        Dictionary<string, string> annotations,
        Dictionary<string, List<(string PropertyName, int? Order)>> compositeKeyGroups)
    {
        bool hasKey = false;
        foreach (AttributeData attribute in attributes)
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticKeyAttributeMetadataName, StringComparison.Ordinal))
            {
                continue;
            }

            hasKey = true;
            annotations["schema.key"] = "true";
            annotations["schema.key.kind"] = KeyKind.Primary.ToString();

            string? keyGroupName = null;
            int? order = null;
            foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
            {
                if (string.Equals(key, nameof(SemanticKeyAttribute.Name), StringComparison.Ordinal))
                {
                    keyGroupName = value.Value as string;
                }
                else if (string.Equals(key, nameof(SemanticKeyAttribute.Kind), StringComparison.Ordinal) && value.Value is int keyKindValue && Enum.IsDefined(typeof(KeyKind), keyKindValue))
                {
                    annotations["schema.key.kind"] = ((KeyKind)keyKindValue).ToString();
                }
                else if (string.Equals(key, nameof(SemanticKeyAttribute.Order), StringComparison.Ordinal) && value.Value is int orderValue)
                {
                    order = orderValue;
                    annotations["schema.key.order"] = orderValue.ToString(CultureInfo.InvariantCulture);
                }
                else if (string.Equals(key, nameof(SemanticKeyAttribute.IsGenerated), StringComparison.Ordinal) && value.Value is bool isGenerated)
                {
                    annotations["schema.key.generated"] = isGenerated ? "true" : "false";
                }
            }

            if (!string.IsNullOrWhiteSpace(keyGroupName))
            {
                annotations["schema.key.name"] = keyGroupName!;
                if (!compositeKeyGroups.TryGetValue(keyGroupName!, out List<(string PropertyName, int? Order)>? group))
                {
                    group = [];
                    compositeKeyGroups[keyGroupName!] = group;
                }

                group.Add((property.Name, order));
            }
        }

        return hasKey;
    }

    private void ValidateCompositeKeyGroups(INamedTypeSymbol type, Dictionary<string, List<(string PropertyName, int? Order)>> groups)
    {
        foreach ((string keyGroupName, List<(string PropertyName, int? Order)> members) in groups)
        {
            HashSet<int> seenOrders = [];
            bool hasMissingOrder = members.Any(static member => member.Order is null);
            foreach ((string _, int? order) in members)
            {
                if (order is null)
                {
                    continue;
                }

                if (!seenOrders.Add(order.Value))
                {
                    _diagnostics.Add(new DotNetExtractionDiagnostic(
                        "STM5016",
                        $"Composite key '{keyGroupName}' on '{type.ToDisplayString()}' has duplicate order '{order.Value}'.",
                        type.Locations.FirstOrDefault()));
                }
            }

            if (hasMissingOrder)
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5016",
                    $"Composite key '{keyGroupName}' on '{type.ToDisplayString()}' requires explicit order on all members.",
                    type.Locations.FirstOrDefault()));
            }
        }
    }

    private bool TryAddRelationshipAnnotations(
        ImmutableArray<AttributeData> attributes,
        ITypeSymbol memberType,
        Dictionary<string, string> annotations)
    {
        bool hasRelationship = false;
        foreach (AttributeData attribute in attributes)
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticRelationshipAttributeMetadataName, StringComparison.Ordinal))
            {
                continue;
            }

            hasRelationship = true;
            annotations["schema.relationship"] = "explicit";
            annotations["schema.relationship.target"] = GetTypeId(memberType);

            if (attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is string principalTypeName
                && !string.IsNullOrWhiteSpace(principalTypeName))
            {
                annotations["schema.relationship.principalType"] = principalTypeName;
                annotations["schema.relationship.target"] = principalTypeName;
            }

            foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
            {
                if (string.Equals(key, nameof(SemanticRelationshipAttribute.PrincipalKey), StringComparison.Ordinal)
                    && value.Value is string principalKey
                    && !string.IsNullOrWhiteSpace(principalKey))
                {
                    annotations["schema.relationship.principalKey"] = principalKey;
                }
                else if (string.Equals(key, nameof(SemanticRelationshipAttribute.ForeignKey), StringComparison.Ordinal)
                    && value.Value is string foreignKey
                    && !string.IsNullOrWhiteSpace(foreignKey))
                {
                    annotations["schema.relationship.foreignKey"] = foreignKey;
                }
                else if (string.Equals(key, nameof(SemanticRelationshipAttribute.Cardinality), StringComparison.Ordinal)
                    && value.Value is int cardinalityValue
                    && Enum.IsDefined(typeof(RelationshipCardinality), cardinalityValue))
                {
                    annotations["schema.relationship.cardinality"] = ((RelationshipCardinality)cardinalityValue).ToString();
                }
            }
        }

        return hasRelationship;
    }

    private bool TryInferRelationship(IPropertySymbol property, ITypeSymbol memberType, Dictionary<string, string> annotations, out string? targetTypeId)
    {
        targetTypeId = null;
        if (TryGetCollectionItemType(memberType, out ITypeSymbol? itemType))
        {
            (ITypeSymbol normalizedItemType, _) = NormalizeNullability(itemType!);
            if (normalizedItemType.TypeKind is TypeKind.Class or TypeKind.Struct)
            {
                targetTypeId = GetTypeId(normalizedItemType);
                annotations["schema.relationship"] = "inferred";
                annotations["schema.relationship.cardinality"] = RelationshipCardinality.OneToMany.ToString();
                annotations["schema.relationship.target"] = targetTypeId;
                return true;
            }
        }

        (ITypeSymbol normalizedType, _) = NormalizeNullability(memberType);
        if (normalizedType is INamedTypeSymbol namedType && namedType.TypeKind is TypeKind.Class or TypeKind.Struct)
        {
            targetTypeId = GetTypeId(namedType);
            annotations["schema.relationship"] = "inferred";
            annotations["schema.relationship.cardinality"] = RelationshipCardinality.ManyToOne.ToString();
            annotations["schema.relationship.target"] = targetTypeId;
            if (property.Name.EndsWith("Id", StringComparison.Ordinal))
            {
                annotations["schema.relationship.foreignKey"] = property.Name;
            }

            return true;
        }

        return false;
    }

    private static bool TryGetCollectionItemType(ITypeSymbol memberType, out ITypeSymbol? itemType)
    {
        itemType = null;
        if (memberType is IArrayTypeSymbol arrayType)
        {
            itemType = arrayType.ElementType;
            return true;
        }

        if (memberType is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            if (ImplementsAny(
                    namedType,
                    "System.Collections.Generic.IEnumerable<T>",
                    "System.Collections.Generic.IReadOnlyCollection<T>",
                    "System.Collections.Generic.IReadOnlyList<T>",
                    "System.Collections.Generic.ICollection<T>",
                    "System.Collections.Generic.IList<T>",
                    "System.Collections.Generic.List<T>",
                    "System.Collections.Generic.HashSet<T>"))
            {
                itemType = namedType.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    private static string? GetXmlSummary(string? xmlDocumentation)
    {
        if (string.IsNullOrWhiteSpace(xmlDocumentation))
        {
            return null;
        }

        const string startTag = "<summary>";
        const string endTag = "</summary>";
        int start = xmlDocumentation.IndexOf(startTag, StringComparison.Ordinal);
        int end = xmlDocumentation.IndexOf(endTag, StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            return null;
        }

        string content = xmlDocumentation[(start + startTag.Length)..end];
        return content.Trim().Replace("\n", " ", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal);
    }

    private string GetTypeDisplayName(INamedTypeSymbol type)
    {
        foreach (AttributeData attribute in type.GetAttributes())
        {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string title
                && !string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticTypeAttributeMetadataName, StringComparison.Ordinal))
            {
                foreach ((string? key, TypedConstant value) in attribute.NamedArguments)
                {
                    if (string.Equals(key, nameof(SemanticTypeAttribute.Name), StringComparison.Ordinal)
                        && value.Value is string semanticTypeName
                        && !string.IsNullOrWhiteSpace(semanticTypeName))
                    {
                        return semanticTypeName;
                    }
                }
            }
        }

        return ApplyNamingPolicy(type.Name);
    }

    private string GetEnumValueName(IFieldSymbol field)
    {
        foreach (AttributeData attribute in field.GetAttributes())
        {
            if (string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string name
                && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return ApplyNamingPolicy(field.Name);
    }

    private string ApplyNamingPolicy(string value)
    {
        return _options.NamingPolicy switch
        {
            DotNetNamingPolicy.Preserve => value,
            DotNetNamingPolicy.CamelCase => ToCamelCase(value),
            DotNetNamingPolicy.SnakeCase => ToSeparatedCase(value, "_"),
            DotNetNamingPolicy.KebabCase => ToSeparatedCase(value, "-"),
            _ => value,
        };
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string ToSeparatedCase(string value, string separator)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var result = new System.Text.StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (char.IsUpper(current))
            {
                bool shouldPrefixSeparator = i > 0 && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1])));
                if (shouldPrefixSeparator)
                {
                    _ = result.Append(separator);
                }

                _ = result.Append(char.ToLowerInvariant(current));
            }
            else
            {
                _ = result.Append(current);
            }
        }

        return result.ToString();
    }

    private static bool ImplementsAny(INamedTypeSymbol symbol, params string[] names)
    {
        return symbol.AllInterfaces.Any(interfaceType =>
                   interfaceType.IsGenericType && names.Contains(interfaceType.OriginalDefinition.ToDisplayString(), StringComparer.Ordinal))
               || (symbol.IsGenericType && names.Contains(symbol.OriginalDefinition.ToDisplayString(), StringComparer.Ordinal));
    }

    private static INamedTypeSymbol? FindImplementedDictionary(INamedTypeSymbol type)
    {
        static bool IsDictionaryDefinition(INamedTypeSymbol candidate)
        {
            string fullName = candidate.ToDisplayString();
            return string.Equals(fullName, "System.Collections.Generic.IDictionary<TKey, TValue>", StringComparison.Ordinal)
                || string.Equals(fullName, "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>", StringComparison.Ordinal)
                || string.Equals(fullName, "System.Collections.Generic.Dictionary<TKey, TValue>", StringComparison.Ordinal);
        }

        if (type.IsGenericType && IsDictionaryDefinition(type.OriginalDefinition))
        {
            return type;
        }

        return type.AllInterfaces.FirstOrDefault(interfaceType => interfaceType.IsGenericType && IsDictionaryDefinition(interfaceType.OriginalDefinition));
    }

    private static bool IsSupportedDictionaryKey(ITypeSymbol keyType)
    {
        (ITypeSymbol normalized, _) = NormalizeNullability(keyType);
        if (normalized.SpecialType is SpecialType.System_String
            or SpecialType.System_Byte
            or SpecialType.System_SByte
            or SpecialType.System_Int16
            or SpecialType.System_UInt16
            or SpecialType.System_Int32
            or SpecialType.System_UInt32
            or SpecialType.System_Int64
            or SpecialType.System_UInt64)
        {
            return true;
        }

        return string.Equals(normalized.Name, "Guid", StringComparison.Ordinal)
            && string.Equals(normalized.ContainingNamespace?.ToDisplayString(), "System", StringComparison.Ordinal);
    }

    private static bool ShouldIncludeProperty(IPropertySymbol property, DotNetExtractionOptions options)
    {
        bool isSupportedAccessibility = property.DeclaredAccessibility == Accessibility.Public
            || (options.IncludeInternalMembers && property.DeclaredAccessibility == Accessibility.Internal);

        if (!isSupportedAccessibility
            || property.IsStatic
            || property.IsIndexer
            || property.IsImplicitlyDeclared)
        {
            return false;
        }

        return !property.GetAttributes().Any(attribute =>
            string.Equals(attribute.AttributeClass?.ToDisplayString(), SemanticIgnoreAttributeMetadataName, StringComparison.Ordinal)
            || string.Equals(attribute.AttributeClass?.Name, nameof(CompilerGeneratedAttribute), StringComparison.Ordinal));
    }

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
    {
        return attributeType is not null
            && symbol.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attributes, string metadataName)
    {
        return attributes.Any(attribute => string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal));
    }

    private static (ITypeSymbol Type, bool AllowsNull) NormalizeNullability(ITypeSymbol type, NullableAnnotation? annotationOverride = null)
    {
        NullableAnnotation annotation = annotationOverride ?? type.NullableAnnotation;
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return (named.TypeArguments[0], true);
        }

        bool allowsNull = type.IsReferenceType
            ? annotation != NullableAnnotation.NotAnnotated
            : false;
        return (type, allowsNull);
    }

    private string GetNormalizedTypeId(ITypeSymbol type, out bool allowsNull)
    {
        (ITypeSymbol normalizedType, bool nullable) = NormalizeNullability(type);
        allowsNull = nullable;
        return GetTypeId(normalizedType);
    }

    private static string GetTypeId(ITypeSymbol type)
    {
        SymbolDisplayFormat format = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters);
        return type.ToDisplayString(format);
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }

    private void DiagnoseMissingXmlDocumentationIfRequired(ISymbol symbol, Location? location)
    {
        if (!_options.RequireXmlDocumentation)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(symbol.GetDocumentationCommentXml()))
        {
            return;
        }

        _diagnostics.Add(new DotNetExtractionDiagnostic(
            "STM5012",
            $"XML documentation is required but missing for '{symbol.ToDisplayString()}'.",
            location));
    }
}
