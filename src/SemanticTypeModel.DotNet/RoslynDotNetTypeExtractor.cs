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
        bool requireXml = fallback.RequireXmlDocumentation;

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
        }

        return fallback with
        {
            GeneratedNamespace = string.IsNullOrWhiteSpace(generatedNamespace) ? fallback.GeneratedNamespace : generatedNamespace!,
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? fallback.ProviderName : providerName!,
            IncludeInternalTypes = includeInternal,
            RequireXmlDocumentation = requireXml,
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
        CollectTypes(scope, roots, semanticTypeAttribute, semanticIgnoreAttribute, options, cancellationToken);

        roots.Sort(static (left, right) => string.CompareOrdinal(
            left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        return roots;
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

        TryAddNameAndDescriptionAnnotations(typeAttributes, annotations);
        TryAddRoleAnnotation(typeAttributes, annotations, type.Locations.FirstOrDefault());
        ValidateTypeAttributeUsage(typeAttributes, type);
        AddInheritanceAnnotations(type, annotations, cancellationToken);
        DiagnoseMissingXmlDocumentationIfRequired(type, type.Locations.FirstOrDefault());

        foreach (ISymbol member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is not IPropertySymbol property || !ShouldIncludeProperty(property))
            {
                continue;
            }

            (ITypeSymbol memberType, bool allowsNull) = NormalizeNullability(property.Type, property.NullableAnnotation);
            string typeId = GetTypeId(memberType);
            ExtractType(memberType, cancellationToken);

            var memberAnnotations = new Dictionary<string, string>(StringComparer.Ordinal);
            ImmutableArray<AttributeData> memberAttributes = property.GetAttributes();
            TryAddNameAndDescriptionAnnotations(memberAttributes, memberAnnotations);
            ValidateMemberAttributeUsage(memberAttributes, property);
            DiagnoseMissingXmlDocumentationIfRequired(property, property.Locations.FirstOrDefault());

            if (HasAttribute(memberAttributes, SemanticKeyAttributeMetadataName))
            {
                memberAnnotations["schema.key"] = "true";
            }

            if (HasAttribute(memberAttributes, SemanticRelationshipAttributeMetadataName))
            {
                memberAnnotations["schema.relationship"] = "true";
            }

            properties.Add(new DotNetPropertyDescriptor
            {
                Name = GetPropertyName(property),
                TypeId = typeId,
                IsRequired = property.IsRequired,
                IsNullable = allowsNull,
                Annotations = memberAnnotations,
            });
        }

        properties.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        return new DotNetObjectTypeDescriptor
        {
            Id = id,
            Name = type.Name,
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
        TryAddNameAndDescriptionAnnotations(enumType.GetAttributes(), annotations);
        ValidateTypeAttributeUsage(enumType.GetAttributes(), enumType);
        DiagnoseMissingXmlDocumentationIfRequired(enumType, enumType.Locations.FirstOrDefault());

        foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>().Where(static f => f.HasConstantValue))
        {
            if (field.ConstantValue is null)
            {
                continue;
            }

            long numericValue = Convert.ToInt64(field.ConstantValue, CultureInfo.InvariantCulture);
            values.Add(new DotNetEnumValueDescriptor { Name = field.Name, NumericValue = numericValue });

            if (seenNumeric.TryGetValue(numericValue, out string? firstName))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5011",
                    $"Enum '{enumType.Name}' has duplicate numeric value '{numericValue}' for '{firstName}' and '{field.Name}'.",
                    field.Locations.FirstOrDefault()));
            }
            else
            {
                seenNumeric[numericValue] = field.Name;
            }
        }

        values.Sort(static (left, right) => string.CompareOrdinal(left.Name, right.Name));

        return new DotNetEnumTypeDescriptor
        {
            Id = id,
            Name = enumType.Name,
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
        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticRoleAttributeMetadataName, StringComparison.Ordinal))
            {
                _diagnostics.Add(new DotNetExtractionDiagnostic(
                    "STM5001",
                    $"[SemanticRole] is not valid on property '{property.Name}'.",
                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? property.Locations.FirstOrDefault()));
            }
        }
    }

    private void ValidateTypeAttributeUsage(ImmutableArray<AttributeData> attributes, INamedTypeSymbol type)
    {
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

    private static void TryAddNameAndDescriptionAnnotations(ImmutableArray<AttributeData> attributes, Dictionary<string, string> annotations)
    {
        foreach (AttributeData attribute in attributes)
        {
            string? metadataName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(metadataName, SemanticNameAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string title
                && !string.IsNullOrWhiteSpace(title))
            {
                annotations["schema.title"] = title;
            }

            if (string.Equals(metadataName, SemanticDescriptionAttributeMetadataName, StringComparison.Ordinal)
                && attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is string description
                && !string.IsNullOrWhiteSpace(description))
            {
                annotations["schema.description"] = description;
            }
        }
    }

    private static string GetPropertyName(IPropertySymbol property)
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

        return property.Name;
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

    private static bool ShouldIncludeProperty(IPropertySymbol property)
    {
        if (property.DeclaredAccessibility != Accessibility.Public
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
