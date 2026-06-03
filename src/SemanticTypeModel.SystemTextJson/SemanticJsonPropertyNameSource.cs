namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Defines the source used when SemanticTypeModel customizes System.Text.Json property names.
/// </summary>
public enum SemanticJsonPropertyNameSource
{
    /// <summary>
    /// Preserve the property name supplied by the existing System.Text.Json contract.
    /// </summary>
    ExistingJsonContract,

    /// <summary>
    /// Use the imported System.Text.Json property-name annotation when present.
    /// </summary>
    SystemTextJsonPropertyNameAnnotation,

    /// <summary>
    /// Use the semantic property name from the canonical semantic type model.
    /// </summary>
    SemanticPropertyName,
}
