using System.Text.Json;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Model.SchemaDiagnostic;

namespace SemanticTypeModel.JsonSchema.Domain;

/// <summary>
/// Package-owned JSON Schema domain semantic model derived from a canonical SemanticTypeModel model.
/// </summary>
public sealed record JsonSchemaSemanticModel
{
    /// <summary>Gets the JSON Schema dialect URI.</summary>
    public required string DialectUri { get; init; }

    /// <summary>Gets the optional document identifier.</summary>
    public Uri? Id { get; init; }

    /// <summary>Gets the root schema node.</summary>
    public required JsonSchemaNode Root { get; init; }

    /// <summary>Gets deterministic definition entries keyed by JSON Schema definition name.</summary>
    public required IReadOnlyDictionary<string, JsonSchemaNode> Definitions { get; init; }

    /// <summary>Gets diagnostics produced while deriving this domain semantic model.</summary>
    public IReadOnlyList<SchemaDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>Base type for JSON Schema domain schema nodes.</summary>
public abstract record JsonSchemaNode
{
    /// <summary>Gets the stable domain node name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the JSON Schema title annotation.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the JSON Schema description annotation.</summary>
    public string? Description { get; init; }

    /// <summary>Gets deterministic custom annotations to emit as extension keywords.</summary>
    public IReadOnlyDictionary<string, JsonElement> Annotations { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
}

/// <summary>Represents a JSON Schema object node.</summary>
public sealed record JsonSchemaObjectNode : JsonSchemaNode
{
    /// <summary>Gets deterministic properties for the object.</summary>
    public required IReadOnlyList<JsonSchemaProperty> Properties { get; init; }

    /// <summary>Gets a value indicating whether additional properties are permitted.</summary>
    public bool AdditionalPropertiesAllowed { get; init; } = true;
}

/// <summary>Represents a JSON Schema property.</summary>
public sealed record JsonSchemaProperty
{
    /// <summary>Gets the property name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the referenced or inline property schema.</summary>
    public required JsonSchemaSchemaRef Schema { get; init; }

    /// <summary>Gets a value indicating whether the property is required.</summary>
    public bool IsRequired { get; init; }

    /// <summary>Gets a value indicating whether the property accepts null.</summary>
    public bool IsNullable { get; init; }

    /// <summary>Gets the JSON Schema title annotation.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the JSON Schema description annotation.</summary>
    public string? Description { get; init; }

    /// <summary>Gets scalar, string, array, or object constraints attached to the property.</summary>
    public JsonSchemaConstraintSet Constraints { get; init; } = new();

    /// <summary>Gets deterministic custom annotations to emit as extension keywords.</summary>
    public IReadOnlyDictionary<string, JsonElement> Annotations { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
}

/// <summary>Represents a JSON Schema scalar node.</summary>
public sealed record JsonSchemaScalarNode : JsonSchemaNode
{
    /// <summary>Gets the JSON Schema scalar type name.</summary>
    public required string Type { get; init; }

    /// <summary>Gets a value indicating whether null is allowed.</summary>
    public bool IsNullable { get; init; }

    /// <summary>Gets the JSON Schema format.</summary>
    public string? Format { get; init; }

    /// <summary>Gets constraints attached to the scalar node.</summary>
    public JsonSchemaConstraintSet Constraints { get; init; } = new();
}

/// <summary>Represents a JSON Schema array node.</summary>
public sealed record JsonSchemaArrayNode : JsonSchemaNode
{
    /// <summary>Gets the item schema.</summary>
    public required JsonSchemaSchemaRef Items { get; init; }

    /// <summary>Gets constraints attached to the array node.</summary>
    public JsonSchemaConstraintSet Constraints { get; init; } = new();
}

/// <summary>Represents a JSON Schema dictionary/object-map node.</summary>
public sealed record JsonSchemaDictionaryNode : JsonSchemaNode
{
    /// <summary>Gets the value schema for additional properties.</summary>
    public required JsonSchemaSchemaRef Values { get; init; }
}

/// <summary>Represents a JSON Schema enum node.</summary>
public sealed record JsonSchemaEnumNode : JsonSchemaNode
{
    /// <summary>Gets deterministic enum values.</summary>
    public required IReadOnlyList<JsonElement> Values { get; init; }
}

/// <summary>Represents simple oneOf/anyOf composition in the JSON Schema domain model.</summary>
public sealed record JsonSchemaCompositionNode : JsonSchemaNode
{
    /// <summary>Gets the composition keyword.</summary>
    public required JsonSchemaCompositionKind Kind { get; init; }

    /// <summary>Gets deterministic composition alternatives.</summary>
    public required IReadOnlyList<JsonSchemaSchemaRef> Alternatives { get; init; }
}

/// <summary>Supported simple JSON Schema composition kinds.</summary>
public enum JsonSchemaCompositionKind
{
    /// <summary>Exclusive alternatives emitted as oneOf.</summary>
    OneOf,

    /// <summary>Non-exclusive alternatives emitted as anyOf.</summary>
    AnyOf,
}

/// <summary>Represents a JSON Schema reference or inline schema.</summary>
public sealed record JsonSchemaSchemaRef
{
    /// <summary>Gets the referenced definition name, or # for the root schema.</summary>
    public string? Reference { get; init; }

    /// <summary>Gets the inline schema node.</summary>
    public JsonSchemaNode? Inline { get; init; }

    /// <summary>Creates a definition reference.</summary>
    public static JsonSchemaSchemaRef FromReference(string reference)
    {
        return new() { Reference = reference };
    }

    /// <summary>Creates an inline schema reference.</summary>
    public static JsonSchemaSchemaRef FromInline(JsonSchemaNode inline)
    {
        return new() { Inline = inline };
    }
}

/// <summary>JSON Schema constraints represented explicitly by the domain model.</summary>
public sealed record JsonSchemaConstraintSet
{
    /// <summary>Gets the minimum string length.</summary>
    public int? MinLength { get; init; }

    /// <summary>Gets the maximum string length.</summary>
    public int? MaxLength { get; init; }

    /// <summary>Gets the string pattern.</summary>
    public string? Pattern { get; init; }

    /// <summary>Gets the inclusive numeric minimum.</summary>
    public decimal? Minimum { get; init; }

    /// <summary>Gets the inclusive numeric maximum.</summary>
    public decimal? Maximum { get; init; }

    /// <summary>Gets a value indicating whether the minimum is exclusive.</summary>
    public bool ExclusiveMinimum { get; init; }

    /// <summary>Gets a value indicating whether the maximum is exclusive.</summary>
    public bool ExclusiveMaximum { get; init; }

    /// <summary>Gets the numeric multiple constraint.</summary>
    public decimal? MultipleOf { get; init; }

    /// <summary>Gets the minimum array item count.</summary>
    public int? MinItems { get; init; }

    /// <summary>Gets the maximum array item count.</summary>
    public int? MaxItems { get; init; }

    /// <summary>Gets a value indicating whether array items must be unique.</summary>
    public bool UniqueItems { get; init; }

    /// <summary>Gets the minimum object property count.</summary>
    public int? MinProperties { get; init; }

    /// <summary>Gets the maximum object property count.</summary>
    public int? MaxProperties { get; init; }
}
