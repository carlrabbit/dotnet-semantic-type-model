namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Defines annotation keys used to preserve System.Text.Json contract metadata on semantic shapes.
/// </summary>
public static class SystemTextJsonAnnotationNames
{
    /// <summary>JsonPropertyNameAttribute value.</summary>
    public const string PropertyName = "systemTextJson.propertyName";
    /// <summary>JsonIgnoreAttribute marker.</summary>
    public const string Ignore = "systemTextJson.ignore";
    /// <summary>JsonIgnoreCondition value.</summary>
    public const string IgnoreCondition = "systemTextJson.ignoreCondition";
    /// <summary>JsonIncludeAttribute marker.</summary>
    public const string Include = "systemTextJson.include";
    /// <summary>JsonConverterAttribute converter type.</summary>
    public const string Converter = "systemTextJson.converter";
    /// <summary>JsonNumberHandlingAttribute value.</summary>
    public const string NumberHandling = "systemTextJson.numberHandling";
    /// <summary>JsonRequiredAttribute marker.</summary>
    public const string Required = "systemTextJson.required";
    /// <summary>JsonExtensionDataAttribute marker.</summary>
    public const string ExtensionData = "systemTextJson.extensionData";
    /// <summary>JsonObjectCreationHandlingAttribute value.</summary>
    public const string ObjectCreationHandling = "systemTextJson.objectCreationHandling";
    /// <summary>JsonUnmappedMemberHandlingAttribute value.</summary>
    public const string UnmappedMemberHandling = "systemTextJson.unmappedMemberHandling";
    /// <summary>JsonPolymorphic/JsonDerivedType marker.</summary>
    public const string Polymorphism = "systemTextJson.polymorphism";
}
