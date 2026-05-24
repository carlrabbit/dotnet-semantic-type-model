using System.Diagnostics.CodeAnalysis;

namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Identifies the primitive kind of a scalar type shape.
/// </summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "These names intentionally mirror JSON Schema primitive type names.")]
public enum ScalarKind
{
    /// <summary>A string scalar.</summary>
    String,
    /// <summary>An integer scalar.</summary>
    Integer,
    /// <summary>A floating-point numeric scalar.</summary>
    Number,
    /// <summary>A boolean scalar.</summary>
    Boolean,
    /// <summary>An explicit null scalar.</summary>
    Null,
}
