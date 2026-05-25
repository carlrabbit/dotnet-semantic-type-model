using Microsoft.CodeAnalysis;

namespace SemanticTypeModel.DotNet;

/// <summary>
/// Defines configuration for Roslyn-based .NET type extraction.
/// </summary>
public sealed record DotNetExtractionOptions
{
    /// <summary>
    /// Gets the default extraction options.
    /// </summary>
    public static DotNetExtractionOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether internal types are included.
    /// </summary>
    public bool IncludeInternalTypes { get; init; }

    /// <summary>
    /// Gets a value indicating whether XML documentation is required.
    /// </summary>
    public bool RequireXmlDocumentation { get; init; }

    /// <summary>
    /// Gets the generated provider namespace.
    /// </summary>
    public string GeneratedNamespace { get; init; } = "SemanticTypeModel.Generated";

    /// <summary>
    /// Gets the generated provider type name.
    /// </summary>
    public string ProviderName { get; init; } = "AppSemanticTypeModel";
}

/// <summary>
/// Represents a deterministic extraction diagnostic.
/// </summary>
/// <param name="Code">The stable diagnostic code.</param>
/// <param name="Message">The diagnostic message.</param>
/// <param name="Location">The source location when available.</param>
public sealed record DotNetExtractionDiagnostic(string Code, string Message, Location? Location);

/// <summary>
/// Describes the extracted model graph for code generation.
/// </summary>
public sealed class DotNetExtractionResult
{
    /// <summary>
    /// Gets the extracted type descriptors keyed by deterministic type id.
    /// </summary>
    public required IReadOnlyDictionary<string, DotNetTypeDescriptor> TypesById { get; init; }

    /// <summary>
    /// Gets all extraction diagnostics.
    /// </summary>
    public required IReadOnlyList<DotNetExtractionDiagnostic> Diagnostics { get; init; }

    /// <summary>
    /// Gets the default root type id, when one was discovered.
    /// </summary>
    public string? RootTypeId { get; init; }

    /// <summary>
    /// Gets the effective extraction options used for this result.
    /// </summary>
    public required DotNetExtractionOptions Options { get; init; }
}

/// <summary>
/// Represents a generic extracted .NET type descriptor.
/// </summary>
public abstract record DotNetTypeDescriptor
{
    /// <summary>
    /// Gets the deterministic type identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display-oriented type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets type-level annotations.
    /// </summary>
    public IReadOnlyDictionary<string, string> Annotations { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>
/// Represents an extracted object descriptor.
/// </summary>
public sealed record DotNetObjectTypeDescriptor : DotNetTypeDescriptor
{
    /// <summary>
    /// Gets object properties.
    /// </summary>
    public required IReadOnlyList<DotNetPropertyDescriptor> Properties { get; init; }
}

/// <summary>
/// Represents an extracted scalar descriptor.
/// </summary>
public sealed record DotNetScalarTypeDescriptor : DotNetTypeDescriptor
{
    /// <summary>
    /// Gets the logical scalar kind.
    /// </summary>
    public required DotNetScalarKind ScalarKind { get; init; }
}

/// <summary>
/// Represents an extracted enum descriptor.
/// </summary>
public sealed record DotNetEnumTypeDescriptor : DotNetTypeDescriptor
{
    /// <summary>
    /// Gets enum values and numeric payloads.
    /// </summary>
    public required IReadOnlyList<DotNetEnumValueDescriptor> Values { get; init; }
}

/// <summary>
/// Represents an extracted array/collection descriptor.
/// </summary>
public sealed record DotNetArrayTypeDescriptor : DotNetTypeDescriptor
{
    /// <summary>
    /// Gets the item type id.
    /// </summary>
    public required string ItemTypeId { get; init; }
}

/// <summary>
/// Represents an extracted dictionary descriptor.
/// </summary>
public sealed record DotNetDictionaryTypeDescriptor : DotNetTypeDescriptor
{
    /// <summary>
    /// Gets the dictionary value type id.
    /// </summary>
    public required string ValueTypeId { get; init; }
}

/// <summary>
/// Represents an extracted object property descriptor.
/// </summary>
public sealed record DotNetPropertyDescriptor
{
    /// <summary>
    /// Gets property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets referenced type id.
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the property is required.
    /// </summary>
    public required bool IsRequired { get; init; }

    /// <summary>
    /// Gets a value indicating whether the property allows null.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Gets member annotations.
    /// </summary>
    public IReadOnlyDictionary<string, string> Annotations { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>
/// Represents an extracted enum value descriptor.
/// </summary>
public sealed record DotNetEnumValueDescriptor
{
    /// <summary>
    /// Gets enum member name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets enum numeric value.
    /// </summary>
    public required long NumericValue { get; init; }
}

/// <summary>
/// Identifies extracted scalar kinds.
/// </summary>
public enum DotNetScalarKind
{
    /// <summary>Boolean scalar.</summary>
    Boolean,
    /// <summary>String scalar.</summary>
    String,
    /// <summary>Integer scalar.</summary>
    Integer,
    /// <summary>Number scalar.</summary>
    Number,
    /// <summary>Decimal scalar.</summary>
    Decimal,
    /// <summary>Date scalar.</summary>
    Date,
    /// <summary>Time scalar.</summary>
    Time,
    /// <summary>DateTime scalar.</summary>
    DateTime,
    /// <summary>DateTimeOffset scalar.</summary>
    DateTimeOffset,
    /// <summary>Duration scalar.</summary>
    Duration,
    /// <summary>Guid scalar.</summary>
    Guid,
    /// <summary>Binary scalar.</summary>
    Binary,
    /// <summary>Json scalar.</summary>
    Json,
}
