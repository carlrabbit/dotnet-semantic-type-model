using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    private const string SemanticRoleAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticRoleAttribute";
    private const string SemanticKeyAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticKeyAttribute";
    private const string SemanticRelationshipAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticRelationshipAttribute";
    private const string GeneratorOptionsAttributeMetadataName = "SemanticTypeModel.DotNet.SemanticTypeModelGeneratorOptionsAttribute";

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
        TryAddXmlDescriptionAnnotation(type, typeAttributes, annotations);
        TryAddSemanticTypeOverrides(typeAttributes, annotations);
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
            TryAddNameAndDescriptionAnnotations(memberAttributes, memberAnnotations, property);
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
        TryAddXmlDescriptionAnnotation(enumType, typeAttributes, annotations);
        ValidateTypeAttributeUsage(typeAttributes, enumType);
        DiagnoseMissingXmlDocumentationIfRequired(enumType, enumType.Locations.FirstOrDefault());

        foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>().Where(static f => f.HasConstantValue))
        {
            if (field.ConstantValue is null)
            {
                continue;
            }

            long numericValue = Convert.ToInt64(field.ConstantValue, CultureInfo.InvariantCulture);
            string valueName = GetEnumValueName(field);
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
