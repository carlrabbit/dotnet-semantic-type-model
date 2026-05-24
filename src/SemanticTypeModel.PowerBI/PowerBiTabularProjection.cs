#pragma warning disable IDE0046
#pragma warning disable IDE0072
#pragma warning disable IDE0305
#pragma warning disable CA1822
#pragma warning disable CA1826
#pragma warning disable CA1859
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Projects canonical hardened type models to a TOM-like tabular metadata model.
/// </summary>
public sealed class PowerBiTabularProjection : ISchemaProjection<TabularModelDefinition>
{
    private static readonly HashSet<EntityRole> TableRoles =
    [
        EntityRole.Fact,
        EntityRole.Dimension,
        EntityRole.Lookup,
        EntityRole.Entity,
    ];

    private readonly PowerBiProjectionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PowerBiTabularProjection"/> class.
    /// </summary>
    /// <param name="options">Projection options.</param>
    public PowerBiTabularProjection(PowerBiProjectionOptions? options = null)
    {
        _options = options ?? PowerBiProjectionOptions.Default;
    }

    /// <summary>
    /// Projects a canonical hardened model into TOM-like metadata.
    /// </summary>
    /// <param name="model">The source model.</param>
    /// <param name="context">Projection context carrying diagnostic sink state.</param>
    /// <returns>Projected TOM-like model definition.</returns>
    public TabularModelDefinition Project(TypeSchemaModel model, SchemaProjectionContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        IList<SchemaDiagnostic> diagnostics = context.Diagnostics;
        var projectedTables = new List<ProjectedTableInfo>();
        var tableNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            if (!ShouldProjectAsTable(objectType))
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "POWERBI_OBJECT_NOT_PROJECTED",
                    $"Object type '{objectType.Name}' was not projected as a table because table role metadata is missing and unannotated table projection is disabled.",
                    ModelPath.ForType(objectType.Id));
                continue;
            }

            string desiredName = ResolveName(
                objectType.Annotations,
                objectType.DisplayName,
                objectType.Name,
                ModelPath.ForType(objectType.Id),
                diagnostics,
                "tom.tableName",
                "powerBi.tableName");

            string? resolvedTableName = ResolveUniqueName(
                desiredName,
                tableNameSet,
                "table",
                ModelPath.ForType(objectType.Id),
                diagnostics);
            if (resolvedTableName is null)
            {
                continue;
            }

            projectedTables.Add(ProjectTable(model, objectType, resolvedTableName, diagnostics));
        }

        IReadOnlyDictionary<TypeId, ProjectedTableInfo> tablesByTypeId = projectedTables.ToDictionary(static table => table.SourceTypeId);
        IReadOnlyList<TabularRelationshipDefinition> relationships = ProjectRelationships(model, tablesByTypeId, diagnostics);

        return new TabularModelDefinition
        {
            Name = model.Id.Value,
            Tables = projectedTables.Select(static table => table.Table).ToArray(),
            Relationships = relationships,
            Diagnostics = diagnostics.ToArray(),
        };
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
            ProjectionTarget = ProjectionTarget.PowerBi,
            ModelPath = modelPath,
            RelatedModelPaths = relatedPaths ?? [],
        });
    }

    private bool ShouldProjectAsTable(ObjectTypeDefinition objectType)
    {
        bool hasExplicitTableRole = TryGetStringAnnotation(objectType.Annotations, "powerBi.tableRole", out _);
        bool isValueObject = objectType.Semantics.IsValueObject || objectType.Semantics.Role == EntityRole.ValueObject;
        if (hasExplicitTableRole)
        {
            return true;
        }

        if (isValueObject)
        {
            return false;
        }

        if (TableRoles.Contains(objectType.Semantics.Role))
        {
            return true;
        }

        return _options.ProjectUnannotatedObjectsAsTables;
    }

    private ProjectedTableInfo ProjectTable(
        TypeSchemaModel model,
        ObjectTypeDefinition objectType,
        string tableName,
        IList<SchemaDiagnostic> diagnostics)
    {
        string tablePath = ModelPath.ForType(objectType.Id);
        var keyPropertyIds = objectType.Keys.SelectMany(static key => key.Properties).Select(static property => property.Id).ToHashSet();
        var columns = new List<TabularColumnDefinition>();
        var columnNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columnNameByProperty = new Dictionary<PropertyId, string>();

        foreach (PropertyDefinition property in objectType.Properties)
        {
            foreach (TabularColumnDefinition projectedColumn in ProjectProperty(model, objectType, property, keyPropertyIds, diagnostics))
            {
                string? resolvedColumnName = ResolveUniqueName(
                    projectedColumn.Name,
                    columnNameSet,
                    "column",
                    ModelPath.ForProperty(objectType.Id, property.Name),
                    diagnostics);
                if (resolvedColumnName is null)
                {
                    continue;
                }

                TabularColumnDefinition finalColumn = projectedColumn with { Name = resolvedColumnName };
                columns.Add(finalColumn);
                if (!columnNameByProperty.ContainsKey(property.Id))
                {
                    columnNameByProperty[property.Id] = finalColumn.Name;
                }
            }
        }

        IReadOnlyList<TabularMeasureDefinition> measures = ProjectMeasures(objectType, diagnostics);
        string? displayFolder = ResolveStringAnnotation(
            objectType.Annotations,
            tablePath,
            diagnostics,
            "powerBi.displayFolder",
            "ui.category");

        return new ProjectedTableInfo
        {
            SourceTypeId = objectType.Id,
            Table = new TabularTableDefinition
            {
                Name = tableName,
                Columns = columns,
                Measures = measures,
                Description = objectType.Description,
                DisplayFolder = displayFolder,
                Annotations = objectType.Annotations,
            },
            KeyPropertyIds = keyPropertyIds,
            ColumnNameByProperty = columnNameByProperty,
        };
    }

    private IReadOnlyList<TabularColumnDefinition> ProjectProperty(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        IReadOnlySet<PropertyId> keyPropertyIds,
        IList<SchemaDiagnostic> diagnostics)
    {
        string propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        if (model.TryGetType(property.Type.Id) is not TypeDefinition propertyType)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Error,
                "POWERBI_PROPERTY_TYPE_NOT_FOUND",
                $"Property '{property.Name}' references unknown type id '{property.Type.Id.Value}'.",
                propertyPath);
            return [];
        }

        string columnName = ResolveName(
            property.Annotations,
            property.DisplayName,
            property.Name,
            propertyPath,
            diagnostics,
            "tom.columnName",
            "powerBi.columnName");

        bool isHidden = ResolveBooleanAnnotation(property.Annotations, propertyPath, diagnostics, "powerBi.isHidden") ?? false;
        if (isHidden && !_options.IncludeHiddenColumns)
        {
            return [];
        }

        bool isNullable = property.Cardinality.AllowsNull || propertyType.Nullability.AllowsNull;
        bool isKey = keyPropertyIds.Contains(property.Id);
        string? dataCategory = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "powerBi.dataCategory");
        string? formatString = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "powerBi.formatString");

        if (propertyType is ScalarTypeDefinition scalar)
        {
            return
            [
                CreateColumn(columnName, MapScalarDataType(scalar, propertyPath, diagnostics), isNullable, isKey, isHidden, property.Description, dataCategory, formatString, property.Annotations),
            ];
        }

        if (propertyType is EnumTypeDefinition enumType)
        {
            return
            [
                CreateColumn(columnName, MapEnumDataType(enumType), isNullable, isKey, isHidden, property.Description, dataCategory, formatString, property.Annotations),
            ];
        }

        if (propertyType is ObjectTypeDefinition nestedObjectType && (nestedObjectType.Semantics.IsValueObject || nestedObjectType.Semantics.Role == EntityRole.ValueObject))
        {
            return ProjectValueObject(owner, property, nestedObjectType, isKey, isHidden, diagnostics);
        }

        if (propertyType is ObjectTypeDefinition)
        {
            return HandleUnsupportedShape(property, propertyPath, "object", diagnostics);
        }

        if (propertyType is ArrayTypeDefinition)
        {
            return HandleUnsupportedShape(property, propertyPath, "array", diagnostics);
        }

        if (propertyType is DictionaryTypeDefinition)
        {
            return HandleUnsupportedShape(property, propertyPath, "dictionary", diagnostics);
        }

        if (propertyType is UnionTypeDefinition)
        {
            return HandleUnsupportedShape(property, propertyPath, "union", diagnostics);
        }

        return HandleUnsupportedShape(property, propertyPath, propertyType.Kind.ToString(), diagnostics);
    }

    private IReadOnlyList<TabularColumnDefinition> ProjectValueObject(
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        ObjectTypeDefinition valueObjectType,
        bool isKey,
        bool isHidden,
        IList<SchemaDiagnostic> diagnostics)
    {
        string propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        if (_options.ValueObjectProjectionMode == ValueObjectProjectionMode.Diagnose)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_VALUE_OBJECT_UNSUPPORTED",
                $"Value object '{valueObjectType.Name}' cannot be projected without flatten or serialize mode.",
                propertyPath);
            return [];
        }

        if (_options.ValueObjectProjectionMode == ValueObjectProjectionMode.SerializeJson)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_VALUE_OBJECT_SERIALIZED_JSON",
                $"Value object '{valueObjectType.Name}' was serialized as JSON/string.",
                propertyPath);

            return
            [
                CreateColumn(
                    property.Name,
                    TabularDataType.String,
                    property.Cardinality.AllowsNull || valueObjectType.Nullability.AllowsNull,
                    isKey,
                    isHidden,
                    property.Description,
                    ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "powerBi.dataCategory"),
                    ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, "powerBi.formatString"),
                    property.Annotations),
            ];
        }

        var flattened = new List<TabularColumnDefinition>();
        foreach (PropertyDefinition nestedProperty in valueObjectType.Properties)
        {
            string nestedPath = ModelPath.ForProperty(valueObjectType.Id, nestedProperty.Name);
            string flattenedName = $"{property.Name}_{nestedProperty.Name}";
            if (nestedProperty.Type.Id == valueObjectType.Id)
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "POWERBI_VALUE_OBJECT_FLATTEN_RECURSIVE_UNSUPPORTED",
                    $"Recursive value object property '{nestedProperty.Name}' was not flattened.",
                    nestedPath);
                continue;
            }

            if (nestedProperty.Type.Id == owner.Id)
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "POWERBI_VALUE_OBJECT_FLATTEN_CYCLE_UNSUPPORTED",
                    $"Cyclic value object property '{nestedProperty.Name}' was not flattened.",
                    nestedPath);
                continue;
            }

            if (nestedProperty.Type.Id != default &&
                (nestedProperty.Type.Id.Value.Length == 0 || nestedProperty.Name.Length == 0))
            {
                continue;
            }

            flattened.AddRange(ProjectNestedValueObjectProperty(flattenedName, nestedProperty, diagnostics));
        }

        if (flattened.Count == 0)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_VALUE_OBJECT_FLATTEN_EMPTY",
                $"Value object '{valueObjectType.Name}' produced no flattenable columns.",
                propertyPath);
        }

        return flattened;
    }

    private IReadOnlyList<TabularColumnDefinition> ProjectNestedValueObjectProperty(
        string flattenedName,
        PropertyDefinition nestedProperty,
        IList<SchemaDiagnostic> diagnostics)
    {
        string nestedPath = nestedProperty.Id.Value;
        if (nestedProperty.Type.Id == default)
        {
            return [];
        }

        return
        [
            CreateColumn(
                flattenedName,
                TabularDataType.String,
                nestedProperty.Cardinality.AllowsNull,
                false,
                false,
                nestedProperty.Description,
                ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, "powerBi.dataCategory"),
                ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, "powerBi.formatString"),
                nestedProperty.Annotations),
        ];
    }

    private IReadOnlyList<TabularColumnDefinition> HandleUnsupportedShape(
        PropertyDefinition property,
        string propertyPath,
        string shapeName,
        IList<SchemaDiagnostic> diagnostics)
    {
        if (_options.UnsupportedShapeBehavior == UnsupportedTabularShapeBehavior.SerializeJson)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_UNSUPPORTED_SHAPE_SERIALIZED_JSON",
                $"Property '{property.Name}' with shape '{shapeName}' was serialized as JSON/string.",
                propertyPath);
            return
            [
                CreateColumn(property.Name, TabularDataType.String, true, false, false, property.Description, null, null, property.Annotations),
            ];
        }

        if (_options.UnsupportedShapeBehavior == UnsupportedTabularShapeBehavior.IgnoreWithWarning)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_UNSUPPORTED_SHAPE_IGNORED",
                $"Property '{property.Name}' with shape '{shapeName}' was skipped for tabular projection.",
                propertyPath);
            return [];
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "POWERBI_UNSUPPORTED_SHAPE",
            $"Property '{property.Name}' with shape '{shapeName}' is unsupported for tabular projection.",
            propertyPath);
        return [];
    }

    private static TabularColumnDefinition CreateColumn(
        string name,
        TabularDataType dataType,
        bool isNullable,
        bool isKey,
        bool isHidden,
        string? description,
        string? dataCategory,
        string? formatString,
        AnnotationBag annotations)
    {
        return new TabularColumnDefinition
        {
            Name = name,
            DataType = dataType,
            IsNullable = isNullable,
            IsKey = isKey,
            IsHidden = isHidden,
            Description = description,
            DataCategory = dataCategory,
            FormatString = formatString,
            Annotations = annotations,
        };
    }

    private TabularDataType MapEnumDataType(EnumTypeDefinition enumType)
    {
        if (_options.EnumProjectionMode == EnumProjectionMode.NumericWhenAvailable)
        {
            return enumType.StorageKind switch
            {
                EnumStorageKind.Integer => TabularDataType.Int64,
                EnumStorageKind.Number => TabularDataType.Double,
                _ => TabularDataType.String,
            };
        }

        return TabularDataType.String;
    }

    private TabularDataType MapScalarDataType(ScalarTypeDefinition scalar, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        return scalar.ScalarKind switch
        {
            ScalarKind.Boolean => TabularDataType.Boolean,
            ScalarKind.String => TabularDataType.String,
            ScalarKind.Integer => TabularDataType.Int64,
            ScalarKind.Number => _options.NumericProjectionMode == NumericProjectionMode.DecimalWhenDefined && scalar.Precision is not null
                ? TabularDataType.Decimal
                : TabularDataType.Double,
            ScalarKind.Decimal => TabularDataType.Decimal,
            ScalarKind.Date => TabularDataType.Date,
            ScalarKind.Time => TabularDataType.Time,
            ScalarKind.DateTime => TabularDataType.DateTime,
            ScalarKind.DateTimeOffset => ReportLossyType(scalar, "POWERBI_LOSSY_DATETIMEOFFSET_MAPPING", "DateTimeOffset was projected as DateTime and offset semantics are lost.", TabularDataType.DateTime, modelPath, diagnostics),
            ScalarKind.Duration => ReportLossyType(scalar, "POWERBI_LOSSY_DURATION_MAPPING", "Duration was projected as String.", TabularDataType.String, modelPath, diagnostics),
            ScalarKind.Guid => TabularDataType.String,
            ScalarKind.Binary => ReportLossyType(scalar, "POWERBI_LOSSY_BINARY_MAPPING", "Binary was projected as Binary without provider-specific guarantees.", TabularDataType.Binary, modelPath, diagnostics),
            ScalarKind.Json => ReportLossyType(scalar, "POWERBI_LOSSY_JSON_MAPPING", "Json was projected as String.", TabularDataType.String, modelPath, diagnostics),
            _ => ReportLossyType(scalar, "POWERBI_UNKNOWN_SCALAR_MAPPING", $"Scalar kind '{scalar.ScalarKind}' was projected as String.", TabularDataType.String, modelPath, diagnostics),
        };
    }

    private static TabularDataType ReportLossyType(
        ScalarTypeDefinition scalar,
        string code,
        string message,
        TabularDataType dataType,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics)
    {
        _ = scalar;
        Report(diagnostics, SchemaDiagnosticSeverity.Warning, code, message, modelPath);
        return dataType;
    }

    private IReadOnlyList<TabularMeasureDefinition> ProjectMeasures(ObjectTypeDefinition objectType, IList<SchemaDiagnostic> diagnostics)
    {
        string typePath = ModelPath.ForType(objectType.Id);
        var measures = new List<TabularMeasureDefinition>();
        var measureNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ComputedMemberDefinition member in objectType.ComputedMembers)
        {
            string memberPath = ModelPath.ForComputedMember(objectType.Id, member.Name);
            bool isDax = string.Equals(member.Expression.Language, "DAX", StringComparison.OrdinalIgnoreCase);
            if (!isDax && !_options.PreserveUnsupportedMeasureExpressions)
            {
                Report(
                    diagnostics,
                    SchemaDiagnosticSeverity.Warning,
                    "POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE",
                    $"Computed member '{member.Name}' uses unsupported expression language '{member.Expression.Language}'.",
                    memberPath);
                continue;
            }

            string? expressionOverride = ResolveStringAnnotation(member.Annotations, memberPath, diagnostics, "tom.measureExpression");
            string expression = string.IsNullOrWhiteSpace(expressionOverride) ? member.Expression.Body : expressionOverride;
            string desiredName = member.Name;
            string? resolvedName = ResolveUniqueName(desiredName, measureNameSet, "measure", memberPath, diagnostics);
            if (resolvedName is null)
            {
                continue;
            }

            measures.Add(new TabularMeasureDefinition
            {
                Name = resolvedName,
                Expression = expression,
                ExpressionLanguage = member.Expression.Language,
                DisplayFolder = ResolveStringAnnotation(member.Annotations, memberPath, diagnostics, "powerBi.displayFolder", "ui.category"),
                FormatString = ResolveStringAnnotation(member.Annotations, memberPath, diagnostics, "tom.measureFormatString", "powerBi.formatString"),
                Description = ResolveStringAnnotation(member.Annotations, memberPath, diagnostics, "schema.description"),
            });
        }

        _ = typePath;
        return measures;
    }

    private IReadOnlyList<TabularRelationshipDefinition> ProjectRelationships(
        TypeSchemaModel model,
        IReadOnlyDictionary<TypeId, ProjectedTableInfo> tablesByTypeId,
        IList<SchemaDiagnostic> diagnostics)
    {
        var projectedRelationships = new List<TabularRelationshipDefinition>();
        var relationshipNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (RelationshipDefinition relationship in objectType.Relationships)
            {
                string relationshipPath = ModelPath.ForRelationship(objectType.Id, relationship.Id);
                if (!dedupe.Add(relationship.Id.Value))
                {
                    continue;
                }

                if (relationship.Cardinality == RelationshipCardinality.ManyToMany)
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_MANY_TO_MANY_RELATIONSHIP_UNSUPPORTED",
                        $"Relationship '{relationship.Id.Value}' is many-to-many and is not projected by default.",
                        relationshipPath);
                    continue;
                }

                if (!tablesByTypeId.TryGetValue(relationship.DependentType.Id, out ProjectedTableInfo? fromTable))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_TABLE_NOT_PROJECTED",
                        $"Dependent relationship endpoint '{relationship.DependentType.Id.Value}' is not projected as a table.",
                        relationshipPath);
                    continue;
                }

                if (!tablesByTypeId.TryGetValue(relationship.PrincipalType.Id, out ProjectedTableInfo? toTable))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_TABLE_NOT_PROJECTED",
                        $"Principal relationship endpoint '{relationship.PrincipalType.Id.Value}' is not projected as a table.",
                        relationshipPath);
                    continue;
                }

                if (relationship.DependentProperties.Count == 0 || relationship.PrincipalProperties.Count == 0)
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_PROPERTY_MISSING",
                        $"Relationship '{relationship.Id.Value}' does not include both endpoint properties.",
                        relationshipPath);
                    continue;
                }

                PropertyId fromPropertyId = relationship.DependentProperties[0].Id;
                PropertyId toPropertyId = relationship.PrincipalProperties[0].Id;

                if (!fromTable.ColumnNameByProperty.TryGetValue(fromPropertyId, out string? fromColumn))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_COLUMN_NOT_PROJECTED",
                        $"Dependent relationship endpoint property '{fromPropertyId.Value}' was not projected as a column.",
                        relationshipPath);
                    continue;
                }

                if (!toTable.ColumnNameByProperty.TryGetValue(toPropertyId, out string? toColumn))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_COLUMN_NOT_PROJECTED",
                        $"Principal relationship endpoint property '{toPropertyId.Value}' was not projected as a column.",
                        relationshipPath);
                    continue;
                }

                if (!toTable.KeyPropertyIds.Contains(toPropertyId))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_MISSING_KEY",
                        $"Principal property '{toPropertyId.Value}' is not part of a key.",
                        relationshipPath);
                }

                string desiredName = ResolveName(
                    relationship.Annotations,
                    null,
                    relationship.Id.Value,
                    relationshipPath,
                    diagnostics,
                    "tom.relationshipName");
                string? resolvedName = ResolveUniqueName(desiredName, relationshipNameSet, "relationship", relationshipPath, diagnostics);
                if (resolvedName is null)
                {
                    continue;
                }

                bool isActive = ResolveBooleanAnnotation(relationship.Annotations, relationshipPath, diagnostics, "powerBi.isActive") ?? true;
                projectedRelationships.Add(new TabularRelationshipDefinition
                {
                    Name = resolvedName,
                    FromTable = fromTable.Table.Name,
                    FromColumn = fromColumn,
                    ToTable = toTable.Table.Name,
                    ToColumn = toColumn,
                    Cardinality = MapCardinality(relationship.Cardinality),
                    IsActive = isActive,
                });
            }
        }

        return projectedRelationships;
    }

    private static TabularRelationshipCardinality MapCardinality(RelationshipCardinality cardinality)
    {
        return cardinality switch
        {
            RelationshipCardinality.OneToOne => TabularRelationshipCardinality.OneToOne,
            RelationshipCardinality.OneToMany => TabularRelationshipCardinality.OneToMany,
            RelationshipCardinality.ManyToOne => TabularRelationshipCardinality.ManyToOne,
            RelationshipCardinality.ManyToMany => TabularRelationshipCardinality.ManyToMany,
            _ => TabularRelationshipCardinality.ManyToOne,
        };
    }

    private string ResolveName(
        AnnotationBag annotations,
        string? displayName,
        string canonicalName,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics,
        params string[] annotationKeys)
    {
        foreach (string key in annotationKeys)
        {
            if (!TryGetAnnotationValue(annotations, key, out object? value))
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
                "POWERBI_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a non-empty string.",
                modelPath);
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        return canonicalName;
    }

    private string? ResolveUniqueName(
        string desiredName,
        HashSet<string> usedNames,
        string scope,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics)
    {
        if (usedNames.Add(desiredName))
        {
            return desiredName;
        }

        if (_options.NameCollisionBehavior == NameCollisionBehavior.Suffix)
        {
            int suffix = 2;
            string candidate = desiredName;
            while (!usedNames.Add(candidate))
            {
                candidate = $"{desiredName}_{suffix}";
                suffix++;
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_NAME_COLLISION_SUFFIX_APPLIED",
                $"Duplicate {scope} name '{desiredName}' was renamed to '{candidate}'.",
                modelPath);
            return candidate;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Error,
            "POWERBI_DUPLICATE_PROJECTED_NAME",
            $"Duplicate {scope} name '{desiredName}' was detected.",
            modelPath);
        return null;
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

    private static bool TryGetStringAnnotation(AnnotationBag annotations, string key, out string value)
    {
        if (TryGetAnnotationValue(annotations, key, out object? raw) && raw is string text && !string.IsNullOrWhiteSpace(text))
        {
            value = text.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? ResolveStringAnnotation(
        AnnotationBag annotations,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics,
        params string[] keys)
    {
        foreach (string key in keys)
        {
            if (!TryGetAnnotationValue(annotations, key, out object? raw))
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
                "POWERBI_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a non-empty string.",
                modelPath);
        }

        return null;
    }

    private static bool? ResolveBooleanAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, string key)
    {
        if (!TryGetAnnotationValue(annotations, key, out object? raw))
        {
            return null;
        }

        if (raw is bool flag)
        {
            return flag;
        }

        if (raw is string text && bool.TryParse(text, out bool parsed))
        {
            return parsed;
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "POWERBI_INVALID_ANNOTATION_VALUE",
            $"Annotation '{key}' must be a boolean value.",
            modelPath);
        return null;
    }

    private sealed record ProjectedTableInfo
    {
        public required TypeId SourceTypeId { get; init; }

        public required TabularTableDefinition Table { get; init; }

        public required IReadOnlySet<PropertyId> KeyPropertyIds { get; init; }

        public required IReadOnlyDictionary<PropertyId, string> ColumnNameByProperty { get; init; }
    }
}
#pragma warning restore CA1859
#pragma warning restore CA1826
#pragma warning restore CA1822
#pragma warning restore IDE0305
#pragma warning restore IDE0072
#pragma warning restore IDE0046
