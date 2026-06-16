// These warnings are disabled for this prototype file to keep deterministic projection logic explicit and
// readable while preserving stable contracts and analyzer-clean builds under repository-wide warning-as-error policy.
#pragma warning disable IDE0046
#pragma warning disable IDE0072
#pragma warning disable IDE0305
#pragma warning disable CA1822
#pragma warning disable CA1859
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Diagnostics;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Projects canonical semantic models into an EF Core-like metadata model without requiring EF Core packages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EfCoreModelProjection"/> class.
/// </remarks>
/// <param name="options">Projection options.</param>
public sealed class EfCoreModelProjection(EfCoreProjectionOptions? options = null) : ISchemaProjection<EfModelDefinition>, IProjectionCapabilityProvider
{
    private readonly EfCoreProjectionOptions _options = options ?? EfCoreProjectionOptions.Default;

    /// <summary>
    /// Projects a canonical canonical semantic model into EF Core-like metadata.
    /// </summary>
    /// <param name="model">The source model.</param>
    /// <param name="context">Projection context carrying diagnostic sink state.</param>
    /// <returns>Projected EF Core-like metadata.</returns>
    public EfModelDefinition Project(TypeSchemaModel model, SchemaProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        IList<SchemaDiagnostic> diagnostics = context.Diagnostics;
        var entityInfos = new List<ProjectedEntityInfo>();
        var entityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            var typePath = ModelPath.ForType(objectType.Id);
            if (!ShouldProjectAsEntity(objectType, typePath, diagnostics))
            {
                continue;
            }

            var desiredName = objectType.Name;
            var resolvedName = ResolveUniqueName(desiredName, entityNames, "entity", typePath, diagnostics);
            if (resolvedName is null)
            {
                continue;
            }

            entityInfos.Add(ProjectEntity(model, objectType, resolvedName, false, entityInfos, entityNames, diagnostics));
        }

        ProjectRelationships(model, entityInfos, diagnostics);

        return new EfModelDefinition
        {
            Name = model.Id.Value,
            EntityTypes = entityInfos.Select(static info => info.ToDefinition()).ToArray(),
            Diagnostics = diagnostics.ToArray(),
        };
    }

    /// <inheritdoc />
    public ProjectionCompatibilityContract GetCapabilities()
    {
        return ProjectionCapabilityCatalog.ForTarget(ProjectionTarget.EfCore);
    }

    private static void Report(
        IList<SchemaDiagnostic> diagnostics,
        SchemaDiagnosticSeverity severity,
        string code,
        string message,
        string? modelPath,
        IReadOnlyList<string>? relatedPaths = null)
    {
        diagnostics.Add(new SchemaDiagnostic
        {
            Severity = severity,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Projection,
            ProjectionTarget = ProjectionTarget.EfCore,
            ModelPath = modelPath,
            RelatedModelPaths = relatedPaths ?? [],
        });
    }

    private bool ShouldProjectAsEntity(ObjectTypeDefinition objectType, string typePath, IList<SchemaDiagnostic> diagnostics)
    {
        var explicitOwned = ResolveBooleanAnnotation(objectType.Annotations, typePath, diagnostics, "efCore.owned");
        if (explicitOwned == true || objectType.Semantics.IsValueObject || objectType.Semantics.Role == EntityRole.ValueObject)
        {
            return false;
        }

        var explicitEntity = ResolveBooleanAnnotation(objectType.Annotations, typePath, diagnostics, "efCore.entity");
        if (explicitEntity == true)
        {
            return true;
        }

        if (objectType.Semantics.Role == EntityRole.Entity || objectType.Semantics.IsAggregateRoot)
        {
            return true;
        }

        if (_options.ProjectUnannotatedObjectsAsEntities)
        {
            return true;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_OBJECT_NOT_PROJECTED",
            $"Object type '{objectType.Name}' was not projected as an entity because entity role metadata is missing and unannotated entity projection is disabled.",
            typePath);
        return false;
    }

    private ProjectedEntityInfo ProjectEntity(
        TypeSchemaModel model,
        ObjectTypeDefinition objectType,
        string entityName,
        bool isOwned,
        List<ProjectedEntityInfo> entityInfos,
        HashSet<string> entityNames,
        IList<SchemaDiagnostic> diagnostics)
    {
        var typePath = ModelPath.ForType(objectType.Id);
        var tableName = isOwned ? null : ResolveName(objectType.Annotations, objectType.DisplayName, objectType.Name, typePath, diagnostics, true, "efCore.tableName");
        var schemaName = isOwned ? null : ResolveStringAnnotation(objectType.Annotations, typePath, diagnostics, "efCore.schemaName");
        var properties = new List<EfPropertyDefinition>();
        var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var projectedPropertyNamesById = new Dictionary<PropertyId, string>();
        var relationships = new List<EfRelationshipDefinition>();
        var relationshipNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indexNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyPropertyIds = objectType.Keys.SelectMany(static key => key.Properties).Select(static property => property.Id).ToHashSet();
        var generatedKeyPropertyIds = objectType.Keys.Where(static key => key.IsGenerated).SelectMany(static key => key.Properties).Select(static property => property.Id).ToHashSet();

        foreach (PropertyDefinition property in objectType.Properties)
        {
            foreach (EfPropertyDefinition projected in ProjectProperty(model, objectType, entityName, property, generatedKeyPropertyIds.Contains(property.Id), entityInfos, entityNames, diagnostics, out var sourcePropertyName))
            {
                var propertyPath = ModelPath.ForProperty(objectType.Id, property.Name);
                var resolvedPropertyName = ResolveUniqueName(projected.Name, propertyNames, "property", propertyPath, diagnostics);
                if (resolvedPropertyName is null)
                {
                    continue;
                }

                EfPropertyDefinition finalProperty = projected with
                {
                    Name = resolvedPropertyName,
                    ColumnName = string.IsNullOrWhiteSpace(projected.ColumnName) ? resolvedPropertyName : projected.ColumnName,
                };

                properties.Add(finalProperty);
                if (sourcePropertyName is not null && !projectedPropertyNamesById.ContainsKey(property.Id))
                {
                    projectedPropertyNamesById[property.Id] = finalProperty.Name;
                }
            }
        }

        IReadOnlyList<EfKeyDefinition> keys = ProjectKeys(objectType, keyNames, projectedPropertyNamesById, diagnostics);
        IReadOnlyList<EfIndexDefinition> indexes = ProjectIndexes(objectType, indexNames, projectedPropertyNamesById, diagnostics);
        EfInheritanceDefinition? inheritance = ProjectInheritance(model, objectType, entityInfos, diagnostics);
        if (!isOwned && !_options.AllowKeylessEntities && !keys.Any(static key => key.Kind == EfKeyKind.Primary))
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_ENTITY_PRIMARY_KEY_MISSING",
                $"Entity candidate '{entityName}' does not define a projected primary key.",
                typePath);
        }

        return new ProjectedEntityInfo
        {
            SourceTypeId = objectType.Id,
            EntityName = entityName,
            TableName = tableName,
            SchemaName = schemaName,
            IsOwned = isOwned,
            Properties = properties,
            Keys = keys,
            Relationships = relationships,
            Indexes = indexes,
            Inheritance = inheritance,
            RelationshipNames = relationshipNames,
            KeyPropertyIds = keyPropertyIds,
            ProjectedPropertyNamesById = projectedPropertyNamesById,
            Annotations = objectType.Annotations,
        };
    }

    private IReadOnlyList<EfPropertyDefinition> ProjectProperty(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        string ownerEntityName,
        PropertyDefinition property,
        bool isGeneratedKeyProperty,
        List<ProjectedEntityInfo> entityInfos,
        HashSet<string> entityNames,
        IList<SchemaDiagnostic> diagnostics,
        out string? sourcePropertyName)
    {
        sourcePropertyName = property.Name;
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        if (model.TryGetType(property.Type.Id) is not TypeDefinition propertyType)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Error,
                "EFCORE_PROPERTY_TYPE_NOT_FOUND",
                $"Property '{property.Name}' references unknown type id '{property.Type.Id.Value}'.",
                propertyPath);
            sourcePropertyName = null;
            return [];
        }

        var projectedName = ResolveName(property.Annotations, property.DisplayName, property.Name, propertyPath, diagnostics, false, "efCore.columnName");
        var effectiveNullability = ResolveNullability(property, propertyType, propertyPath, diagnostics);
        var valueGenerated = ResolveValueGenerated(property.Annotations, propertyPath, diagnostics) || isGeneratedKeyProperty;
        var conversion = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "efCore.conversion");
        _ = TryResolveTypeAnnotation(property.Annotations, propertyPath, diagnostics, "efCore.valueConverterType", out Type? converterType);
        _ = TryResolveTypeAnnotation(property.Annotations, propertyPath, diagnostics, "efCore.providerClrType", out Type? providerClrType);

        PreserveSchemaOptionality(property, effectiveNullability, propertyPath, diagnostics);
        ReportConstraintPreservation(property, propertyPath, diagnostics);

        if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData))
        {
            return [];
        }

        if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedCollection))
        {
            Report(diagnostics, SchemaDiagnosticSeverity.Warning, StmDiagnosticIds.OwnedCollectionPolicyRequired, $"Owned collection '{owner.Name}.{property.Name}' requires an explicit EF Core projection policy.", propertyPath);
            return [];
        }

        if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedObject) && propertyType is ObjectTypeDefinition ownedObject)
        {
            return FlattenValueObject(model, owner, property, ownedObject, projectedName, diagnostics)
                .Select(projected => projected with { Annotations = AppendAnnotations(projected.Annotations, CreateAnnotation("efCore.ownership", "OwnsOne", AnnotationScope.Projection, AnnotationSource.Generated)) })
                .ToArray();
        }

        if (IsEnvelopePayload(owner, property))
        {
            return ProjectEnvelopePayload(model, owner, ownerEntityName, property, propertyType, projectedName, effectiveNullability, valueGenerated, entityInfos, entityNames, diagnostics);
        }

        if (propertyType is ScalarTypeDefinition scalar)
        {
            return
            [
                CreateProperty(
                    projectedName,
                    projectedName,
                    ResolveClrType(scalar, property.Annotations, propertyPath, diagnostics),
                    property.Cardinality.IsRequired,
                    effectiveNullability,
                    property.Constraints.String?.MaxLength,
                    scalar.Precision,
                    conversion,
                    valueGenerated,
                    BuildPropertyAnnotations(property, effectiveNullability)) with { ConverterType = converterType, ProviderClrType = providerClrType },
            ];
        }

        if (propertyType is EnumTypeDefinition enumType)
        {
            return
            [
                CreateProperty(
                    projectedName,
                    projectedName,
                    ResolveEnumClrType(enumType, property, propertyPath, diagnostics),
                    property.Cardinality.IsRequired,
                    effectiveNullability,
                    property.Constraints.String?.MaxLength,
                    null,
                    ResolveEnumConversion(enumType, property, propertyPath, diagnostics, conversion),
                    valueGenerated,
                    BuildPropertyAnnotations(property, effectiveNullability)) with { ConverterType = converterType, ProviderClrType = providerClrType },
            ];
        }

        if (propertyType is ObjectTypeDefinition nestedObject && IsValueObject(nestedObject, propertyPath, diagnostics))
        {
            return ProjectValueObject(model, owner, ownerEntityName, property, nestedObject, projectedName, effectiveNullability, valueGenerated, entityInfos, entityNames, diagnostics);
        }

        if (propertyType is ArrayTypeDefinition)
        {
            return HandleUnsupportedShape(property, projectedName, propertyPath, "array", effectiveNullability, valueGenerated, diagnostics);
        }

        if (propertyType is DictionaryTypeDefinition)
        {
            return HandleUnsupportedShape(property, projectedName, propertyPath, "dictionary", effectiveNullability, valueGenerated, diagnostics);
        }

        if (propertyType is UnionTypeDefinition)
        {
            return HandleUnsupportedShape(property, projectedName, propertyPath, "union", effectiveNullability, valueGenerated, diagnostics);
        }

        if (propertyType is ObjectTypeDefinition)
        {
            return HandleUnsupportedShape(property, projectedName, propertyPath, "object", effectiveNullability, valueGenerated, diagnostics);
        }

        return HandleUnsupportedShape(property, projectedName, propertyPath, propertyType.Kind.ToString(), effectiveNullability, valueGenerated, diagnostics);
    }


    private IReadOnlyList<EfPropertyDefinition> ProjectEnvelopePayload(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        string ownerEntityName,
        PropertyDefinition property,
        TypeDefinition propertyType,
        string projectedName,
        bool isNullable,
        bool isGenerated,
        List<ProjectedEntityInfo> entityInfos,
        HashSet<string> entityNames,
        IList<SchemaDiagnostic> diagnostics)
    {
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        EfCoreEnvelopePayloadPolicy policy = ResolveEnvelopePolicy(owner, property);
        switch (policy.StoragePolicy)
        {
            case EfCoreEnvelopePayloadStoragePolicy.Ignored:
                return [];
            case EfCoreEnvelopePayloadStoragePolicy.OwnedSameTable when propertyType is ObjectTypeDefinition payloadObject:
                return FlattenValueObject(model, owner, property, payloadObject, policy.ColumnName ?? projectedName, diagnostics);
            case EfCoreEnvelopePayloadStoragePolicy.OwnedJson:
                return [CreateProperty(projectedName, policy.ColumnName ?? projectedName, typeof(object), property.Cardinality.IsRequired, isNullable, null, null, "OwnedJson", isGenerated, AppendAnnotations(BuildPropertyAnnotations(property, isNullable), CreateAnnotation("efCore.payloadStorage", "OwnedJson", AnnotationScope.Projection, AnnotationSource.Generated)))];
            case EfCoreEnvelopePayloadStoragePolicy.OwnedSeparateTable when propertyType is ObjectTypeDefinition payloadObject:
                var ownedName = ResolveUniqueName($"{ownerEntityName}_{projectedName}", entityNames, "entity", propertyPath, diagnostics);
                if (ownedName is null)
                {
                    return [];
                }

                entityInfos.Add(ProjectEntity(model, payloadObject, ownedName, true, entityInfos, entityNames, diagnostics));
                return [CreateProperty(projectedName, null, typeof(object), property.Cardinality.IsRequired, isNullable, null, null, "OwnedSeparateTable", isGenerated, AppendAnnotations(BuildPropertyAnnotations(property, isNullable), CreateAnnotation("efCore.ownedTypeName", ownedName, AnnotationScope.Projection, AnnotationSource.Generated), CreateAnnotation("efCore.payloadStorage", "OwnedSeparateTable", AnnotationScope.Projection, AnnotationSource.Generated)))];
            case EfCoreEnvelopePayloadStoragePolicy.OwnedSameTable:
            case EfCoreEnvelopePayloadStoragePolicy.OwnedSeparateTable:
                Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_ENVELOPE_PAYLOAD_STORAGE_UNSUPPORTED", $"Envelope payload '{property.Name}' requires an object payload for storage policy '{policy.StoragePolicy}'.", propertyPath);
                return [];
            case EfCoreEnvelopePayloadStoragePolicy.SerializedJson:
            default:
                return [CreateProperty(projectedName, policy.ColumnName ?? projectedName, typeof(string), property.Cardinality.IsRequired, isNullable, property.Constraints.String?.MaxLength, null, "Json", isGenerated, AppendAnnotations(BuildPropertyAnnotations(property, isNullable), CreateAnnotation("efCore.payloadStorage", "SerializedJson", AnnotationScope.Projection, AnnotationSource.Generated)))];
        }
    }

    private EfCoreEnvelopePayloadPolicy ResolveEnvelopePolicy(ObjectTypeDefinition owner, PropertyDefinition property)
    {
        if (_options.EnvelopePolicies.TryGetValue(owner.Name, out EfCoreEnvelopePayloadPolicy? byName) || _options.EnvelopePolicies.TryGetValue(owner.Id.Value, out byName))
        {
            return byName;
        }

        return new EfCoreEnvelopePayloadPolicy { EnvelopeTypeName = owner.Name, PayloadPropertyName = property.Name, StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.SerializedJson };
    }

    private static bool IsEnvelopePayload(ObjectTypeDefinition owner, PropertyDefinition property)
    {
        return owner.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.Envelope, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            && property.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.EnvelopePayload, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
    }

    private IReadOnlyList<EfKeyDefinition> ProjectKeys(
        ObjectTypeDefinition objectType,
        HashSet<string> keyNames,
        IReadOnlyDictionary<PropertyId, string> projectedPropertyNamesById,
        IList<SchemaDiagnostic> diagnostics)
    {
        var projectedKeys = new List<EfKeyDefinition>();
        foreach (KeyDefinition key in objectType.Keys)
        {
            var keyPath = ModelPath.ForKey(objectType.Id, key.Name);
            var resolvedKeyName = ResolveUniqueName(key.Name, keyNames, "key", keyPath, diagnostics);
            if (resolvedKeyName is null)
            {
                continue;
            }

            var propertyNames = new List<string>();
            foreach (PropertyRef propertyRef in key.Properties)
            {
                if (projectedPropertyNamesById.TryGetValue(propertyRef.Id, out var propertyName))
                {
                    propertyNames.Add(propertyName);
                    continue;
                }

                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "EFCORE_KEY_PROPERTY_NOT_PROJECTED",
                    $"Key '{key.Name}' references property '{propertyRef.Id.Value}' that was not projected.",
                    keyPath);
            }

            if (propertyNames.Count == 0)
            {
                continue;
            }

            projectedKeys.Add(new EfKeyDefinition
            {
                Name = resolvedKeyName,
                PropertyNames = propertyNames,
                Kind = MapKeyKind(key.Kind),
                IsGenerated = key.IsGenerated,
            });
        }

        return projectedKeys;
    }

    private IReadOnlyList<EfIndexDefinition> ProjectIndexes(
        ObjectTypeDefinition objectType,
        HashSet<string> indexNames,
        IReadOnlyDictionary<PropertyId, string> projectedPropertyNamesById,
        IList<SchemaDiagnostic> diagnostics)
    {
        var indexes = new List<EfIndexDefinition>();
        foreach (PropertyDefinition property in objectType.Properties)
        {
            var propertyPath = ModelPath.ForProperty(objectType.Id, property.Name);
            if (!TryGetAnnotationValue(property.Annotations, "efCore.index", out var raw))
            {
                continue;
            }

            if (!projectedPropertyNamesById.TryGetValue(property.Id, out var propertyName))
            {
                Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_INDEX_PROPERTY_NOT_PROJECTED", $"Index metadata for property '{property.Name}' could not be applied because the property was not projected.", propertyPath);
                continue;
            }

            var desiredName = raw is string text && !string.IsNullOrWhiteSpace(text) && !string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
                ? text.Trim()
                : $"IX_{objectType.Name}_{propertyName}";
            var resolvedName = ResolveUniqueName(desiredName, indexNames, "index", propertyPath, diagnostics);
            if (resolvedName is null)
            {
                continue;
            }

            var unique = ResolveBooleanAnnotation(property.Annotations, propertyPath, diagnostics, "efCore.uniqueIndex") == true;
            indexes.Add(new EfIndexDefinition { Name = resolvedName, PropertyNames = [propertyName], IsUnique = unique });
        }

        if (TryGetAnnotationValue(objectType.Annotations, "efCore.indexes", out var rawIndexes))
        {
            foreach (var specification in Convert.ToString(rawIndexes, System.Globalization.CultureInfo.InvariantCulture)?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
            {
                var parts = specification.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_INVALID_ANNOTATION_VALUE", "Annotation 'efCore.indexes' entries must use 'Name:PropertyId,PropertyId[:unique]'.", ModelPath.ForType(objectType.Id));
                    continue;
                }

                var propertyNames = new List<string>();
                foreach (var propertyIdText in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (projectedPropertyNamesById.TryGetValue(new PropertyId(propertyIdText), out var projectedName))
                    {
                        propertyNames.Add(projectedName);
                        continue;
                    }

                    Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_INDEX_PROPERTY_NOT_PROJECTED", $"Index '{parts[0]}' references property '{propertyIdText}' that was not projected.", ModelPath.ForType(objectType.Id));
                }

                if (propertyNames.Count == 0)
                {
                    continue;
                }

                var resolvedName = ResolveUniqueName(parts[0], indexNames, "index", ModelPath.ForType(objectType.Id), diagnostics);
                if (resolvedName is null)
                {
                    continue;
                }

                var unique = parts.Length > 2 && string.Equals(parts[2], "unique", StringComparison.OrdinalIgnoreCase);
                indexes.Add(new EfIndexDefinition { Name = resolvedName, PropertyNames = propertyNames, IsUnique = unique });
            }
        }

        return indexes;
    }

    private EfInheritanceDefinition? ProjectInheritance(TypeSchemaModel model, ObjectTypeDefinition objectType, IReadOnlyList<ProjectedEntityInfo> entityInfos, IList<SchemaDiagnostic> diagnostics)
    {
        var typePath = ModelPath.ForType(objectType.Id);
        string? baseEntity = null;
        if (objectType.Composition.AllOf.Count > 0)
        {
            TypeId baseTypeId = objectType.Composition.AllOf[0].Id;
            baseEntity = entityInfos.FirstOrDefault(info => info.SourceTypeId == baseTypeId)?.EntityName;
            if (baseEntity is null && model.TryGetType(baseTypeId) is ObjectTypeDefinition baseType)
            {
                baseEntity = baseType.Name;
            }
        }

        EfCoreInheritanceStrategy strategy = ResolveInheritanceStrategy(objectType.Annotations, typePath, diagnostics);
        if (strategy == EfCoreInheritanceStrategy.Unspecified)
        {
            strategy = _options.DefaultInheritanceStrategy;
        }

        if (baseEntity is null && strategy == EfCoreInheritanceStrategy.Unspecified)
        {
            return null;
        }

        if (baseEntity is not null && strategy == EfCoreInheritanceStrategy.Unspecified)
        {
            Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_INHERITANCE_STRATEGY_REQUIRED", $"Entity '{objectType.Name}' declares inheritance but no EF Core inheritance strategy was selected.", typePath);
            return null;
        }

        return new EfInheritanceDefinition
        {
            Strategy = strategy,
            BaseEntity = baseEntity,
            DiscriminatorProperty = ResolveStringAnnotation(objectType.Annotations, typePath, diagnostics, "efCore.discriminatorProperty"),
            DiscriminatorValue = ResolveStringAnnotation(objectType.Annotations, typePath, diagnostics, "efCore.discriminatorValue"),
        };
    }

    private void ProjectRelationships(TypeSchemaModel model, IReadOnlyList<ProjectedEntityInfo> entityInfos, IList<SchemaDiagnostic> diagnostics)
    {
        IReadOnlyDictionary<TypeId, ProjectedEntityInfo> projectedByTypeId = entityInfos.ToDictionary(static info => info.SourceTypeId);
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (RelationshipDefinition relationship in objectType.Relationships)
            {
                if (!dedupe.Add(relationship.Id.Value))
                {
                    continue;
                }

                var relationshipPath = ModelPath.ForRelationship(objectType.Id, relationship.Id);
                if (relationship.Cardinality == RelationshipCardinality.ManyToMany)
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "EFCORE_MANY_TO_MANY_UNSUPPORTED",
                        $"Relationship '{relationship.Id.Value}' is many-to-many and is deferred by the EF Core prototype.",
                        relationshipPath);
                    continue;
                }

                if (!projectedByTypeId.TryGetValue(relationship.PrincipalType.Id, out ProjectedEntityInfo? principalEntity))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "EFCORE_RELATIONSHIP_ENDPOINT_TYPE_NOT_PROJECTED",
                        $"Principal relationship endpoint '{relationship.PrincipalType.Id.Value}' is not projected as an entity.",
                        relationshipPath);
                    continue;
                }

                if (!projectedByTypeId.TryGetValue(relationship.DependentType.Id, out ProjectedEntityInfo? dependentEntity))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "EFCORE_RELATIONSHIP_ENDPOINT_TYPE_NOT_PROJECTED",
                        $"Dependent relationship endpoint '{relationship.DependentType.Id.Value}' is not projected as an entity.",
                        relationshipPath);
                    continue;
                }

                IReadOnlyList<string> principalProperties = ResolveRelationshipProperties(relationship.PrincipalProperties, principalEntity, relationshipPath, diagnostics, "principal");
                IReadOnlyList<string> dependentProperties = ResolveRelationshipProperties(relationship.DependentProperties, dependentEntity, relationshipPath, diagnostics, "dependent");
                if (principalProperties.Count == 0 || dependentProperties.Count == 0)
                {
                    continue;
                }

                var resolvedName = ResolveUniqueName(relationship.Id.Value, dependentEntity.RelationshipNames, "relationship", relationshipPath, diagnostics);
                if (resolvedName is null)
                {
                    continue;
                }

                dependentEntity.Relationships.Add(new EfRelationshipDefinition
                {
                    Name = resolvedName,
                    PrincipalEntity = principalEntity.EntityName,
                    DependentEntity = dependentEntity.EntityName,
                    PrincipalProperties = principalProperties,
                    DependentProperties = dependentProperties,
                    Cardinality = MapCardinality(relationship.Cardinality),
                    DeleteBehavior = ResolveDeleteBehavior(relationship, relationshipPath, diagnostics),
                    Annotations = relationship.Annotations,
                });
            }
        }
    }

    private IReadOnlyList<string> ResolveRelationshipProperties(
        IReadOnlyList<PropertyRef> propertyRefs,
        ProjectedEntityInfo entity,
        string relationshipPath,
        IList<SchemaDiagnostic> diagnostics,
        string endpointName)
    {
        var projected = new List<string>();
        if (propertyRefs.Count == 0)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_RELATIONSHIP_ENDPOINT_PROPERTY_MISSING",
                $"The {endpointName} endpoint does not declare any properties.",
                relationshipPath);
            return projected;
        }

        foreach (PropertyRef propertyRef in propertyRefs)
        {
            if (entity.ProjectedPropertyNamesById.TryGetValue(propertyRef.Id, out var propertyName))
            {
                projected.Add(propertyName);
                continue;
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_RELATIONSHIP_ENDPOINT_PROPERTY_NOT_PROJECTED",
                $"The {endpointName} endpoint property '{propertyRef.Id.Value}' was not projected.",
                relationshipPath);
        }

        return projected;
    }

    private IReadOnlyList<EfPropertyDefinition> ProjectValueObject(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        string ownerEntityName,
        PropertyDefinition property,
        ObjectTypeDefinition valueObjectType,
        string projectedName,
        bool isNullable,
        bool isGenerated,
        List<ProjectedEntityInfo> entityInfos,
        HashSet<string> entityNames,
        IList<SchemaDiagnostic> diagnostics)
    {
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        switch (_options.ValueObjectProjectionMode)
        {
            case ValueObjectEfProjectionMode.Diagnose:
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "EFCORE_VALUE_OBJECT_REQUIRES_MODE",
                    $"Value object '{valueObjectType.Name}' requires owned, flatten, or serialize configuration for EF Core projection.",
                    propertyPath);
                return [];

            case ValueObjectEfProjectionMode.SerializeJson:
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "EFCORE_VALUE_OBJECT_SERIALIZED_JSON",
                    $"Value object '{valueObjectType.Name}' was serialized as JSON/text.",
                    propertyPath);
                return
                [
                    CreateProperty(
                        projectedName,
                        projectedName,
                        typeof(string),
                        property.Cardinality.IsRequired,
                        isNullable,
                        property.Constraints.String?.MaxLength,
                        null,
                        "Json",
                        isGenerated,
                        BuildPropertyAnnotations(property, isNullable)),
                ];

            case ValueObjectEfProjectionMode.Flatten:
                return FlattenValueObject(model, owner, property, valueObjectType, projectedName, diagnostics);

            case ValueObjectEfProjectionMode.Owned:
                var ownedName = ResolveUniqueName($"{ownerEntityName}_{projectedName}", entityNames, "entity", propertyPath, diagnostics);
                if (ownedName is null)
                {
                    return [];
                }

                entityInfos.Add(ProjectEntity(model, valueObjectType, ownedName, true, entityInfos, entityNames, diagnostics));
                return
                [
                    CreateProperty(
                        projectedName,
                        null,
                        typeof(object),
                        property.Cardinality.IsRequired,
                        isNullable,
                        null,
                        null,
                        "Owned",
                        isGenerated,
                        AppendAnnotations(
                            BuildPropertyAnnotations(property, isNullable),
                            CreateAnnotation("efCore.ownedTypeName", ownedName, AnnotationScope.Projection, AnnotationSource.Generated))),
                ];

            default:
                return [];
        }
    }

    private IReadOnlyList<EfPropertyDefinition> FlattenValueObject(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        ObjectTypeDefinition valueObjectType,
        string projectedName,
        IList<SchemaDiagnostic> diagnostics)
    {
        var flattened = new List<EfPropertyDefinition>();
        foreach (PropertyDefinition nestedProperty in valueObjectType.Properties)
        {
            var nestedPath = ModelPath.ForProperty(valueObjectType.Id, nestedProperty.Name);
            if (model.TryGetType(nestedProperty.Type.Id) is not TypeDefinition nestedType)
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "EFCORE_PROPERTY_TYPE_NOT_FOUND",
                    $"Nested property '{nestedProperty.Name}' references unknown type id '{nestedProperty.Type.Id.Value}'.",
                    nestedPath);
                continue;
            }

            if (nestedType is not ScalarTypeDefinition and not EnumTypeDefinition)
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "EFCORE_VALUE_OBJECT_FLATTEN_UNSUPPORTED",
                    $"Nested value object property '{nestedProperty.Name}' is not scalar or enum and was not flattened.",
                    nestedPath);
                continue;
            }

            var flattenedName = $"{projectedName}_{nestedProperty.Name}";
            var flattenedNullability = nestedProperty.Cardinality.AllowsNull || nestedType.Nullability.AllowsNull;
            if (nestedType is ScalarTypeDefinition scalar)
            {
                flattened.Add(
                    CreateProperty(
                        flattenedName,
                        flattenedName,
                        ResolveClrType(scalar, nestedProperty.Annotations, nestedPath, diagnostics),
                        nestedProperty.Cardinality.IsRequired,
                        flattenedNullability,
                        nestedProperty.Constraints.String?.MaxLength,
                        scalar.Precision,
                        ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, "efCore.conversion"),
                        false,
                        BuildPropertyAnnotations(nestedProperty, flattenedNullability)));
                continue;
            }

            flattened.Add(
                CreateProperty(
                    flattenedName,
                    flattenedName,
                    ResolveEnumClrType((EnumTypeDefinition)nestedType, nestedProperty, nestedPath, diagnostics),
                    nestedProperty.Cardinality.IsRequired,
                    flattenedNullability,
                    nestedProperty.Constraints.String?.MaxLength,
                    null,
                    ResolveEnumConversion((EnumTypeDefinition)nestedType, nestedProperty, nestedPath, diagnostics, ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, "efCore.conversion")),
                    false,
                    BuildPropertyAnnotations(nestedProperty, flattenedNullability)));
        }

        if (flattened.Count == 0)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_VALUE_OBJECT_FLATTEN_EMPTY",
                $"Value object '{valueObjectType.Name}' produced no flattenable properties.",
                ModelPath.ForProperty(owner.Id, property.Name));
        }

        return flattened;
    }

    private IReadOnlyList<EfPropertyDefinition> HandleUnsupportedShape(
        PropertyDefinition property,
        string projectedName,
        string propertyPath,
        string shapeName,
        bool isNullable,
        bool isGenerated,
        IList<SchemaDiagnostic> diagnostics)
    {
        if (_options.UnsupportedShapeBehavior == UnsupportedEfShapeBehavior.SerializeJson)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                $"EFCORE_{shapeName.ToUpperInvariant()}_SERIALIZED_JSON",
                $"Property '{property.Name}' with shape '{shapeName}' was serialized as JSON/text.",
                propertyPath);
            return
            [
                CreateProperty(projectedName, projectedName, typeof(string), property.Cardinality.IsRequired, isNullable, null, null, "Json", isGenerated, BuildPropertyAnnotations(property, isNullable)),
            ];
        }

        if (_options.UnsupportedShapeBehavior == UnsupportedEfShapeBehavior.IgnoreWithWarning)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                $"EFCORE_{shapeName.ToUpperInvariant()}_IGNORED",
                $"Property '{property.Name}' with shape '{shapeName}' was skipped for EF Core projection.",
                propertyPath);
            return [];
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            $"EFCORE_{shapeName.ToUpperInvariant()}_UNSUPPORTED",
            $"Property '{property.Name}' with shape '{shapeName}' is unsupported for EF Core projection.",
            propertyPath);
        return [];
    }

    private static EfPropertyDefinition CreateProperty(
        string name,
        string? columnName,
        Type clrType,
        bool isRequired,
        bool isNullable,
        int? maxLength,
        NumericPrecision? precision,
        string? conversion,
        bool isGenerated,
        AnnotationBag annotations)
    {
        return new EfPropertyDefinition
        {
            Name = name,
            ColumnName = columnName,
            ClrType = clrType,
            IsRequired = isRequired,
            IsNullable = isNullable,
            MaxLength = maxLength,
            Precision = precision,
            Conversion = conversion,
            IsGenerated = isGenerated,
            Annotations = annotations,
        };
    }

    private Type ResolveClrType(ScalarTypeDefinition scalar, AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (TryResolveClrTypeAnnotation(annotations, modelPath, diagnostics, out Type annotatedType))
        {
            return annotatedType;
        }

        return scalar.ScalarKind switch
        {
            ScalarKind.Boolean => typeof(bool),
            ScalarKind.String => typeof(string),
            ScalarKind.Integer => typeof(long),
            ScalarKind.Number => scalar.Precision is not null ? typeof(decimal) : typeof(double),
            ScalarKind.Decimal => typeof(decimal),
            ScalarKind.Date => typeof(DateOnly),
            ScalarKind.Time => typeof(TimeOnly),
            ScalarKind.DateTime => typeof(DateTime),
            ScalarKind.DateTimeOffset => typeof(DateTimeOffset),
            ScalarKind.Duration => typeof(TimeSpan),
            ScalarKind.Guid => typeof(Guid),
            ScalarKind.Binary => typeof(byte[]),
            ScalarKind.Json => ReportLossyClrType("EFCORE_JSON_SCALAR_MAPPED_TO_STRING", "Json scalar values are projected as string without provider-specific JSON mapping.", typeof(string), modelPath, diagnostics),
            _ => ReportLossyClrType("EFCORE_UNKNOWN_SCALAR_MAPPED_TO_STRING", $"Scalar kind '{scalar.ScalarKind}' was projected as string.", typeof(string), modelPath, diagnostics),
        };
    }

    private Type ResolveEnumClrType(EnumTypeDefinition enumType, PropertyDefinition property, string propertyPath, IList<SchemaDiagnostic> diagnostics)
    {
        EnumEfProjectionMode storage = ResolveEnumStorage(property, propertyPath, diagnostics);
        if (storage == EnumEfProjectionMode.Numeric)
        {
            return enumType.StorageKind switch
            {
                EnumStorageKind.Integer => typeof(long),
                EnumStorageKind.Number => typeof(double),
                _ => ReportLossyClrType(
                    "EFCORE_ENUM_STORAGE_INVALID",
                    $"Enum '{enumType.Name}' cannot use numeric storage because its canonical storage kind is '{enumType.StorageKind}'.",
                    typeof(string),
                    propertyPath,
                    diagnostics),
            };
        }

        return typeof(string);
    }

    private string? ResolveEnumConversion(EnumTypeDefinition enumType, PropertyDefinition property, string propertyPath, IList<SchemaDiagnostic> diagnostics, string? explicitConversion)
    {
        if (!string.IsNullOrWhiteSpace(explicitConversion))
        {
            return explicitConversion;
        }

        EnumEfProjectionMode storage = ResolveEnumStorage(property, propertyPath, diagnostics);
        if (storage == EnumEfProjectionMode.Numeric && enumType.StorageKind is not EnumStorageKind.Integer and not EnumStorageKind.Number)
        {
            return "EnumToString";
        }

        return storage == EnumEfProjectionMode.Numeric ? null : "EnumToString";
    }

    private EnumEfProjectionMode ResolveEnumStorage(PropertyDefinition property, string propertyPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (TryGetAnnotationValue(property.Annotations, "efCore.enumStorage", out var raw))
        {
            if (raw is string text)
            {
                if (string.Equals(text, "String", StringComparison.OrdinalIgnoreCase))
                {
                    return EnumEfProjectionMode.String;
                }

                if (string.Equals(text, "Numeric", StringComparison.OrdinalIgnoreCase))
                {
                    return EnumEfProjectionMode.Numeric;
                }
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                "Annotation 'efCore.enumStorage' must be 'String' or 'Numeric'.",
                propertyPath);
        }

        return _options.EnumProjectionMode;
    }

    private static Type ReportLossyClrType(string code, string message, Type fallbackType, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        Report(diagnostics, SchemaDiagnosticSeverity.Warning, code, message, modelPath);
        return fallbackType;
    }

    private bool ResolveNullability(PropertyDefinition property, TypeDefinition propertyType, string propertyPath, IList<SchemaDiagnostic> diagnostics)
    {
        var annotation = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "dotnet.nullability");
        if (annotation is not null)
        {
            if (string.Equals(annotation, "Nullable", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(annotation, "NonNullable", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                "Annotation 'dotnet.nullability' must be 'Nullable' or 'NonNullable'.",
                propertyPath);
        }

        return property.Cardinality.AllowsNull || propertyType.Nullability.AllowsNull;
    }

    private static void PreserveSchemaOptionality(PropertyDefinition property, bool isNullable, string propertyPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (property.Cardinality.IsRequired && !isNullable)
        {
            return;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_OPTIONALITY_PRESERVED_AS_ANNOTATION",
            $"Property '{property.Name}' carries schema presence/nullability semantics that are preserved as annotations for EF Core projection.",
            propertyPath);
    }

    private static void ReportConstraintPreservation(PropertyDefinition property, string propertyPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(property.Constraints.String?.Pattern))
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_STRING_PATTERN_PRESERVED_AS_ANNOTATION",
                $"Property '{property.Name}' pattern constraint is preserved as annotation metadata.",
                propertyPath);
        }

        if (property.Constraints.Numeric?.Minimum is not null ||
            property.Constraints.Numeric?.Maximum is not null ||
            property.Constraints.Numeric?.MultipleOf is not null)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_NUMERIC_CONSTRAINTS_PRESERVED_AS_ANNOTATION",
                $"Property '{property.Name}' numeric constraints are preserved as annotation metadata.",
                propertyPath);
        }
    }

    private EfKeyKind MapKeyKind(KeyKind keyKind)
    {
        return keyKind switch
        {
            KeyKind.Primary => EfKeyKind.Primary,
            KeyKind.Surrogate => EfKeyKind.Surrogate,
            KeyKind.External => _options.AlternateKeyProjectionMode == AlternateKeyProjectionMode.UniqueIndex ? EfKeyKind.UniqueIndex : EfKeyKind.External,
            KeyKind.Alternate or KeyKind.Natural => _options.AlternateKeyProjectionMode switch
            {
                AlternateKeyProjectionMode.UniqueIndex => EfKeyKind.UniqueIndex,
                AlternateKeyProjectionMode.AnnotationOnly => EfKeyKind.External,
                _ => EfKeyKind.Alternate,
            },
            _ => EfKeyKind.Alternate,
        };
    }

    private static EfRelationshipCardinality MapCardinality(RelationshipCardinality cardinality)
    {
        return cardinality switch
        {
            RelationshipCardinality.OneToOne => EfRelationshipCardinality.OneToOne,
            RelationshipCardinality.OneToMany => EfRelationshipCardinality.OneToMany,
            RelationshipCardinality.ManyToOne => EfRelationshipCardinality.ManyToOne,
            RelationshipCardinality.ManyToMany => EfRelationshipCardinality.ManyToMany,
            _ => EfRelationshipCardinality.ManyToOne,
        };
    }

    private static EfDeleteBehavior MapDeleteBehavior(DeleteBehaviorSemantics deleteBehavior)
    {
        return deleteBehavior switch
        {
            DeleteBehaviorSemantics.Restrict => EfDeleteBehavior.Restrict,
            DeleteBehaviorSemantics.Cascade => EfDeleteBehavior.Cascade,
            DeleteBehaviorSemantics.SetNull => EfDeleteBehavior.SetNull,
            DeleteBehaviorSemantics.NoAction => EfDeleteBehavior.NoAction,
            _ => EfDeleteBehavior.Unspecified,
        };
    }

    private static bool IsValueObject(ObjectTypeDefinition objectType, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        var annotation = ResolveBooleanAnnotation(objectType.Annotations, modelPath, diagnostics, "efCore.owned");
        return annotation == true || objectType.Semantics.IsValueObject || objectType.Semantics.Role == EntityRole.ValueObject;
    }

    private static bool ResolveValueGenerated(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (!TryGetAnnotationValue(annotations, "efCore.valueGenerated", out var raw))
        {
            return false;
        }

        if (raw is bool flag)
        {
            return flag;
        }

        if (raw is string text)
        {
            if (string.Equals(text, "Never", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "False", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_INVALID_ANNOTATION_VALUE",
            "Annotation 'efCore.valueGenerated' must be a boolean or generation mode string.",
            modelPath);
        return false;
    }

    private EfDeleteBehavior ResolveDeleteBehavior(RelationshipDefinition relationship, string relationshipPath, IList<SchemaDiagnostic> diagnostics)
    {
        var annotation = ResolveStringAnnotation(relationship.Annotations, relationshipPath, diagnostics, "efCore.deleteBehavior");
        if (annotation is null)
        {
            return MapDeleteBehavior(relationship.DeleteBehavior);
        }

        return annotation switch
        {
            var value when string.Equals(value, "Restrict", StringComparison.OrdinalIgnoreCase) => EfDeleteBehavior.Restrict,
            var value when string.Equals(value, "Cascade", StringComparison.OrdinalIgnoreCase) => EfDeleteBehavior.Cascade,
            var value when string.Equals(value, "SetNull", StringComparison.OrdinalIgnoreCase) => EfDeleteBehavior.SetNull,
            var value when string.Equals(value, "NoAction", StringComparison.OrdinalIgnoreCase) => EfDeleteBehavior.NoAction,
            _ => ReportInvalidDeleteBehavior(annotation, relationshipPath, diagnostics),
        };
    }

    private static EfDeleteBehavior ReportInvalidDeleteBehavior(string value, string relationshipPath, IList<SchemaDiagnostic> diagnostics)
    {
        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_INVALID_ANNOTATION_VALUE",
            $"Annotation 'efCore.deleteBehavior' value '{value}' is invalid.",
            relationshipPath);
        return EfDeleteBehavior.Unspecified;
    }

    private string ResolveName(
        AnnotationBag annotations,
        string? displayName,
        string canonicalName,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics,
        bool useDisplayName,
        params string[] annotationKeys)
    {
        foreach (var key in annotationKeys)
        {
            if (!TryGetAnnotationValue(annotations, key, out var value))
            {
                continue;
            }

            if (value is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a non-empty string.",
                modelPath);
        }

        if (useDisplayName && _options.PreferDisplayNamesForTableAndColumnNames && !string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return canonicalName;
    }

    private string? ResolveUniqueName(string desiredName, HashSet<string> usedNames, string scope, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (usedNames.Add(desiredName))
        {
            return desiredName;
        }

        if (_options.NameCollisionBehavior == NameCollisionBehavior.Suffix)
        {
            var suffix = 2;
            var candidate = desiredName;
            while (!usedNames.Add(candidate))
            {
                candidate = $"{desiredName}_{suffix}";
                suffix++;
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_NAME_COLLISION_SUFFIX_APPLIED",
                $"Duplicate {scope} name '{desiredName}' was renamed to '{candidate}'.",
                modelPath);
            return candidate;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Error,
            "EFCORE_DUPLICATE_PROJECTED_NAME",
            $"Duplicate {scope} name '{desiredName}' was detected.",
            modelPath);
        return null;
    }

    private static bool TryResolveTypeAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, string annotationKey, out Type? type)
    {
        type = null;
        if (!TryGetAnnotationValue(annotations, annotationKey, out var raw))
        {
            return false;
        }

        if (raw is Type directType)
        {
            type = directType;
            return true;
        }

        if (raw is string typeName && !string.IsNullOrWhiteSpace(typeName))
        {
            type = Type.GetType(typeName, throwOnError: false);
            if (type is not null)
            {
                return true;
            }
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_INVALID_ANNOTATION_VALUE",
            $"Annotation '{annotationKey}' must be a System.Type or assembly-qualified type name.",
            modelPath);
        return false;
    }

    private EfCoreInheritanceStrategy ResolveInheritanceStrategy(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        var annotation = ResolveStringAnnotation(annotations, modelPath, diagnostics, "efCore.inheritanceStrategy");
        if (annotation is null)
        {
            return EfCoreInheritanceStrategy.Unspecified;
        }

        return annotation switch
        {
            var value when string.Equals(value, "TPH", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Tph", StringComparison.OrdinalIgnoreCase) => EfCoreInheritanceStrategy.Tph,
            var value when string.Equals(value, "TPT", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Tpt", StringComparison.OrdinalIgnoreCase) => EfCoreInheritanceStrategy.Tpt,
            var value when string.Equals(value, "TPC", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Tpc", StringComparison.OrdinalIgnoreCase) => EfCoreInheritanceStrategy.Tpc,
            _ => ReportInvalidInheritanceStrategy(annotation, modelPath, diagnostics),
        };
    }

    private static EfCoreInheritanceStrategy ReportInvalidInheritanceStrategy(string value, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        Report(diagnostics, SchemaDiagnosticSeverity.Warning, "EFCORE_INVALID_ANNOTATION_VALUE", $"Annotation 'efCore.inheritanceStrategy' value '{value}' is invalid.", modelPath);
        return EfCoreInheritanceStrategy.Unspecified;
    }

    private static bool TryResolveClrTypeAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, out Type clrType)
    {
        clrType = typeof(string);
        if (!TryGetAnnotationValue(annotations, "dotnet.clrType", out var raw))
        {
            return false;
        }

        if (raw is not string text || string.IsNullOrWhiteSpace(text))
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                "Annotation 'dotnet.clrType' must be a non-empty string.",
                modelPath);
            return false;
        }

        clrType = text.Trim() switch
        {
            "bool" or "System.Boolean" => typeof(bool),
            "string" or "System.String" => typeof(string),
            "long" or "System.Int64" => typeof(long),
            "int" or "System.Int32" => typeof(int),
            "double" or "System.Double" => typeof(double),
            "decimal" or "System.Decimal" => typeof(decimal),
            "Guid" or "System.Guid" => typeof(Guid),
            "DateOnly" or "System.DateOnly" => typeof(DateOnly),
            "TimeOnly" or "System.TimeOnly" => typeof(TimeOnly),
            "DateTime" or "System.DateTime" => typeof(DateTime),
            "DateTimeOffset" or "System.DateTimeOffset" => typeof(DateTimeOffset),
            "TimeSpan" or "System.TimeSpan" => typeof(TimeSpan),
            "byte[]" or "System.Byte[]" => typeof(byte[]),
            _ => Type.GetType(text.Trim(), throwOnError: false) ?? typeof(void),
        };

        if (clrType == typeof(void))
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                $"Annotation 'dotnet.clrType' value '{text}' could not be resolved.",
                modelPath);
            clrType = typeof(string);
        }

        return true;
    }

    private static bool TryGetAnnotationValue(AnnotationBag annotations, string key, out object? value)
    {
        Annotation? annotation = annotations.Items.LastOrDefault(item => string.Equals(item.Key.Value, key, StringComparison.Ordinal));
        if (annotation is null)
        {
            value = null;
            return false;
        }

        value = annotation.Value;
        return true;
    }

    private static string? ResolveStringAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!TryGetAnnotationValue(annotations, key, out var raw))
            {
                continue;
            }

            if (raw is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text.Trim();
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "EFCORE_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a non-empty string.",
                modelPath);
        }

        return null;
    }

    private static bool HasBooleanAnnotation(AnnotationBag annotations, string key)
    {
        return TryGetAnnotationValue(annotations, key, out var raw)
            && Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool? ResolveBooleanAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, string key)
    {
        if (!TryGetAnnotationValue(annotations, key, out var raw))
        {
            return null;
        }

        if (raw is bool flag)
        {
            return flag;
        }

        if (raw is string text && bool.TryParse(text, out var parsed))
        {
            return parsed;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "EFCORE_INVALID_ANNOTATION_VALUE",
            $"Annotation '{key}' must be a boolean value.",
            modelPath);
        return null;
    }

    private static AnnotationBag BuildPropertyAnnotations(PropertyDefinition property, bool isNullable)
    {
        return AppendAnnotations(
            property.Annotations,
            CreateAnnotation("schema.isRequired", property.Cardinality.IsRequired, AnnotationScope.Projection, AnnotationSource.Generated),
            CreateAnnotation("schema.isOptional", !property.Cardinality.IsRequired, AnnotationScope.Projection, AnnotationSource.Generated),
            CreateAnnotation("schema.allowsNull", isNullable, AnnotationScope.Projection, AnnotationSource.Generated),
            CreateAnnotation("dotnet.nullability", isNullable ? "Nullable" : "NonNullable", AnnotationScope.Projection, AnnotationSource.Generated));
    }

    private static Annotation CreateAnnotation(string key, object? value, AnnotationScope scope, AnnotationSource source)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = scope,
            Source = source,
        };
    }

    private static AnnotationBag AppendAnnotations(AnnotationBag source, params Annotation[] annotations)
    {
        return new AnnotationBag
        {
            Items = [.. source.Items, .. annotations],
        };
    }

    private sealed class ProjectedEntityInfo
    {
        public required TypeId SourceTypeId { get; init; }

        public required string EntityName { get; init; }

        public string? TableName { get; init; }

        public string? SchemaName { get; init; }

        public bool IsOwned { get; init; }

        public required List<EfPropertyDefinition> Properties { get; init; }

        public required IReadOnlyList<EfKeyDefinition> Keys { get; init; }

        public required List<EfRelationshipDefinition> Relationships { get; init; }

        public required IReadOnlyList<EfIndexDefinition> Indexes { get; init; }

        public EfInheritanceDefinition? Inheritance { get; init; }

        public required HashSet<string> RelationshipNames { get; init; }

        public required IReadOnlySet<PropertyId> KeyPropertyIds { get; init; }

        public required IReadOnlyDictionary<PropertyId, string> ProjectedPropertyNamesById { get; init; }

        public required AnnotationBag Annotations { get; init; }

        public EfEntityTypeDefinition ToDefinition()
        {
            _ = KeyPropertyIds;
            return new EfEntityTypeDefinition
            {
                Name = EntityName,
                TableName = TableName,
                SchemaName = SchemaName,
                Properties = Properties.ToArray(),
                Keys = Keys,
                Relationships = Relationships.ToArray(),
                Indexes = Indexes,
                Inheritance = Inheritance,
                IsOwned = IsOwned,
                Annotations = Annotations,
            };
        }
    }
}
#pragma warning restore CA1859
#pragma warning restore CA1822
#pragma warning restore IDE0305
#pragma warning restore IDE0072
#pragma warning restore IDE0046
