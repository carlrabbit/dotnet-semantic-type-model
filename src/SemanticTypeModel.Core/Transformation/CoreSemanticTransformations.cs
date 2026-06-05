using System.Globalization;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Diagnostics;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Normalizes schema role aliases into canonical object semantic roles.
/// </summary>
public sealed class NormalizeSemanticAliasesTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "core.normalize-semantic-aliases";

    /// <inheritdoc />
    public string DisplayName => nameof(NormalizeSemanticAliasesTransformation);

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        List<TypeDefinition> transformed = [];
        List<string> changes = [];

        foreach (TypeDefinition type in model.Types)
        {
            if (type is not ObjectTypeDefinition objectType)
            {
                transformed.Add(type);
                continue;
            }

            var roleText = GetStringAnnotation(objectType.Annotations, "schema.role");
            if (string.IsNullOrWhiteSpace(roleText))
            {
                transformed.Add(type);
                continue;
            }

            if (!TryParseRole(roleText, out EntityRole role))
            {
                context.Diagnostics.Report(Diagnostic(
                    StmDiagnosticIds.SemanticRoleAliasInvalid,
                    SchemaDiagnosticSeverity.Warning,
                    $"Semantic role alias '{roleText}' is not a supported core semantic role.",
                    ModelPath.ForType(objectType.Id),
                    context.TransformationId));
                transformed.Add(type);
                continue;
            }

            EntitySemantics current = objectType.Semantics;
            if (current.Role != EntityRole.Unspecified && current.Role != role)
            {
                context.Diagnostics.Report(Diagnostic(
                    StmDiagnosticIds.SemanticRoleAliasConflict,
                    SchemaDiagnosticSeverity.Warning,
                    $"Semantic role alias '{role}' conflicts with existing role '{current.Role}'.",
                    ModelPath.ForType(objectType.Id),
                    context.TransformationId));
                transformed.Add(type);
                continue;
            }

            EntitySemantics nextSemantics = current with
            {
                Role = role,
                IsValueObject = role == EntityRole.ValueObject || current.IsValueObject,
            };

            if (nextSemantics != current)
            {
                transformed.Add(objectType with { Semantics = nextSemantics });
                changes.Add($"{ModelPath.ForType(objectType.Id)}/semantics/role -> {role}");
            }
            else
            {
                transformed.Add(type);
            }
        }

        return new SemanticModelTransformationStepResult
        {
            Model = Rebuild(model, transformed),
            ChangeSummary = changes,
        };
    }

    private static bool TryParseRole(string value, out EntityRole role)
    {
        foreach (EntityRole candidate in Enum.GetValues<EntityRole>())
        {
            if (string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                role = candidate;
                return true;
            }
        }

        role = EntityRole.Unspecified;
        return false;
    }

    internal static SchemaDiagnostic Diagnostic(string code, SchemaDiagnosticSeverity severity, string message, string modelPath, string transformationId, IReadOnlyList<string>? relatedModelPaths = null)
    {
        return new SchemaDiagnostic
        {
            Severity = severity,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Transformation,
            PipelineStage = transformationId,
            ModelPath = modelPath,
            RelatedModelPaths = relatedModelPaths ?? [],
        };
    }

    internal static string? GetStringAnnotation(AnnotationBag bag, string key)
    {
        return bag.Items
            .Where(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal))
            .Select(static annotation => annotation.Value?.ToString())
            .LastOrDefault(static value => !string.IsNullOrWhiteSpace(value));
    }

    internal static bool GetBooleanAnnotation(AnnotationBag bag, string key)
    {
        var value = GetStringAnnotation(bag, key);
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    internal static TypeSchemaModel Rebuild(TypeSchemaModel model, IReadOnlyList<TypeDefinition> types)
    {
        return new TypeSchemaModel
        {
            Id = model.Id,
            Types = types,
            TypesById = TypeSchemaModelCloner.CreateTypeIndex(types),
            Annotations = model.Annotations,
        };
    }
}

/// <summary>
/// Derives canonical key definitions from explicit key annotations.
/// </summary>
public sealed class DeriveSemanticKeysTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "core.derive-semantic-keys";

    /// <inheritdoc />
    public string DisplayName => nameof(DeriveSemanticKeysTransformation);

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        List<TypeDefinition> transformed = [];
        List<string> changes = [];

        foreach (TypeDefinition type in model.Types)
        {
            if (type is not ObjectTypeDefinition objectType)
            {
                transformed.Add(type);
                continue;
            }

            List<PropertyDefinition> keyProperties = [.. objectType.Properties.Where(static property => NormalizeSemanticAliasesTransformation.GetBooleanAnnotation(property.Annotations, "schema.key"))];
            if (keyProperties.Count == 0)
            {
                transformed.Add(type);
                continue;
            }

            if (objectType.Semantics.Role != EntityRole.Entity)
            {
                context.Diagnostics.Report(NormalizeSemanticAliasesTransformation.Diagnostic(
                    StmDiagnosticIds.SemanticKeyOnNonEntity,
                    SchemaDiagnosticSeverity.Warning,
                    $"Explicit semantic key metadata was declared on non-entity type '{objectType.Name}'.",
                    ModelPath.ForType(objectType.Id),
                    context.TransformationId));
            }

            List<KeyDefinition> derivedKeys = [.. objectType.Keys];
            foreach (IGrouping<string, PropertyDefinition> group in keyProperties.GroupBy(GetKeyName, StringComparer.Ordinal).OrderBy(static group => group.Key, StringComparer.Ordinal))
            {
                var keyName = group.Key;
                if (derivedKeys.Any(key => string.Equals(key.Name, keyName, StringComparison.Ordinal)))
                {
                    continue;
                }

                PropertyDefinition[] orderedProperties = [.. group.OrderBy(GetKeyOrder).ThenBy(static property => property.Name, StringComparer.Ordinal)];
                KeyKind keyKind = ParseKeyKind(NormalizeSemanticAliasesTransformation.GetStringAnnotation(orderedProperties[0].Annotations, "schema.key.kind"));
                var isGenerated = orderedProperties.Any(static property => NormalizeSemanticAliasesTransformation.GetBooleanAnnotation(property.Annotations, "schema.key.generated"));

                derivedKeys.Add(new KeyDefinition
                {
                    Name = keyName,
                    Kind = keyKind,
                    IsGenerated = isGenerated,
                    Properties = [.. orderedProperties.Select(static property => new PropertyRef(property.Id))],
                    Annotations = new AnnotationBag(),
                });
                changes.Add($"{ModelPath.ForType(objectType.Id)}/keys/{keyName} -> {string.Join(",", orderedProperties.Select(static property => property.Name))}");
            }

            var primaryCount = derivedKeys.Count(static key => key.Kind == KeyKind.Primary);
            if (primaryCount > 1)
            {
                context.Diagnostics.Report(NormalizeSemanticAliasesTransformation.Diagnostic(
                    StmDiagnosticIds.MultiplePrimarySemanticKeys,
                    SchemaDiagnosticSeverity.Warning,
                    $"Type '{objectType.Name}' declares multiple primary semantic keys.",
                    ModelPath.ForType(objectType.Id),
                    context.TransformationId));
            }

            transformed.Add(objectType with { Keys = derivedKeys });
        }

        return new SemanticModelTransformationStepResult
        {
            Model = NormalizeSemanticAliasesTransformation.Rebuild(model, transformed),
            ChangeSummary = changes,
        };
    }

    private static string GetKeyName(PropertyDefinition property)
    {
        var configuredName = NormalizeSemanticAliasesTransformation.GetStringAnnotation(property.Annotations, "schema.key.name");
        return !string.IsNullOrWhiteSpace(configuredName)
            ? configuredName
            : $"PK_{property.Name}";
    }

    private static int GetKeyOrder(PropertyDefinition property)
    {
        var orderText = NormalizeSemanticAliasesTransformation.GetStringAnnotation(property.Annotations, "schema.key.order");
        return int.TryParse(orderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var order) ? order : 0;
    }

    private static KeyKind ParseKeyKind(string? value)
    {
        foreach (KeyKind candidate in Enum.GetValues<KeyKind>())
        {
            if (string.Equals(candidate.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return KeyKind.Primary;
    }
}

/// <summary>
/// Normalizes canonical display metadata from schema annotations where explicit members are absent.
/// </summary>
public sealed class NormalizeDisplayMetadataTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "core.normalize-display-metadata";

    /// <inheritdoc />
    public string DisplayName => nameof(NormalizeDisplayMetadataTransformation);

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        List<TypeDefinition> transformed = [];
        List<string> changes = [];

        foreach (TypeDefinition type in model.Types)
        {
            TypeDefinition next = NormalizeType(type, changes);
            transformed.Add(next);
        }

        return new SemanticModelTransformationStepResult
        {
            Model = NormalizeSemanticAliasesTransformation.Rebuild(model, transformed),
            ChangeSummary = changes,
        };
    }

    private static TypeDefinition NormalizeType(TypeDefinition type, List<string> changes)
    {
        var title = NormalizeSemanticAliasesTransformation.GetStringAnnotation(type.Annotations, "schema.title");
        var description = NormalizeSemanticAliasesTransformation.GetStringAnnotation(type.Annotations, "schema.description");

        TypeDefinition next = type with
        {
            DisplayName = string.IsNullOrWhiteSpace(type.DisplayName) ? title : type.DisplayName,
            Description = string.IsNullOrWhiteSpace(type.Description) ? description : type.Description,
        };

        if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(type.DisplayName))
        {
            changes.Add($"{ModelPath.ForType(type.Id)}/displayName -> {title}");
        }

        if (!string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(type.Description))
        {
            changes.Add($"{ModelPath.ForType(type.Id)}/description -> annotation");
        }

        return next switch
        {
            ObjectTypeDefinition objectType => objectType with { Properties = [.. objectType.Properties.Select(property => NormalizeProperty(objectType, property, changes))] },
            _ => next,
        };
    }

    private static PropertyDefinition NormalizeProperty(ObjectTypeDefinition type, PropertyDefinition property, List<string> changes)
    {
        var title = NormalizeSemanticAliasesTransformation.GetStringAnnotation(property.Annotations, "schema.title");
        var description = NormalizeSemanticAliasesTransformation.GetStringAnnotation(property.Annotations, "schema.description");

        if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(property.DisplayName))
        {
            changes.Add($"{ModelPath.ForProperty(type.Id, property.Id.Value)}/displayName -> {title}");
        }

        if (!string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(property.Description))
        {
            changes.Add($"{ModelPath.ForProperty(type.Id, property.Id.Value)}/description -> annotation");
        }

        return property with
        {
            DisplayName = string.IsNullOrWhiteSpace(property.DisplayName) ? title : property.DisplayName,
            Description = string.IsNullOrWhiteSpace(property.Description) ? description : property.Description,
        };
    }
}
