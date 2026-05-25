namespace SemanticTypeModel.DotNet;

/// <summary>
/// Marks a CLR type as an explicit semantic-model root for compile-time extraction.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class SemanticTypeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the canonical semantic name for the attributed type.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the canonical semantic role for the attributed type.
    /// </summary>
    public string? Role { get; init; }
}

/// <summary>
/// Excludes a type or member from semantic-model extraction.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticIgnoreAttribute : Attribute;

/// <summary>
/// Overrides semantic display/name metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the declared semantic name.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Declares semantic description metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticDescriptionAttribute(string description) : Attribute
{
    /// <summary>
    /// Gets the declared semantic description.
    /// </summary>
    public string Description { get; } = description;
}

/// <summary>
/// Declares semantic role metadata for a type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class SemanticRoleAttribute(string role) : Attribute
{
    /// <summary>
    /// Gets the declared semantic role.
    /// </summary>
    public string Role { get; } = role;
}

/// <summary>
/// Marks a member as participating in a key definition.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class SemanticKeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the key group name for composite keys.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the semantic key kind.
    /// </summary>
    public KeyKind Kind { get; init; } = KeyKind.Primary;

    /// <summary>
    /// Gets or sets the key member order for composite keys.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the key is generated.
    /// </summary>
    public bool IsGenerated { get; init; }
}

/// <summary>
/// Marks a member as participating in relationship semantics.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class SemanticRelationshipAttribute(string? principalTypeName = null) : Attribute
{
    /// <summary>
    /// Gets the principal type metadata name when explicitly provided.
    /// </summary>
    public string? PrincipalTypeName { get; } = principalTypeName;

    /// <summary>
    /// Gets or sets the principal key name.
    /// </summary>
    public string? PrincipalKey { get; init; }

    /// <summary>
    /// Gets or sets the foreign key property name.
    /// </summary>
    public string? ForeignKey { get; init; }

    /// <summary>
    /// Gets or sets the relationship cardinality.
    /// </summary>
    public RelationshipCardinality Cardinality { get; init; } = RelationshipCardinality.Unknown;
}

/// <summary>
/// Declares generator-wide compile-time options.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class SemanticTypeModelGeneratorOptionsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new options attribute instance.
    /// </summary>
    /// <param name="generatedNamespace">The generated provider namespace.</param>
    /// <param name="providerName">The generated provider type name.</param>
    public SemanticTypeModelGeneratorOptionsAttribute(
        string? generatedNamespace = null,
        string? providerName = null)
    {
        GeneratedNamespace = generatedNamespace;
        ProviderName = providerName;
    }

    /// <summary>
    /// Gets the generated provider namespace.
    /// </summary>
    public string? GeneratedNamespace { get; }

    /// <summary>
    /// Gets the generated provider type name.
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether internal types are included in discovery.
    /// </summary>
    public bool IncludeInternalTypes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether XML documentation extraction is required.
    /// </summary>
    public bool RequireXmlDocumentation { get; set; }

    /// <summary>
    /// Gets or sets the type discovery mode.
    /// </summary>
    public DotNetTypeDiscoveryMode DiscoveryMode { get; set; } = DotNetTypeDiscoveryMode.ExplicitAttributes;

    /// <summary>
    /// Gets or sets included namespace prefixes for namespace discovery mode.
    /// </summary>
    public string? IncludedNamespaces { get; set; }

    /// <summary>
    /// Gets or sets excluded namespace prefixes for namespace discovery mode.
    /// </summary>
    public string? ExcludedNamespaces { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether internal members are included.
    /// </summary>
    public bool IncludeInternalMembers { get; set; }

    /// <summary>
    /// Gets or sets the naming policy.
    /// </summary>
    public DotNetNamingPolicy NamingPolicy { get; set; } = DotNetNamingPolicy.Preserve;

    /// <summary>
    /// Gets or sets a value indicating whether key inference is enabled.
    /// </summary>
    public bool InferKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether relationship inference is enabled.
    /// </summary>
    public bool InferRelationships { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether XML documentation descriptions are included.
    /// </summary>
    public bool IncludeXmlDocumentation { get; set; }
}

/// <summary>
/// Represents semantic key kinds.
/// </summary>
public enum KeyKind
{
    /// <summary>
    /// Primary key semantics.
    /// </summary>
    Primary,

    /// <summary>
    /// Alternate key semantics.
    /// </summary>
    Alternate,

    /// <summary>
    /// Natural key semantics.
    /// </summary>
    Natural,
}

/// <summary>
/// Represents relationship cardinality semantics.
/// </summary>
public enum RelationshipCardinality
{
    /// <summary>
    /// Unspecified cardinality.
    /// </summary>
    Unknown,

    /// <summary>
    /// One-to-one relationship.
    /// </summary>
    OneToOne,

    /// <summary>
    /// One-to-many relationship.
    /// </summary>
    OneToMany,

    /// <summary>
    /// Many-to-one relationship.
    /// </summary>
    ManyToOne,

    /// <summary>
    /// Many-to-many relationship.
    /// </summary>
    ManyToMany,
}
