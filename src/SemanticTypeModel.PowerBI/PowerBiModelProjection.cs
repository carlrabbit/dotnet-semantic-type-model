// These warnings are disabled for this prototype file to keep deterministic projection logic explicit and
// readable while preserving stable contracts and analyzer-clean builds under repository-wide warning-as-error policy.
#pragma warning disable IDE0046
#pragma warning disable IDE0072
#pragma warning disable IDE0305
#pragma warning disable CA1822
#pragma warning disable CA1826
#pragma warning disable CA1859
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Diagnostics;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.PowerBI;

/// <summary>
/// Projects canonical semantic models to a Power BI metadata model.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PowerBiModelProjection"/> class.
/// </remarks>
/// <param name="options">Projection options.</param>
public sealed class PowerBiModelProjection(PowerBiProjectionOptions? options = null) : ISchemaProjection<PowerBiProjectionModel>, IProjectionCapabilityProvider
{
    private static readonly HashSet<EntityRole> TableRoles =
    [
        EntityRole.Fact,
        EntityRole.Dimension,
        EntityRole.Lookup,
        EntityRole.Entity,
    ];

    private readonly PowerBiProjectionOptions _options = options ?? PowerBiProjectionOptions.Default;

    /// <summary>
    /// Projects a canonical canonical semantic model into Power BI metadata.
    /// </summary>
    /// <param name="model">The source model.</param>
    /// <param name="context">Projection context carrying diagnostic sink state.</param>
    /// <returns>Projected Power BI model definition.</returns>
    public PowerBiProjectionModel Project(TypeSchemaModel model, SchemaProjectionContext context)
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

            var desiredName = ResolveName(
                objectType.Annotations,
                _options.NamingPolicy == PowerBiNamingPolicy.DisplayName ? objectType.DisplayName : null,
                objectType.Name,
                ModelPath.ForType(objectType.Id),
                diagnostics,
                "tom.tableName",
                PowerBiAnnotationNames.TableName);

            var resolvedTableName = ResolveUniqueName(
                desiredName,
                tableNameSet,
                "table",
                ModelPath.ForType(objectType.Id),
                diagnostics);
            if (resolvedTableName is null)
            {
                continue;
            }

            IReadOnlySet<PropertyId> foreignKeyPropertyIds = GetForeignKeyPropertyIds(objectType);
            projectedTables.Add(ProjectTable(model, objectType, resolvedTableName, foreignKeyPropertyIds, diagnostics));
        }

        IReadOnlyDictionary<TypeId, ProjectedTableInfo> tablesByTypeId = projectedTables.ToDictionary(static table => table.SourceTypeId);
        IReadOnlyList<PowerBiRelationshipDefinition> relationships = ProjectRelationships(model, tablesByTypeId, diagnostics);

        return new PowerBiProjectionModel
        {
            Name = model.Id.Value,
            Tables = projectedTables.Select(static table => table.Table).ToArray(),
            Relationships = relationships,
            Diagnostics = diagnostics.ToArray(),
        };
    }

    /// <inheritdoc />
    public ProjectionCompatibilityContract GetCapabilities()
    {
        return ProjectionCapabilityCatalog.ForTarget(ProjectionTarget.PowerBi);
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
        var hasExplicitTableRole = TryGetStringAnnotation(objectType.Annotations, PowerBiAnnotationNames.TableRole, out _);
        var isValueObject = objectType.Semantics.IsValueObject || objectType.Semantics.Role == EntityRole.ValueObject;
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
        IReadOnlySet<PropertyId> foreignKeyPropertyIds,
        IList<SchemaDiagnostic> diagnostics)
    {
        var tablePath = ModelPath.ForType(objectType.Id);
        var keyPropertyIds = objectType.Keys.SelectMany(static key => key.Properties).Select(static property => property.Id).ToHashSet();
        var columns = new List<PowerBiColumnDefinition>();
        var columnNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columnNameByProperty = new Dictionary<PropertyId, string>();

        foreach (PropertyDefinition property in objectType.Properties)
        {
            foreach (PowerBiColumnDefinition projectedColumn in ProjectProperty(model, objectType, property, keyPropertyIds, foreignKeyPropertyIds, diagnostics))
            {
                var resolvedColumnName = ResolveUniqueName(
                    projectedColumn.Name,
                    columnNameSet,
                    "column",
                    ModelPath.ForProperty(objectType.Id, property.Name),
                    diagnostics);
                if (resolvedColumnName is null)
                {
                    continue;
                }

                PowerBiColumnDefinition finalColumn = projectedColumn with { Name = resolvedColumnName };
                columns.Add(finalColumn);
                if (!columnNameByProperty.ContainsKey(property.Id))
                {
                    columnNameByProperty[property.Id] = finalColumn.Name;
                }
            }
        }

        ValidateSortByColumns(objectType, columns, columnNameSet, diagnostics);

        IReadOnlyList<PowerBiMeasureDefinition> measures = ProjectMeasures(objectType, diagnostics);
        var displayFolder = ResolveStringAnnotation(
            objectType.Annotations,
            tablePath,
            diagnostics,
            PowerBiAnnotationNames.DisplayFolder,
            "ui.category");

        return new ProjectedTableInfo
        {
            SourceTypeId = objectType.Id,
            Table = new PowerBiTableDefinition
            {
                Name = tableName,
                Columns = columns,
                Measures = measures,
                DisplayName = objectType.DisplayName,
                Description = objectType.Description,
                DisplayFolder = displayFolder,
                Role = ResolveTableRole(objectType, tablePath, diagnostics),
                IsHidden = ResolveBooleanAnnotation(objectType.Annotations, tablePath, diagnostics, PowerBiAnnotationNames.Hidden, "powerBi.hidden") ?? false,
                SourceTypeId = objectType.Id,
                Annotations = objectType.Annotations,
            },
            KeyPropertyIds = keyPropertyIds,
            ColumnNameByProperty = columnNameByProperty,
        };
    }


    private static void ValidateSortByColumns(
        ObjectTypeDefinition objectType,
        IReadOnlyList<PowerBiColumnDefinition> columns,
        HashSet<string> columnNameSet,
        IList<SchemaDiagnostic> diagnostics)
    {
        foreach (PowerBiColumnDefinition column in columns)
        {
            if (string.IsNullOrWhiteSpace(column.SortByColumn))
            {
                continue;
            }

            if (columnNameSet.Contains(column.SortByColumn))
            {
                continue;
            }

            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_SORT_BY_COLUMN_NOT_FOUND",
                $"Column '{column.Name}' references unresolved sort-by-column '{column.SortByColumn}'.",
                ModelPath.ForProperty(objectType.Id, column.SourcePropertyId?.Value ?? column.Name));
        }
    }

    private IReadOnlyList<PowerBiColumnDefinition> ProjectProperty(
        TypeSchemaModel model,
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        IReadOnlySet<PropertyId> keyPropertyIds,
        IReadOnlySet<PropertyId> foreignKeyPropertyIds,
        IList<SchemaDiagnostic> diagnostics)
    {
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
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

        var columnName = ResolveName(
            property.Annotations,
            _options.NamingPolicy == PowerBiNamingPolicy.DisplayName ? property.DisplayName : null,
            property.Name,
            propertyPath,
            diagnostics,
            "tom.columnName",
            PowerBiAnnotationNames.ColumnName);

        var isHidden = ResolveBooleanAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.Hidden, "powerBi.hidden") ?? false;
        if (isHidden && !_options.IncludeHiddenColumns)
        {
            return [];
        }

        var isNullable = property.Cardinality.AllowsNull || propertyType.Nullability.AllowsNull;
        var isKey = keyPropertyIds.Contains(property.Id);
        var isForeignKey = foreignKeyPropertyIds.Contains(property.Id);
        isHidden = isHidden || (isKey && _options.HideTechnicalKeys) || (isForeignKey && _options.HideForeignKeys);
        if (isHidden && !_options.IncludeHiddenColumns)
        {
            return [];
        }

        var dataCategory = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.DataCategory);
        var formatString = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.FormatString);
        var sortByColumn = ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.SortByColumn);
        PowerBiSummarization summarization = ResolveSummarization(property.Annotations, propertyPath, isKey, propertyType, diagnostics);

        if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.ExtensionData))
        {
            return [];
        }

        if (HasBooleanAnnotation(property.Annotations, CoreSemanticAnnotationKeys.OwnedCollection))
        {
            Report(diagnostics, SchemaDiagnosticSeverity.Warning, StmDiagnosticIds.OwnedCollectionPolicyRequired, $"Owned collection '{owner.Name}.{property.Name}' requires an explicit Power BI projection policy.", propertyPath);
            return [];
        }

        if (IsEnvelopePayload(owner, property))
        {
            return ProjectEnvelopePayload(owner, property, propertyType, isHidden, diagnostics);
        }

        if (propertyType is ScalarTypeDefinition scalar)
        {
            return
            [
                CreateColumn(columnName, property.DisplayName, MapScalarDataType(scalar, propertyPath, diagnostics), isNullable, isKey, isHidden, property.Description, summarization, property.Id, dataCategory, formatString, sortByColumn, property.Annotations),
            ];
        }

        if (propertyType is EnumTypeDefinition enumType)
        {
            return
            [
                CreateColumn(columnName, property.DisplayName, MapEnumDataType(enumType), isNullable, isKey, isHidden, property.Description, summarization, property.Id, dataCategory, formatString, sortByColumn, property.Annotations),
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


    private IReadOnlyList<PowerBiColumnDefinition> ProjectEnvelopePayload(
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        TypeDefinition propertyType,
        bool isHidden,
        IList<SchemaDiagnostic> diagnostics)
    {
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
        PowerBiEnvelopeProjectionPolicy policy = ResolveEnvelopePolicy(owner, property);
        if (policy.PayloadPolicy == PowerBiEnvelopePayloadAnalyticalPolicy.Ignored)
        {
            return [];
        }

        if (policy.PayloadPolicy == PowerBiEnvelopePayloadAnalyticalPolicy.Summary)
        {
            return
            [
                CreateColumn(
                    policy.SummaryColumnName ?? $"{property.Name}Summary",
                    "Payload Summary",
                    PowerBiDataType.String,
                    true,
                    false,
                    isHidden,
                    $"Summary for payload type {propertyType.Name}.",
                    PowerBiSummarization.None,
                    property.Id,
                    null,
                    null,
                    null,
                    property.Annotations),
            ];
        }

        Report(diagnostics, SchemaDiagnosticSeverity.Warning, "POWERBI_ENVELOPE_PAYLOAD_POLICY_UNSUPPORTED", $"Envelope payload policy '{policy.PayloadPolicy}' is not supported without explicit analytical mapping.", propertyPath);
        return [];
    }

    private PowerBiEnvelopeProjectionPolicy ResolveEnvelopePolicy(ObjectTypeDefinition owner, PropertyDefinition property)
    {
        if (_options.EnvelopePolicies.TryGetValue(owner.Name, out PowerBiEnvelopeProjectionPolicy? byName) || _options.EnvelopePolicies.TryGetValue(owner.Id.Value, out byName))
        {
            return byName;
        }

        return new PowerBiEnvelopeProjectionPolicy { EnvelopeTypeName = owner.Name, PayloadPropertyName = property.Name, PayloadPolicy = PowerBiEnvelopePayloadAnalyticalPolicy.Ignored };
    }

    private static bool HasBooleanAnnotation(AnnotationBag annotations, string key)
    {
        return annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, key, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool IsEnvelopePayload(ObjectTypeDefinition owner, PropertyDefinition property)
    {
        return owner.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.Envelope, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true)
            && property.Annotations.Items.Any(annotation => string.Equals(annotation.Key.Value, CoreSemanticAnnotationKeys.EnvelopePayload, StringComparison.Ordinal) && Convert.ToString(annotation.Value, System.Globalization.CultureInfo.InvariantCulture)?.Equals("true", StringComparison.OrdinalIgnoreCase) == true);
    }

    private IReadOnlyList<PowerBiColumnDefinition> ProjectValueObject(
        ObjectTypeDefinition owner,
        PropertyDefinition property,
        ObjectTypeDefinition valueObjectType,
        bool isKey,
        bool isHidden,
        IList<SchemaDiagnostic> diagnostics)
    {
        var propertyPath = ModelPath.ForProperty(owner.Id, property.Name);
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
                    property.DisplayName,
                    PowerBiDataType.String,
                    property.Cardinality.AllowsNull || valueObjectType.Nullability.AllowsNull,
                    isKey,
                    isHidden,
                    property.Description,
                    PowerBiSummarization.None,
                    property.Id,
                    ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.DataCategory),
                    ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.FormatString),
                    ResolveStringAnnotation(property.Annotations, propertyPath, diagnostics, PowerBiAnnotationNames.SortByColumn),
                    property.Annotations),
            ];
        }

        var flattened = new List<PowerBiColumnDefinition>();
        foreach (PropertyDefinition nestedProperty in valueObjectType.Properties)
        {
            var nestedPath = ModelPath.ForProperty(valueObjectType.Id, nestedProperty.Name);
            var flattenedName = $"{property.Name}_{nestedProperty.Name}";
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

    private IReadOnlyList<PowerBiColumnDefinition> ProjectNestedValueObjectProperty(
        string flattenedName,
        PropertyDefinition nestedProperty,
        IList<SchemaDiagnostic> diagnostics)
    {
        var nestedPath = nestedProperty.Id.Value;
        if (nestedProperty.Type.Id == default)
        {
            return [];
        }

        return
        [
            CreateColumn(
                flattenedName,
                nestedProperty.DisplayName,
                PowerBiDataType.String,
                nestedProperty.Cardinality.AllowsNull,
                false,
                false,
                nestedProperty.Description,
                PowerBiSummarization.None,
                nestedProperty.Id,
                ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, PowerBiAnnotationNames.DataCategory),
                ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, PowerBiAnnotationNames.FormatString),
                ResolveStringAnnotation(nestedProperty.Annotations, nestedPath, diagnostics, PowerBiAnnotationNames.SortByColumn),
                nestedProperty.Annotations),
        ];
    }

    private IReadOnlyList<PowerBiColumnDefinition> HandleUnsupportedShape(
        PropertyDefinition property,
        string propertyPath,
        string shapeName,
        IList<SchemaDiagnostic> diagnostics)
    {
        if (_options.UnsupportedShapeBehavior == UnsupportedPowerBiShapeBehavior.SerializeJson)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_UNSUPPORTED_SHAPE_SERIALIZED_JSON",
                $"Property '{property.Name}' with shape '{shapeName}' was serialized as JSON/string.",
                propertyPath);
            return
            [
                CreateColumn(property.Name, property.DisplayName, PowerBiDataType.String, true, false, false, property.Description, PowerBiSummarization.None, property.Id, null, null, null, property.Annotations),
            ];
        }

        if (_options.UnsupportedShapeBehavior == UnsupportedPowerBiShapeBehavior.IgnoreWithWarning)
        {
            Report(
                diagnostics,
                SchemaDiagnosticSeverity.Warning,
                "POWERBI_UNSUPPORTED_SHAPE_IGNORED",
                $"Property '{property.Name}' with shape '{shapeName}' was skipped for Power BI projection.",
                propertyPath);
            return [];
        }

        Report(
            diagnostics,
            SchemaDiagnosticSeverity.Warning,
            "POWERBI_UNSUPPORTED_SHAPE",
            $"Property '{property.Name}' with shape '{shapeName}' is unsupported for Power BI projection.",
            propertyPath);
        return [];
    }

    private static PowerBiColumnDefinition CreateColumn(
        string name,
        string? displayName,
        PowerBiDataType dataType,
        bool isNullable,
        bool isKey,
        bool isHidden,
        string? description,
        PowerBiSummarization summarization,
        PropertyId? sourcePropertyId,
        string? dataCategory,
        string? formatString,
        string? sortByColumn,
        AnnotationBag annotations)
    {
        return new PowerBiColumnDefinition
        {
            Name = name,
            DisplayName = displayName,
            DataType = dataType,
            IsNullable = isNullable,
            IsKey = isKey,
            IsHidden = isHidden,
            Description = description,
            Summarization = summarization,
            SourcePropertyId = sourcePropertyId,
            DataCategory = dataCategory,
            FormatString = formatString,
            SortByColumn = sortByColumn,
            Annotations = annotations,
        };
    }

    private PowerBiDataType MapEnumDataType(EnumTypeDefinition enumType)
    {
        if (_options.EnumProjectionMode == EnumProjectionMode.NumericWhenAvailable)
        {
            return enumType.StorageKind switch
            {
                EnumStorageKind.Integer => PowerBiDataType.Int64,
                EnumStorageKind.Number => PowerBiDataType.Double,
                _ => PowerBiDataType.String,
            };
        }

        return PowerBiDataType.String;
    }

    private PowerBiDataType MapScalarDataType(ScalarTypeDefinition scalar, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        return scalar.ScalarKind switch
        {
            ScalarKind.Boolean => PowerBiDataType.Boolean,
            ScalarKind.String => PowerBiDataType.String,
            ScalarKind.Integer => PowerBiDataType.Int64,
            ScalarKind.Number => _options.NumericProjectionMode == NumericProjectionMode.DecimalWhenDefined && scalar.Precision is not null
                ? PowerBiDataType.Decimal
                : PowerBiDataType.Double,
            ScalarKind.Decimal => PowerBiDataType.Decimal,
            ScalarKind.Date => PowerBiDataType.Date,
            ScalarKind.Time => PowerBiDataType.Time,
            ScalarKind.DateTime => PowerBiDataType.DateTime,
            ScalarKind.DateTimeOffset => ReportLossyType(scalar, "POWERBI_LOSSY_DATETIMEOFFSET_MAPPING", "DateTimeOffset was projected as DateTime and offset semantics are lost.", PowerBiDataType.DateTime, modelPath, diagnostics),
            ScalarKind.Duration => ReportLossyType(scalar, "POWERBI_LOSSY_DURATION_MAPPING", "Duration was projected as String.", PowerBiDataType.String, modelPath, diagnostics),
            ScalarKind.Guid => PowerBiDataType.String,
            ScalarKind.Binary => ReportLossyType(scalar, "POWERBI_LOSSY_BINARY_MAPPING", "Binary was projected as Binary without provider-specific guarantees.", PowerBiDataType.Binary, modelPath, diagnostics),
            ScalarKind.Json => ReportLossyType(scalar, "POWERBI_LOSSY_JSON_MAPPING", "Json was projected as String.", PowerBiDataType.String, modelPath, diagnostics),
            _ => ReportLossyType(scalar, "POWERBI_UNKNOWN_SCALAR_MAPPING", $"Scalar kind '{scalar.ScalarKind}' was projected as String.", PowerBiDataType.String, modelPath, diagnostics),
        };
    }

    private static PowerBiDataType ReportLossyType(
        ScalarTypeDefinition scalar,
        string code,
        string message,
        PowerBiDataType dataType,
        string modelPath,
        IList<SchemaDiagnostic> diagnostics)
    {
        _ = scalar;
        Report(diagnostics, SchemaDiagnosticSeverity.Warning, code, message, modelPath);
        return dataType;
    }

    private IReadOnlyList<PowerBiMeasureDefinition> ProjectMeasures(ObjectTypeDefinition objectType, IList<SchemaDiagnostic> diagnostics)
    {
        var typePath = ModelPath.ForType(objectType.Id);
        var measures = new List<PowerBiMeasureDefinition>();
        var measureNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ComputedMemberDefinition member in objectType.ComputedMembers)
        {
            var memberPath = ModelPath.ForComputedMember(objectType.Id, member.Name);
            var isDax = string.Equals(member.Expression.Language, "DAX", StringComparison.OrdinalIgnoreCase);
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

            var expressionOverride = ResolveStringAnnotation(member.Annotations, memberPath, diagnostics, "tom.measureExpression");
            var expression = string.IsNullOrWhiteSpace(expressionOverride) ? member.Expression.Body : expressionOverride;
            var desiredName = member.Name;
            var resolvedName = ResolveUniqueName(desiredName, measureNameSet, "measure", memberPath, diagnostics);
            if (resolvedName is null)
            {
                continue;
            }

            measures.Add(new PowerBiMeasureDefinition
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

    private IReadOnlyList<PowerBiRelationshipDefinition> ProjectRelationships(
        TypeSchemaModel model,
        IReadOnlyDictionary<TypeId, ProjectedTableInfo> tablesByTypeId,
        IList<SchemaDiagnostic> diagnostics)
    {
        var projectedRelationships = new List<PowerBiRelationshipDefinition>();
        var relationshipNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (RelationshipDefinition relationship in objectType.Relationships)
            {
                var relationshipPath = ModelPath.ForRelationship(objectType.Id, relationship.Id);
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
                        RelationshipDiagnosticSeverity(),
                        "POWERBI_RELATIONSHIP_ENDPOINT_PROPERTY_MISSING",
                        $"Relationship '{relationship.Id.Value}' does not include both endpoint properties.",
                        relationshipPath);
                    continue;
                }

                if (relationship.DependentProperties.Count > 1 || relationship.PrincipalProperties.Count > 1)
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_AMBIGUOUS_RELATIONSHIP_ENDPOINTS",
                        $"Relationship '{relationship.Id.Value}' has composite or ambiguous endpoints and was not projected.",
                        relationshipPath);
                    continue;
                }

                PropertyId fromPropertyId = relationship.DependentProperties[0].Id;
                PropertyId toPropertyId = relationship.PrincipalProperties[0].Id;

                if (!fromTable.ColumnNameByProperty.TryGetValue(fromPropertyId, out var fromColumn))
                {
                    Report(
                        diagnostics,
                        SchemaDiagnosticSeverity.Warning,
                        "POWERBI_RELATIONSHIP_ENDPOINT_COLUMN_NOT_PROJECTED",
                        $"Dependent relationship endpoint property '{fromPropertyId.Value}' was not projected as a column.",
                        relationshipPath);
                    continue;
                }

                if (!toTable.ColumnNameByProperty.TryGetValue(toPropertyId, out var toColumn))
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

                var desiredName = ResolveName(
                    relationship.Annotations,
                    null,
                    relationship.Id.Value,
                    relationshipPath,
                    diagnostics,
                    "tom.relationshipName",
                    PowerBiAnnotationNames.RelationshipName);
                var resolvedName = ResolveUniqueName(desiredName, relationshipNameSet, "relationship", relationshipPath, diagnostics);
                if (resolvedName is null)
                {
                    continue;
                }

                var isActive = ResolveBooleanAnnotation(relationship.Annotations, relationshipPath, diagnostics, PowerBiAnnotationNames.RelationshipActive) ?? true;
                PowerBiRelationshipDirection direction = ResolveRelationshipDirection(relationship.Annotations, relationshipPath, diagnostics);
                projectedRelationships.Add(new PowerBiRelationshipDefinition
                {
                    Name = resolvedName,
                    FromTable = fromTable.Table.Name,
                    FromColumn = fromColumn,
                    ToTable = toTable.Table.Name,
                    ToColumn = toColumn,
                    Cardinality = MapCardinality(relationship.Cardinality),
                    IsActive = isActive,
                    Direction = direction,
                    SourceRelationshipId = relationship.Id,
                });
            }
        }

        return projectedRelationships;
    }

    private static PowerBiRelationshipCardinality MapCardinality(RelationshipCardinality cardinality)
    {
        return cardinality switch
        {
            RelationshipCardinality.OneToOne => PowerBiRelationshipCardinality.OneToOne,
            RelationshipCardinality.OneToMany => PowerBiRelationshipCardinality.OneToMany,
            RelationshipCardinality.ManyToOne => PowerBiRelationshipCardinality.ManyToOne,
            RelationshipCardinality.ManyToMany => PowerBiRelationshipCardinality.ManyToMany,
            _ => PowerBiRelationshipCardinality.ManyToOne,
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
        if (TryGetAnnotationValue(annotations, key, out var raw) && raw is string text && !string.IsNullOrWhiteSpace(text))
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
                "POWERBI_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a non-empty string.",
                modelPath);
        }

        return null;
    }

    private static bool? ResolveBooleanAnnotation(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!TryGetAnnotationValue(annotations, key, out var raw))
            {
                continue;
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
                "POWERBI_INVALID_ANNOTATION_VALUE",
                $"Annotation '{key}' must be a boolean value.",
                modelPath);
        }

        return null;
    }

    private static IReadOnlySet<PropertyId> GetForeignKeyPropertyIds(ObjectTypeDefinition objectType)
    {
        return objectType.Relationships
            .SelectMany(static relationship => relationship.DependentProperties)
            .Select(static property => property.Id)
            .ToHashSet();
    }

    private PowerBiTableRole ResolveTableRole(ObjectTypeDefinition objectType, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (TryGetStringAnnotation(objectType.Annotations, PowerBiAnnotationNames.TableRole, out var roleText) &&
            Enum.TryParse(roleText, ignoreCase: true, out PowerBiTableRole annotationRole))
        {
            return annotationRole;
        }

        if (!string.IsNullOrWhiteSpace(roleText))
        {
            Report(diagnostics, SchemaDiagnosticSeverity.Warning, "POWERBI_INVALID_TABLE_ROLE", $"Annotation '{PowerBiAnnotationNames.TableRole}' has unsupported table role '{roleText}'.", modelPath);
        }

        return objectType.Semantics.Role switch
        {
            EntityRole.Fact => PowerBiTableRole.Fact,
            EntityRole.Dimension or EntityRole.Lookup => PowerBiTableRole.Dimension,
            EntityRole.Entity => _options.DefaultTableRole,
            _ => _options.DefaultTableRole,
        };
    }

    private PowerBiSummarization ResolveSummarization(
        AnnotationBag annotations,
        string modelPath,
        bool isKey,
        TypeDefinition propertyType,
        IList<SchemaDiagnostic> diagnostics)
    {
        if (TryGetStringAnnotation(annotations, PowerBiAnnotationNames.Summarization, out var summarizationText) || TryGetStringAnnotation(annotations, "powerBi.summarization", out summarizationText))
        {
            if (Enum.TryParse(summarizationText, ignoreCase: true, out PowerBiSummarization parsed))
            {
                return parsed;
            }

            Report(diagnostics, SchemaDiagnosticSeverity.Warning, "POWERBI_INVALID_SUMMARIZATION", $"Annotation '{PowerBiAnnotationNames.Summarization}' has unsupported summarization '{summarizationText}'.", modelPath);
        }

        if (isKey)
        {
            return PowerBiSummarization.None;
        }

        return IsNumericType(propertyType) ? _options.DefaultNumericSummarization : PowerBiSummarization.None;
    }

    private static bool IsNumericType(TypeDefinition propertyType)
    {
        return propertyType is ScalarTypeDefinition { ScalarKind: ScalarKind.Integer or ScalarKind.Number or ScalarKind.Decimal }
            or EnumTypeDefinition { StorageKind: EnumStorageKind.Integer or EnumStorageKind.Number };
    }

    private static PowerBiRelationshipDirection ResolveRelationshipDirection(AnnotationBag annotations, string modelPath, IList<SchemaDiagnostic> diagnostics)
    {
        if (!TryGetStringAnnotation(annotations, "powerBi.relationshipDirection", out var directionText))
        {
            return PowerBiRelationshipDirection.Single;
        }

        if (Enum.TryParse(directionText, ignoreCase: true, out PowerBiRelationshipDirection direction))
        {
            return direction;
        }

        Report(diagnostics, SchemaDiagnosticSeverity.Warning, "POWERBI_INVALID_RELATIONSHIP_DIRECTION", $"Annotation 'powerBi.relationshipDirection' has unsupported relationship direction '{directionText}'.", modelPath);
        return PowerBiRelationshipDirection.Single;
    }

    private SchemaDiagnosticSeverity RelationshipDiagnosticSeverity()
    {
        return _options.TreatRelationshipsAsRequired ? SchemaDiagnosticSeverity.Error : SchemaDiagnosticSeverity.Warning;
    }

    private sealed record ProjectedTableInfo
    {
        public required TypeId SourceTypeId { get; init; }

        public required PowerBiTableDefinition Table { get; init; }

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
