namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// A key-value annotation attached to a type shape or property.
/// Annotations carry descriptive metadata such as title, description, and examples.
/// </summary>
public sealed record SchemaAnnotation(string Key, string Value);
