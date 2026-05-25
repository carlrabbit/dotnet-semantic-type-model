namespace SemanticTypeModel.DotNet;

/// <summary>
/// Marks a CLR type as an explicit semantic-model root for compile-time extraction.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class SemanticTypeAttribute : Attribute;

/// <summary>
/// Excludes a type or member from semantic-model extraction.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class SemanticIgnoreAttribute : Attribute;

/// <summary>
/// Overrides semantic display/name metadata for a type or member.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
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
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
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
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
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
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class SemanticKeyAttribute : Attribute;

/// <summary>
/// Marks a member as participating in relationship semantics.
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class SemanticRelationshipAttribute : Attribute;

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
}
