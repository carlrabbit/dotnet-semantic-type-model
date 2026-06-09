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

    /// <summary>Marks a member as lifecycle-contained by its owner.</summary>
    public const string Ownership = "schema.ownership";

    /// <summary>Stores the ownership kind for owned object or collection members.</summary>
    public const string OwnershipKind = "schema.ownership.kind";

    /// <summary>Marks a single object-valued member as owned by its containing object.</summary>
    public const string OwnedObject = "schema.ownedObject";

    /// <summary>Marks a collection-valued member as containing owned elements.</summary>
    public const string OwnedCollection = "schema.ownedCollection";

    /// <summary>Marks a type as participating in version or revision evolution.</summary>
    public const string Versioned = "schema.versioned";

    /// <summary>Marks a member as a version identifier.</summary>
    public const string Version = "schema.version";

    /// <summary>Marks a member as an instance revision identifier.</summary>
    public const string Revision = "schema.revision";

    /// <summary>Marks a member as the current version or revision indicator.</summary>
    public const string CurrentVersion = "schema.currentVersion";

    /// <summary>Marks a type as having a temporal validity interval.</summary>
    public const string TemporalValidity = "schema.temporalValidity";

    /// <summary>Marks a member as the start of a validity interval.</summary>
    public const string ValidFrom = "schema.validFrom";

    /// <summary>Marks a member as the optional end of a validity interval.</summary>
    public const string ValidTo = "schema.validTo";

    /// <summary>Marks a member as lifecycle state.</summary>
    public const string LifecycleState = "schema.lifecycleState";

    /// <summary>Marks a dictionary-like member as instance extension data.</summary>
    public const string ExtensionData = "schema.extensionData";

    /// <summary>Records the key type for an extension-data bag.</summary>
    public const string ExtensionDataKeyType = "schema.extensionData.keyType";

    /// <summary>Records the value type for an extension-data bag.</summary>
    public const string ExtensionDataValueType = "schema.extensionData.valueType";
}
