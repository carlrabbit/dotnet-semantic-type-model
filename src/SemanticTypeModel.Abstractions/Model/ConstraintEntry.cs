namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// A single named constraint value within a <see cref="ConstraintSet"/>.
/// Constraints express validation rules such as minimum, maximum, pattern, and minLength.
/// </summary>
public sealed record ConstraintEntry(string Key, string Value);
