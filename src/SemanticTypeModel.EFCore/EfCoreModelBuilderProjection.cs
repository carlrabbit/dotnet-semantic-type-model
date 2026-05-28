using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Configures runtime EF Core projection behavior for <see cref="SemanticTypeModelEfCoreExtensions.ApplySemanticTypeModel(ModelBuilder, TypeSchemaModel, Action{EfCoreModelBuilderProjectionOptions}?)"/>.
/// </summary>
public sealed class EfCoreModelBuilderProjectionOptions
{
    /// <summary>
    /// Gets or sets the default database schema applied when projected entity definitions do not declare one.
    /// </summary>
    public string? DefaultSchema { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether unannotated object types can become entity types.
    /// </summary>
    public bool ProjectUnannotatedObjectsAsEntities { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether entity candidates may be projected without a primary key.
    /// </summary>
    public bool AllowKeylessEntities { get; set; }

    /// <summary>
    /// Gets or sets how nested value objects are represented.
    /// </summary>
    public ValueObjectEfProjectionMode ValueObjectProjectionMode { get; set; } = ValueObjectEfProjectionMode.Diagnose;

    /// <summary>
    /// Gets or sets how unsupported array, dictionary, union, and nested object shapes are handled.
    /// </summary>
    public UnsupportedEfShapeBehavior UnsupportedShapeBehavior { get; set; } = UnsupportedEfShapeBehavior.Diagnose;

    /// <summary>
    /// Gets or sets how enum properties are stored.
    /// </summary>
    public EnumEfProjectionMode EnumProjectionMode { get; set; } = EnumEfProjectionMode.String;

    /// <summary>
    /// Gets or sets how alternate, natural, and external keys are represented.
    /// </summary>
    public AlternateKeyProjectionMode AlternateKeyProjectionMode { get; set; } = AlternateKeyProjectionMode.AlternateKey;

    /// <summary>
    /// Gets or sets a value indicating whether display names participate in table and column naming precedence.
    /// </summary>
    public bool PreferDisplayNamesForTableAndColumnNames { get; set; }

    /// <summary>
    /// Gets or sets duplicate projected-name handling behavior.
    /// </summary>
    public NameCollisionBehavior NameCollisionBehavior { get; set; } = NameCollisionBehavior.Diagnose;

    internal EfCoreProjectionOptions ToProjectionOptions()
    {
        return new EfCoreProjectionOptions
        {
            ProjectUnannotatedObjectsAsEntities = ProjectUnannotatedObjectsAsEntities,
            AllowKeylessEntities = AllowKeylessEntities,
            ValueObjectProjectionMode = ValueObjectProjectionMode,
            UnsupportedShapeBehavior = UnsupportedShapeBehavior,
            EnumProjectionMode = EnumProjectionMode,
            AlternateKeyProjectionMode = AlternateKeyProjectionMode,
            PreferDisplayNamesForTableAndColumnNames = PreferDisplayNamesForTableAndColumnNames,
            NameCollisionBehavior = NameCollisionBehavior,
        };
    }
}

/// <summary>
/// Represents the EF Core <see cref="ModelBuilder"/> projection result.
/// </summary>
public sealed record EfCoreModelBuilderProjectionResult
{
    /// <summary>
    /// Gets the canonical EF projection metadata generated from the semantic model.
    /// </summary>
    public required EfModelDefinition Model { get; init; }

    /// <summary>
    /// Gets diagnostics produced while projecting the semantic model.
    /// </summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

/// <summary>
/// Provides EF Core projection extension methods for <see cref="ModelBuilder"/>.
/// </summary>
public static class SemanticTypeModelEfCoreExtensions
{
    /// <summary>
    /// Projects a canonical semantic model into <see cref="ModelBuilder"/> configuration and returns the projection result.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder to configure.</param>
    /// <param name="model">The canonical semantic model.</param>
    /// <param name="configure">Optional callback to configure projection options.</param>
    /// <returns>The projection result containing metadata and diagnostics.</returns>
    public static EfCoreModelBuilderProjectionResult ApplySemanticTypeModel(
        this ModelBuilder modelBuilder,
        TypeSchemaModel model,
        Action<EfCoreModelBuilderProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(model);

        var options = new EfCoreModelBuilderProjectionOptions();
        configure?.Invoke(options);

        var projectionContext = new SchemaProjectionContext { Target = ProjectionTarget.EfCore };
        EfModelDefinition projectedModel = new EfCoreModelProjection(options.ToProjectionOptions()).Project(model, projectionContext);
        ApplyProjectedModel(modelBuilder, projectedModel, options.DefaultSchema);

        return new EfCoreModelBuilderProjectionResult
        {
            Model = projectedModel,
            Diagnostics = projectedModel.Diagnostics,
        };
    }

    private static void ApplyProjectedModel(ModelBuilder modelBuilder, EfModelDefinition model, string? defaultSchema)
    {
        foreach (EfEntityTypeDefinition entity in model.EntityTypes.Where(static entity => !entity.IsOwned))
        {
            EntityTypeBuilder entityBuilder = modelBuilder.SharedTypeEntity<Dictionary<string, object>>(entity.Name);
            ApplyTable(entityBuilder, entity, defaultSchema);
            ApplyProperties(entityBuilder, entity.Properties);
            ApplyKeysAndIndexes(entityBuilder, entity.Keys);
        }
    }

    private static void ApplyTable(EntityTypeBuilder entityBuilder, EfEntityTypeDefinition entity, string? defaultSchema)
    {
        var schemaName = entity.SchemaName ?? defaultSchema;
        if (!string.IsNullOrWhiteSpace(entity.TableName))
        {
            _ = !string.IsNullOrWhiteSpace(schemaName)
                ? entityBuilder.ToTable(entity.TableName, schemaName)
                : entityBuilder.ToTable(entity.TableName);

            return;
        }

        if (!string.IsNullOrWhiteSpace(defaultSchema))
        {
            _ = entityBuilder.ToTable(entity.Name, defaultSchema);
        }
    }

    private static void ApplyProperties(EntityTypeBuilder entityBuilder, IReadOnlyList<EfPropertyDefinition> properties)
    {
        foreach (EfPropertyDefinition property in properties)
        {
            PropertyBuilder propertyBuilder = entityBuilder.IndexerProperty(property.ClrType, property.Name);
            _ = propertyBuilder.IsRequired(!property.IsNullable);

            if (property.MaxLength is int maxLength)
            {
                _ = propertyBuilder.HasMaxLength(maxLength);
            }

            if (property.Precision?.Precision is int precision)
            {
                _ = property.Precision.Scale is int scale
                    ? propertyBuilder.HasPrecision(precision, scale)
                    : propertyBuilder.HasPrecision(precision);
            }
        }
    }

    private static void ApplyKeysAndIndexes(EntityTypeBuilder entityBuilder, IReadOnlyList<EfKeyDefinition> keys)
    {
        foreach (EfKeyDefinition key in keys)
        {
            if (key.PropertyNames.Count == 0)
            {
                continue;
            }

            if (key.Kind == EfKeyKind.Primary)
            {
                _ = entityBuilder.HasKey([.. key.PropertyNames]).HasName(key.Name);
                continue;
            }

            if (key.Kind == EfKeyKind.Alternate)
            {
                _ = entityBuilder.HasAlternateKey([.. key.PropertyNames]).HasName(key.Name);
                continue;
            }

            if (key.Kind == EfKeyKind.UniqueIndex)
            {
                _ = entityBuilder.HasIndex([.. key.PropertyNames]).IsUnique().HasDatabaseName(key.Name);
            }
        }
    }
}
