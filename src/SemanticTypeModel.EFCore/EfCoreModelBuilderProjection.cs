using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Configures runtime EF Core projection behavior for <see cref="SemanticTypeModelEfCoreExtensions.ApplySemanticTypeModel(ModelBuilder, TypeSchemaModel, Action{EfCoreModelBuilderProjectionOptions}?)"/>.
/// </summary>
public sealed class EfCoreModelBuilderProjectionOptions
{
    /// <summary>Gets or sets the EF integration mode.</summary>
    public EfCoreApplicationMode ApplicationMode { get; set; } = EfCoreApplicationMode.ClrConventionAugmentation;
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
    public ValueObjectEfProjectionMode ValueObjectProjectionMode { get; set; } = ValueObjectEfProjectionMode.Flatten;

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

    /// <summary>Gets or sets the default inheritance strategy for explicit canonical inheritance.</summary>
    public EfCoreInheritanceStrategy DefaultInheritanceStrategy { get; set; } = EfCoreInheritanceStrategy.Unspecified;

    /// <summary>Gets EF Core envelope payload storage policy configuration.</summary>
    public EfCoreEnvelopeProjectionOptions Envelopes { get; } = new();

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
            DefaultInheritanceStrategy = DefaultInheritanceStrategy,
            EnvelopePolicies = Envelopes.Policies,
        };
    }
}

/// <summary>Identifies who owns the EF Core model shape.</summary>
public enum EfCoreApplicationMode
{
    /// <summary>STM creates provider-neutral shared-type entity metadata.</summary>
    SharedTypeProjection,

    /// <summary>EF discovers CLR types and STM suppresses semantic-only CLR members.</summary>
    ClrConventionAugmentation,
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
        if (options.ApplicationMode == EfCoreApplicationMode.SharedTypeProjection)
        {
            ApplyProjectedModel(modelBuilder, projectedModel, options.DefaultSchema);
        }
        else
        {
            ApplyClrConventionAugmentation(modelBuilder, model);
        }

        return new EfCoreModelBuilderProjectionResult
        {
            Model = projectedModel,
            Diagnostics = projectedModel.Diagnostics,
        };
    }

    private static void ApplyClrConventionAugmentation(ModelBuilder modelBuilder, TypeSchemaModel model)
    {
        var semanticTypes = model.Types
            .OfType<ObjectTypeDefinition>()
            .Select(type => (ClrType: ResolveClrType(type), SemanticType: type))
            .Where(static pair => pair.ClrType is not null)
            .ToDictionary(static pair => pair.ClrType!, static pair => pair.SemanticType);

        foreach ((Type ownerClrType, ObjectTypeDefinition owner) in semanticTypes)
        {
            if (modelBuilder.Model.FindEntityType(ownerClrType) is null)
            {
                continue;
            }

            foreach (PropertyDefinition property in owner.Properties.Where(IsOwnedObject))
            {
                if (model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? target)
                    && target is ObjectTypeDefinition targetObject
                    && ResolveClrType(targetObject) is Type targetClrType
                    && (targetObject.Semantics.IsValueObject || targetObject.Semantics.Role == EntityRole.ValueObject))
                {
                    var memberName = GetMemberName(property);
                    OwnedNavigationBuilder owned = modelBuilder.Entity(ownerClrType).OwnsOne(targetClrType, memberName);
                    foreach (PropertyInfo extensionData in GetSemanticExtensionDataProperties(targetClrType))
                    {
                        _ = owned.Ignore(extensionData.Name);
                    }
                }
            }
        }

        foreach (IMutableEntityType? entityType in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            Type clrType = entityType.ClrType;
            if (semanticTypes.TryGetValue(clrType, out ObjectTypeDefinition? semanticType)
                && (semanticType.Semantics.IsValueObject || semanticType.Semantics.Role == EntityRole.ValueObject)
                && !entityType.IsOwned())
            {
                throw new InvalidOperationException($"Semantic value object '{clrType.FullName}' cannot be used as a root EF entity or DbSet<T>. Map it through a semantic entity owner instead.");
            }

            if (entityType.IsOwned())
            {
                continue;
            }

            EntityTypeBuilder builder = modelBuilder.Entity(entityType.Name);
            foreach (PropertyInfo property in GetSemanticExtensionDataProperties(clrType))
            {
                _ = builder.Ignore(property.Name);
            }
        }
    }

    private static bool IsOwnedObject(PropertyDefinition property)
    {
        return property.Annotations.Items.Any(static annotation =>
            (annotation.Key.Value == "schema.ownedObject" || annotation.Key.Value == "schema.ownership")
            && string.Equals(annotation.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetMemberName(PropertyDefinition property)
    {
        return property.Annotations.Items.FirstOrDefault(static annotation => annotation.Key.Value == "dotnet.memberName")?.Value?.ToString()
            ?? property.Name;
    }

    private static IEnumerable<PropertyInfo> GetSemanticExtensionDataProperties(Type clrType)
    {
        const string attributeName = "SemanticTypeModel.DotNet.SemanticExtensionDataAttribute";
        return clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CustomAttributes.Any(attribute => string.Equals(attribute.AttributeType.FullName, attributeName, StringComparison.Ordinal)));
    }

    private static Type? ResolveClrType(ObjectTypeDefinition type)
    {
        var annotatedName = type.Annotations.Items
            .FirstOrDefault(static annotation => annotation.Key.Value == "dotnet.clrType")?.Value?.ToString();
        var name = string.IsNullOrWhiteSpace(annotatedName) ? type.Id.Value : annotatedName;
        return Type.GetType(name, throwOnError: false)
            ?? AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name, throwOnError: false)).FirstOrDefault(static candidate => candidate is not null);
    }

    /// <summary>
    /// Applies an EF Core domain semantic model to <see cref="ModelBuilder" /> configuration.
    /// </summary>
    public static void ApplyEfCoreSemanticModel(this ModelBuilder modelBuilder, EfCoreSemanticModel model, string? defaultSchema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(model);

        ApplyProjectedModel(modelBuilder, model.ToDefinition(), defaultSchema);
    }

    private static void ApplyProjectedModel(ModelBuilder modelBuilder, EfModelDefinition model, string? defaultSchema)
    {
        foreach (EfEntityTypeDefinition entity in model.EntityTypes.Where(static entity => !entity.IsOwned))
        {
            EntityTypeBuilder entityBuilder = modelBuilder.SharedTypeEntity<Dictionary<string, object>>(entity.Name);
            ApplyTable(entityBuilder, entity, defaultSchema);
            ApplyProperties(entityBuilder, entity.Properties);
            ApplyKeysAndIndexes(entityBuilder, entity.Keys, entity.Indexes);
            ApplyRelationships(entityBuilder, entity.Relationships);
            ApplyInheritance(entityBuilder, entity);
        }
    }

    private static void ApplyTable(EntityTypeBuilder entityBuilder, EfEntityTypeDefinition entity, string? defaultSchema)
    {
        var schemaName = entity.SchemaName ?? defaultSchema;
        var tableName = string.IsNullOrWhiteSpace(entity.TableName) ? entity.Name : entity.TableName;
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            _ = string.IsNullOrWhiteSpace(entity.Comment)
                ? entityBuilder.ToTable(tableName)
                : entityBuilder.ToTable(tableName, table => table.HasComment(entity.Comment));
            return;
        }

        _ = string.IsNullOrWhiteSpace(entity.Comment)
            ? entityBuilder.ToTable(tableName, schemaName)
            : entityBuilder.ToTable(tableName, schemaName, table => table.HasComment(entity.Comment));
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

            if (!string.IsNullOrWhiteSpace(property.Comment))
            {
                _ = propertyBuilder.HasComment(property.Comment);
            }

            if (!string.IsNullOrWhiteSpace(property.ColumnName) && !string.Equals(property.ColumnName, property.Name, StringComparison.Ordinal))
            {
                _ = propertyBuilder.HasColumnName(property.ColumnName);
            }

            if (property.ConverterType is not null)
            {
                _ = propertyBuilder.HasConversion(property.ConverterType);
            }
            else if (property.ProviderClrType is not null)
            {
                _ = propertyBuilder.HasConversion(property.ProviderClrType);
            }

            if (property.Precision?.Precision is int precision)
            {
                _ = property.Precision.Scale is int scale
                    ? propertyBuilder.HasPrecision(precision, scale)
                    : propertyBuilder.HasPrecision(precision);
            }
        }
    }

    private static void ApplyKeysAndIndexes(EntityTypeBuilder entityBuilder, IReadOnlyList<EfKeyDefinition> keys, IReadOnlyList<EfIndexDefinition> indexes)
    {
        foreach (EfIndexDefinition index in indexes)
        {
            if (index.PropertyNames.Count > 0)
            {
                IndexBuilder indexBuilder = entityBuilder.HasIndex([.. index.PropertyNames]).HasDatabaseName(index.Name);
                if (index.IsUnique)
                {
                    _ = indexBuilder.IsUnique();
                }
            }
        }

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
    private static void ApplyRelationships(EntityTypeBuilder entityBuilder, IReadOnlyList<EfRelationshipDefinition> relationships)
    {
        foreach (EfRelationshipDefinition relationship in relationships)
        {
            if (relationship.Cardinality == EfRelationshipCardinality.OneToOne)
            {
                ReferenceReferenceBuilder builder = entityBuilder.HasOne(relationship.PrincipalEntity).WithOne();
                _ = builder.HasForeignKey(relationship.DependentEntity, [.. relationship.DependentProperties]);
                if (relationship.PrincipalProperties.Count > 0)
                {
                    _ = builder.HasPrincipalKey(relationship.PrincipalEntity, [.. relationship.PrincipalProperties]);
                }

                ApplyDeleteBehavior(builder, relationship.DeleteBehavior);
                continue;
            }

            ReferenceCollectionBuilder collectionBuilder = entityBuilder.HasOne(relationship.PrincipalEntity).WithMany();
            _ = collectionBuilder.HasForeignKey([.. relationship.DependentProperties]);
            if (relationship.PrincipalProperties.Count > 0)
            {
                _ = collectionBuilder.HasPrincipalKey([.. relationship.PrincipalProperties]);
            }

            ApplyDeleteBehavior(collectionBuilder, relationship.DeleteBehavior);
        }
    }

    private static void ApplyDeleteBehavior(ReferenceReferenceBuilder builder, EfDeleteBehavior deleteBehavior)
    {
        if (deleteBehavior != EfDeleteBehavior.Unspecified)
        {
            _ = builder.OnDelete(MapDeleteBehavior(deleteBehavior));
        }
    }

    private static void ApplyDeleteBehavior(ReferenceCollectionBuilder builder, EfDeleteBehavior deleteBehavior)
    {
        if (deleteBehavior != EfDeleteBehavior.Unspecified)
        {
            _ = builder.OnDelete(MapDeleteBehavior(deleteBehavior));
        }
    }

    private static void ApplyInheritance(EntityTypeBuilder entityBuilder, EfEntityTypeDefinition entity)
    {
        if (entity.Inheritance is not EfInheritanceDefinition inheritance)
        {
            return;
        }

        if (inheritance.Strategy == EfCoreInheritanceStrategy.Tph && !string.IsNullOrWhiteSpace(inheritance.DiscriminatorProperty))
        {
            DiscriminatorBuilder<string> discriminatorBuilder = entityBuilder.HasDiscriminator<string>(inheritance.DiscriminatorProperty);
            if (!string.IsNullOrWhiteSpace(inheritance.DiscriminatorValue))
            {
                _ = discriminatorBuilder.HasValue(inheritance.DiscriminatorValue);
            }
        }

        if (inheritance.Strategy == EfCoreInheritanceStrategy.Tpt && !string.IsNullOrWhiteSpace(entity.TableName))
        {
            _ = entityBuilder.UseTptMappingStrategy();
            _ = entityBuilder.ToTable(entity.TableName);
        }

        if (inheritance.Strategy == EfCoreInheritanceStrategy.Tpc && !string.IsNullOrWhiteSpace(entity.TableName))
        {
            _ = entityBuilder.UseTpcMappingStrategy();
            _ = entityBuilder.ToTable(entity.TableName);
        }
    }

    private static DeleteBehavior MapDeleteBehavior(EfDeleteBehavior deleteBehavior)
    {
        return deleteBehavior switch
        {
            EfDeleteBehavior.Restrict => DeleteBehavior.Restrict,
            EfDeleteBehavior.Cascade => DeleteBehavior.Cascade,
            EfDeleteBehavior.SetNull => DeleteBehavior.SetNull,
            EfDeleteBehavior.NoAction => DeleteBehavior.NoAction,
            EfDeleteBehavior.Unspecified => DeleteBehavior.ClientSetNull,
            _ => DeleteBehavior.ClientSetNull,
        };
    }
}
