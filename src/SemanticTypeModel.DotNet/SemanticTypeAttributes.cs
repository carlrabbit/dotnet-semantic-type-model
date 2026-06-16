namespace SemanticTypeModel.DotNet;

/// <summary>
/// Marks a CLR type as an explicit semantic-model root for compile-time extraction.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class SemanticTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new semantic type attribute.
    /// </summary>
    public SemanticTypeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new semantic type attribute with an explicit role.
    /// </summary>
    /// <param name="role">The declared semantic role.</param>
    public SemanticTypeAttribute(SemanticTypeRole role)
    {
        Role = role.ToString();
    }

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
public sealed class SemanticRoleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new semantic role attribute.
    /// </summary>
    /// <param name="role">The declared semantic role.</param>
    public SemanticRoleAttribute(string role)
    {
        Role = role;
    }

    /// <summary>
    /// Initializes a new semantic role attribute.
    /// </summary>
    /// <param name="role">The declared semantic role.</param>
    public SemanticRoleAttribute(SemanticTypeRole role)
        : this(role.ToString())
    {
    }

    /// <summary>
    /// Gets the declared semantic role.
    /// </summary>
    public string Role { get; }
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
    /// Gets or sets a value indicating whether System.Text.Json attributes are imported as namespaced annotations.
    /// </summary>
    public bool ImportSystemTextJsonAttributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether JsonPropertyName also replaces semantic property names.
    /// </summary>
    public bool UseJsonPropertyNameAsSemanticName { get; set; }

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
/// Represents semantic type roles.
/// </summary>
public enum SemanticTypeRole
{
    /// <summary>
    /// Unspecified role semantics.
    /// </summary>
    Unspecified,

    /// <summary>
    /// Entity role semantics.
    /// </summary>
    Entity,

    /// <summary>
    /// Value-object role semantics.
    /// </summary>
    ValueObject,

    /// <summary>
    /// Dimension role semantics.
    /// </summary>
    Dimension,

    /// <summary>
    /// Fact role semantics.
    /// </summary>
    Fact,

    /// <summary>
    /// Lookup role semantics.
    /// </summary>
    Lookup,

    /// <summary>
    /// Event role semantics.
    /// </summary>
    Event,

    /// <summary>
    /// Configuration role semantics.
    /// </summary>
    Configuration,

    /// <summary>
    /// Form role semantics.
    /// </summary>
    Form,
}

/// <summary>
/// Declares user-facing display metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticDisplayNameAttribute(string displayName) : Attribute
{
    /// <summary>
    /// Gets the declared display name.
    /// </summary>
    public string DisplayName { get; } = displayName;
}

/// <summary>
/// Declares semantic category metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticCategoryAttribute(string category) : Attribute
{
    /// <summary>
    /// Gets the declared category.
    /// </summary>
    public string Category { get; } = category;
}

/// <summary>
/// Declares semantic display ordering metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticOrderAttribute(int order) : Attribute
{
    /// <summary>
    /// Gets the declared order value.
    /// </summary>
    public int Order { get; } = order;
}

/// <summary>
/// Represents common semantic scalar format names.
/// </summary>
public enum SemanticScalarFormat
{
    /// <summary>Email format.</summary>
    Email,
    /// <summary>URI format.</summary>
    Uri,
    /// <summary>Host name format.</summary>
    Hostname,
    /// <summary>IPv4 address format.</summary>
    Ipv4,
    /// <summary>IPv6 address format.</summary>
    Ipv6,
    /// <summary>Date format.</summary>
    Date,
    /// <summary>Time format.</summary>
    Time,
    /// <summary>Date-time format.</summary>
    DateTime,
    /// <summary>Duration format.</summary>
    Duration,
    /// <summary>UUID format.</summary>
    Uuid,
}

/// <summary>
/// Declares scalar format metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticFormatAttribute : Attribute
{
    /// <summary>
    /// Initializes a new semantic format attribute.
    /// </summary>
    /// <param name="format">The declared format.</param>
    public SemanticFormatAttribute(string format)
    {
        Format = format;
    }

    /// <summary>
    /// Initializes a new semantic format attribute.
    /// </summary>
    /// <param name="format">The declared format.</param>
    public SemanticFormatAttribute(SemanticScalarFormat format)
        : this(format switch
        {
            SemanticScalarFormat.Email => "email",
            SemanticScalarFormat.Uri => "uri",
            SemanticScalarFormat.Hostname => "hostname",
            SemanticScalarFormat.Ipv4 => "ipv4",
            SemanticScalarFormat.Ipv6 => "ipv6",
            SemanticScalarFormat.Date => "date",
            SemanticScalarFormat.Time => "time",
            SemanticScalarFormat.DateTime => "date-time",
            SemanticScalarFormat.Duration => "duration",
            SemanticScalarFormat.Uuid => "uuid",
            _ => format.ToString(),
        })
    {
    }

    /// <summary>
    /// Gets the declared format.
    /// </summary>
    public string Format { get; }
}

/// <summary>
/// Declares string constraints for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticStringConstraintsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the minimum string length.
    /// </summary>
    public int MinLength { get; init; } = -1;

    /// <summary>
    /// Gets or sets the maximum string length.
    /// </summary>
    public int MaxLength { get; init; } = -1;

    /// <summary>
    /// Gets or sets the regular expression pattern.
    /// </summary>
    public string? Pattern { get; init; }
}

/// <summary>
/// Declares numeric constraints for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticNumericConstraintsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the inclusive minimum value.
    /// </summary>
    public double Minimum { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets the inclusive maximum value.
    /// </summary>
    public double Maximum { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets the exclusive minimum value.
    /// </summary>
    public double ExclusiveMinimum { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets the exclusive maximum value.
    /// </summary>
    public double ExclusiveMaximum { get; init; } = double.NaN;

    /// <summary>
    /// Gets or sets the multiple-of value.
    /// </summary>
    public double MultipleOf { get; init; } = double.NaN;
}

/// <summary>
/// Declares collection constraints for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticCollectionConstraintsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the minimum item count.
    /// </summary>
    public int MinItems { get; init; } = -1;

    /// <summary>
    /// Gets or sets the maximum item count.
    /// </summary>
    public int MaxItems { get; init; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether collection items must be unique.
    /// </summary>
    public bool UniqueItems { get; init; }
}

/// <summary>
/// Declares enum value metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SemanticEnumValueAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the user-facing display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or sets the semantic description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Declares a custom semantic annotation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public sealed class SemanticAnnotationAttribute(string key, string value) : Attribute
{
    /// <summary>
    /// Gets the annotation key.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    /// Gets the annotation value.
    /// </summary>
    public string Value { get; } = value;
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

/// <summary>
/// Marks a CLR type as a projection-neutral envelope wrapper around a semantic payload.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticEnvelopeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new envelope attribute.
    /// </summary>
    public SemanticEnvelopeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new envelope attribute with a projection-neutral purpose.
    /// </summary>
    /// <param name="purpose">The envelope purpose.</param>
    public SemanticEnvelopeAttribute(string purpose)
    {
        Purpose = purpose;
    }

    /// <summary>
    /// Gets the optional projection-neutral purpose for the envelope.
    /// </summary>
    public string? Purpose { get; init; }
}

/// <summary>
/// Marks the member that carries an envelope payload.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticEnvelopePayloadAttribute : Attribute;

/// <summary>
/// Marks a member as envelope lifecycle/context metadata rather than payload domain state.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticEnvelopeMetadataAttribute : Attribute;

/// <summary>
/// Marks a CLR type as participating in version or revision evolution.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticVersionedAttribute : Attribute;

/// <summary>
/// Marks a member as an owned object or owned collection whose lifecycle follows the declaring owner.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticOwnedAttribute : Attribute
{
    /// <summary>Gets or sets the ownership kind. Defaults to inference from the property shape.</summary>
    public SemanticOwnershipKind Kind { get; init; } = SemanticOwnershipKind.Inferred;
}

/// <summary>
/// Ownership kind declared for a semantic owned member.
/// </summary>
public enum SemanticOwnershipKind
{
    /// <summary>Infer owned object or owned collection from the property type.</summary>
    Inferred,

    /// <summary>The property is a single owned object.</summary>
    Object,

    /// <summary>The property is a collection of owned elements.</summary>
    Collection,
}

/// <summary>
/// Marks a member as a version identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticVersionAttribute : Attribute;

/// <summary>
/// Marks a member as a revision identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticRevisionAttribute : Attribute;

/// <summary>
/// Marks a member as the current version or revision indicator.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticCurrentVersionAttribute : Attribute;

/// <summary>
/// Marks a CLR type as having a temporal validity interval.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticTemporalValidityAttribute : Attribute;

/// <summary>
/// Marks a member as the start of a temporal validity interval.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticValidFromAttribute : Attribute;

/// <summary>
/// Marks a member as the optional end of a temporal validity interval.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticValidToAttribute : Attribute;

/// <summary>
/// Marks a member as lifecycle state.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticLifecycleStateAttribute : Attribute;

/// <summary>
/// Marks a dictionary-like member as instance extension data.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticExtensionDataAttribute : Attribute;

/// <summary>
/// Declares a configuration section name for an options type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticConfigurationSectionAttribute(string sectionName) : Attribute
{
    /// <summary>Gets the configuration section name.</summary>
    public string SectionName { get; } = sectionName;
}

/// <summary>
/// Enables data-annotations validation in the Configuration projection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticValidateDataAnnotationsAttribute : Attribute;

/// <summary>
/// Enables ValidateOnStart in the Configuration projection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticValidateOnStartAttribute : Attribute;

/// <summary>
/// Requests a generated options registration helper for the Configuration projection.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class SemanticGenerateOptionsRegistrationAttribute : Attribute
{
    /// <summary>Gets or sets the generated extension method name.</summary>
    public string? ExtensionMethodName { get; init; }
}

/// <summary>
/// Declares that the attributed property is required when another property equals a literal value.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class SemanticRequiredWhenAttribute(string sourceProperty, string value) : Attribute
{
    /// <summary>Gets the source property name.</summary>
    public string SourceProperty { get; } = sourceProperty;

    /// <summary>Gets the comparison literal.</summary>
    public string Value { get; } = value;

    /// <summary>Gets or sets an optional validation message.</summary>
    public string? Message { get; init; }
}
