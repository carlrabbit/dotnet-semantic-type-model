// CA1720 is disabled because EF projection enums intentionally use canonical type words such as String,
// Numeric, and External to align with the published specification and metadata surface.
#pragma warning disable CA1720
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Represents the EF Core-like projection result for a canonical semantic model.
/// </summary>
public sealed record EfCoreSemanticModel
{
    /// <summary>Gets the stable identifier of the canonical model from which this model was derived.</summary>
    public string? SourceModelId { get; init; }

    /// <summary>Gets the closed application policy carried by this model.</summary>
    public EfCoreApplicationMode ApplicationPolicy { get; init; } = EfCoreApplicationMode.ClosedClrModel;

    /// <summary>Gets source CLR and semantic lineage used by closed application.</summary>
    public IReadOnlyList<EfCoreSourceTypeMapping> SourceTypes { get; init; } = [];
    /// <summary>Gets the projected model name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets projected entity types, including owned types when configured.</summary>
    public required IReadOnlyList<EfEntityTypeDefinition> EntityTypes { get; init; }

    /// <summary>Gets projection diagnostics.</summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }

    /// <summary>Creates an EF Core domain semantic model from the legacy EF model definition.</summary>
    public static EfCoreSemanticModel FromDefinition(EfModelDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new EfCoreSemanticModel
        {
            Name = definition.Name,
            EntityTypes = definition.EntityTypes,
            Diagnostics = definition.Diagnostics,
        };
    }

    internal static EfCoreSemanticModel FromDefinition(EfModelDefinition definition, TypeSchemaModel source)
    {
        EfCoreSemanticModel model = FromDefinition(definition);
        return model with
        {
            SourceModelId = source.Id.Value,
            SourceTypes = EfCoreSourceLineage.Create(source),
        };
    }

    /// <summary>Creates a legacy EF model definition view of this domain semantic model.</summary>
    public EfModelDefinition ToDefinition()
    {
        return new EfModelDefinition
        {
            Name = Name,
            EntityTypes = EntityTypes,
            Diagnostics = Diagnostics,
        };
    }
}

/// <summary>Describes the source identity and semantic classification of a CLR type.</summary>
public sealed record EfCoreSourceTypeMapping
{
    /// <summary>Gets the source semantic type identifier.</summary>
    public required string SourceSemanticTypeId { get; init; }
    /// <summary>Gets the assembly-qualified source CLR type name.</summary>
    public required string SourceClrTypeName { get; init; }
    /// <summary>Gets the semantic role.</summary>
    public required EntityRole SemanticRole { get; init; }
    /// <summary>Gets whether the type is an independent EF root.</summary>
    public bool IsRootEntity { get; init; }
    /// <summary>Gets whether the type is a semantic value object.</summary>
    public bool IsValueObject { get; init; }
    /// <summary>Gets whether the type is reached through semantic ownership.</summary>
    public bool IsOwned { get; init; }
    /// <summary>Gets source member lineage.</summary>
    public IReadOnlyList<EfCoreSourcePropertyMapping> Properties { get; init; } = [];
    /// <summary>Gets members that closed application must suppress.</summary>
    public IReadOnlyList<EfCoreSuppressedMember> SuppressedMembers { get; init; } = [];
    /// <summary>Gets ownership edges declared by the source type.</summary>
    public IReadOnlyList<EfCoreOwnedMapping> OwnedMappings { get; init; } = [];
}

/// <summary>Describes source identity and storage policy for a semantic property.</summary>
public sealed record EfCoreSourcePropertyMapping
{
    /// <summary>Gets the source property identifier.</summary>
    public required string SourcePropertyId { get; init; }
    /// <summary>Gets the source CLR member name.</summary>
    public required string SourceMemberName { get; init; }
    /// <summary>Gets the CLR type that declares the member when known.</summary>
    public required string SourceDeclaringClrTypeName { get; init; }
    /// <summary>Gets the deterministic storage classification.</summary>
    public required EfCoreStorageKind StorageKind { get; init; }
    /// <summary>Gets the semantic-only classification, when the member is not persisted by EF.</summary>
    public EfCoreSemanticOnlyKind SemanticOnlyKind { get; init; }
}

/// <summary>Describes a source ownership edge.</summary>
public sealed record EfCoreOwnedMapping
{
    /// <summary>Gets the owner semantic type identifier.</summary>
    public required string OwnerSourceTypeId { get; init; }
    /// <summary>Gets the owner CLR type name.</summary>
    public required string OwnerClrTypeName { get; init; }
    /// <summary>Gets the CLR navigation name.</summary>
    public required string NavigationName { get; init; }
    /// <summary>Gets the target semantic type identifier.</summary>
    public required string TargetSourceTypeId { get; init; }
    /// <summary>Gets the target CLR type name.</summary>
    public required string TargetClrTypeName { get; init; }
    /// <summary>Gets the target semantic role.</summary>
    public required EntityRole TargetSemanticRole { get; init; }
    /// <summary>Gets the selected storage policy.</summary>
    public required EfCoreStorageKind StorageKind { get; init; }
}

/// <summary>Describes a CLR member excluded from the closed EF model.</summary>
public sealed record EfCoreSuppressedMember
{
    /// <summary>Gets the source CLR member name.</summary>
    public required string SourceMemberName { get; init; }
    /// <summary>Gets the CLR type that declares the member.</summary>
    public required string SourceDeclaringClrTypeName { get; init; }
    /// <summary>Gets the suppression reason.</summary>
    public required string Reason { get; init; }
    /// <summary>Gets the semantic-only classification.</summary>
    public required EfCoreSemanticOnlyKind SemanticOnlyKind { get; init; }
}

/// <summary>Identifies provider-neutral EF storage intent.</summary>
public enum EfCoreStorageKind
{
    /// <summary>A directly persisted scalar.</summary>
    Scalar,
    /// <summary>An EF owned navigation.</summary>
    OwnedNavigation,
    /// <summary>Properties flattened into owner columns.</summary>
    Flattened,
    /// <summary>A provider-neutral JSON value.</summary>
    Json,
    /// <summary>A member excluded from EF storage.</summary>
    Suppressed,
}

/// <summary>Identifies members that exist in the semantic model but not in EF storage.</summary>
public enum EfCoreSemanticOnlyKind
{
    /// <summary>The member participates in EF storage.</summary>
    None,
    /// <summary>The member carries semantic extension data.</summary>
    ExtensionData,
}

/// <summary>
/// Represents the EF Core-like projection result for a canonical semantic model.
/// </summary>
public sealed record EfModelDefinition
{
    /// <summary>
    /// Gets the projected model name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets projected entity types, including owned types when configured.
    /// </summary>
    public required IReadOnlyList<EfEntityTypeDefinition> EntityTypes { get; init; }

    /// <summary>
    /// Gets projection diagnostics.
    /// </summary>
    public required IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; }
}

/// <summary>
/// Represents a projected EF Core-like entity type definition.
/// </summary>
public sealed record EfEntityTypeDefinition
{
    /// <summary>
    /// Gets the projected entity type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the projected table name when applicable.
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Gets the projected schema name when applicable.
    /// </summary>
    public string? SchemaName { get; init; }

    /// <summary>Gets the provider-neutral table comment projected from the technical description.</summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Gets projected scalar, enum, flattened, serialized, or owned-navigation properties.
    /// </summary>
    public required IReadOnlyList<EfPropertyDefinition> Properties { get; init; }

    /// <summary>
    /// Gets projected key definitions.
    /// </summary>
    public required IReadOnlyList<EfKeyDefinition> Keys { get; init; }

    /// <summary>
    /// Gets projected relationship definitions declared for this entity.
    /// </summary>
    public required IReadOnlyList<EfRelationshipDefinition> Relationships { get; init; }

    /// <summary>Gets projected index definitions.</summary>
    public IReadOnlyList<EfIndexDefinition> Indexes { get; init; } = [];

    /// <summary>Gets explicit inheritance mapping metadata when configured.</summary>
    public EfInheritanceDefinition? Inheritance { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entity definition represents an owned/value object projection.
    /// </summary>
    public bool IsOwned { get; init; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected EF Core-like property definition.
/// </summary>
public sealed record EfPropertyDefinition
{
    /// <summary>
    /// Gets the projected property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the CLR type selected for the projection.
    /// </summary>
    public required Type ClrType { get; init; }

    /// <summary>
    /// Gets a value indicating whether property presence is required by canonical semantics.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets a value indicating whether the projected value may be null.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets the maximum string length when available.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets numeric precision metadata when available.
    /// </summary>
    public NumericPrecision? Precision { get; init; }

    /// <summary>
    /// Gets the projected column name when applicable.
    /// </summary>
    public string? ColumnName { get; init; }

    /// <summary>Gets the provider-neutral column comment projected from the technical description.</summary>
    public string? Comment { get; init; }

    /// <summary>
    /// Gets optional conversion metadata.
    /// </summary>
    public string? Conversion { get; init; }

    /// <summary>Gets an explicit EF Core value converter type when configured.</summary>
    public Type? ConverterType { get; init; }

    /// <summary>Gets an explicit provider CLR type when configured.</summary>
    public Type? ProviderClrType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the value is generated by the store or adapter contract.
    /// </summary>
    public bool IsGenerated { get; init; }

    /// <summary>
    /// Gets carried annotations, including preserved schema presence/nullability markers.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}

/// <summary>
/// Represents a projected EF Core-like key definition.
/// </summary>
public sealed record EfKeyDefinition
{
    /// <summary>
    /// Gets the projected key name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets projected property names participating in the key.
    /// </summary>
    public required IReadOnlyList<string> PropertyNames { get; init; }

    /// <summary>
    /// Gets the projected key kind.
    /// </summary>
    public required EfKeyKind Kind { get; init; }

    /// <summary>
    /// Gets a value indicating whether the key is generated.
    /// </summary>
    public bool IsGenerated { get; init; }
}

/// <summary>
/// Represents a projected EF Core-like relationship definition.
/// </summary>
public sealed record EfRelationshipDefinition
{
    /// <summary>
    /// Gets the relationship name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the principal entity name.
    /// </summary>
    public required string PrincipalEntity { get; init; }

    /// <summary>
    /// Gets the dependent entity name.
    /// </summary>
    public required string DependentEntity { get; init; }

    /// <summary>
    /// Gets projected principal-side property names.
    /// </summary>
    public required IReadOnlyList<string> PrincipalProperties { get; init; }

    /// <summary>
    /// Gets projected dependent-side property names.
    /// </summary>
    public required IReadOnlyList<string> DependentProperties { get; init; }

    /// <summary>
    /// Gets the projected relationship cardinality.
    /// </summary>
    public required EfRelationshipCardinality Cardinality { get; init; }

    /// <summary>
    /// Gets the projected delete behavior.
    /// </summary>
    public EfDeleteBehavior DeleteBehavior { get; init; }

    /// <summary>
    /// Gets carried annotations.
    /// </summary>
    public required AnnotationBag Annotations { get; init; }
}


/// <summary>
/// Represents a projected EF Core index definition.
/// </summary>
public sealed record EfIndexDefinition
{
    /// <summary>Gets the projected index name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets projected property names participating in the index.</summary>
    public required IReadOnlyList<string> PropertyNames { get; init; }

    /// <summary>Gets a value indicating whether the index is unique.</summary>
    public bool IsUnique { get; init; }
}

/// <summary>
/// Represents explicit EF Core inheritance mapping metadata.
/// </summary>
public sealed record EfInheritanceDefinition
{
    /// <summary>Gets the selected inheritance strategy.</summary>
    public required EfCoreInheritanceStrategy Strategy { get; init; }

    /// <summary>Gets the base entity name when this entity derives from another entity.</summary>
    public string? BaseEntity { get; init; }

    /// <summary>Gets the discriminator property for TPH mapping when configured.</summary>
    public string? DiscriminatorProperty { get; init; }

    /// <summary>Gets the discriminator value for TPH mapping when configured.</summary>
    public string? DiscriminatorValue { get; init; }
}

/// <summary>
/// Defines EF Core-like key kinds.
/// </summary>
public enum EfKeyKind
{
    /// <summary>Primary key.</summary>
    Primary,

    /// <summary>Alternate key.</summary>
    Alternate,

    /// <summary>Unique index representation.</summary>
    UniqueIndex,

    /// <summary>Surrogate key.</summary>
    Surrogate,

    /// <summary>External identity marker.</summary>
    External,
}

/// <summary>
/// Defines EF Core-like relationship cardinality.
/// </summary>
public enum EfRelationshipCardinality
{
    /// <summary>One-to-one.</summary>
    OneToOne,

    /// <summary>One-to-many.</summary>
    OneToMany,

    /// <summary>Many-to-one.</summary>
    ManyToOne,

    /// <summary>Many-to-many.</summary>
    ManyToMany,
}

/// <summary>
/// Defines EF Core-like delete behavior.
/// </summary>
public enum EfCoreInheritanceStrategy
{
    /// <summary>No explicit inheritance strategy.</summary>
    Unspecified,

    /// <summary>Table-per-hierarchy.</summary>
    Tph,

    /// <summary>Table-per-type.</summary>
    Tpt,

    /// <summary>Table-per-concrete-type.</summary>
    Tpc,
}

/// <summary>
/// Defines EF Core-like delete behavior.
/// </summary>
public enum EfDeleteBehavior
{
    /// <summary>No explicit delete behavior.</summary>
    Unspecified,

    /// <summary>Restrict delete behavior.</summary>
    Restrict,

    /// <summary>Cascade delete behavior.</summary>
    Cascade,

    /// <summary>Set null delete behavior.</summary>
    SetNull,

    /// <summary>No action delete behavior.</summary>
    NoAction,
}
#pragma warning restore CA1720
