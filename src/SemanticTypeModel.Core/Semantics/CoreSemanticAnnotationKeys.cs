namespace SemanticTypeModel.Core.Semantics;

/// <summary>
/// Defines canonical annotation keys for projection-neutral core semantic vocabulary entries.
/// </summary>
public static class CoreSemanticAnnotationKeys
{
    /// <summary>Marks an object type as an envelope wrapper boundary.</summary>
    public const string Envelope = "schema.envelope";

    /// <summary>Stores the optional projection-neutral purpose for an envelope wrapper.</summary>
    public const string EnvelopePurpose = "schema.envelope.purpose";

    /// <summary>Marks the single payload member carried by an envelope wrapper.</summary>
    public const string EnvelopePayload = "schema.envelope.payload";

    /// <summary>Marks a member as envelope lifecycle/context metadata rather than payload domain state.</summary>
    public const string EnvelopeMetadata = "schema.envelope.metadata";
}
