namespace SemanticTypeModel.Core.Diagnostics;

/// <summary>
/// Contains all stable diagnostic IDs emitted by SemanticTypeModel core packages.
/// </summary>
/// <remarks>
/// <para>ID ranges:</para>
/// <list type="bullet">
/// <item><description>STM0xxx — Semantic model validation (emitted by <c>SemanticTypeModel.Core</c>)</description></item>
/// <item><description>STM3xxx — JSON Schema runtime projection (emitted by <c>SemanticTypeModel.JsonSchema</c>)</description></item>
/// </list>
/// <para>
/// STM5xxx (.NET extraction) IDs are defined in
/// <c>SemanticTypeModel.DotNet.Diagnostics.DotNetExtractionDiagnosticIds</c>.
/// </para>
/// </remarks>
public static class StmDiagnosticIds
{
    // -------------------------------------------------------------------------
    // STM0xxx — Semantic model validation
    // Emitted by TypeSchemaModelValidator during the Validation pipeline stage.
    // -------------------------------------------------------------------------

    /// <summary>STM0001 — Duplicate TypeId in the model.</summary>
    public const string DuplicateTypeId = "STM0001";

    /// <summary>STM0002 — Unresolved TypeRef: a property or member references a type that does not exist.</summary>
    public const string UnresolvedTypeRef = "STM0002";

    /// <summary>STM0003 — Duplicate property name within the same object type (case-insensitive).</summary>
    public const string DuplicatePropertyName = "STM0003";

    /// <summary>STM0004 — Duplicate key name within the same object type (case-insensitive).</summary>
    public const string DuplicateKeyName = "STM0004";

    /// <summary>STM0005 — Key references a property that does not exist on the owning type.</summary>
    public const string KeyPropertyRefMissing = "STM0005";

    /// <summary>STM0006 — Relationship references a type that is missing or is not an object type.</summary>
    public const string RelationshipTypeMissing = "STM0006";

    /// <summary>STM0007 — Relationship references a property that does not exist on the owning type.</summary>
    public const string RelationshipPropertyRefMissing = "STM0007";

    /// <summary>STM0008 — Cardinality bounds are invalid (negative or min exceeds max).</summary>
    public const string InvalidCardinalityBounds = "STM0008";

    /// <summary>STM0009 — String constraint bounds are invalid (negative or min exceeds max).</summary>
    public const string InvalidStringConstraintBounds = "STM0009";

    /// <summary>STM0010 — Numeric constraint bounds are invalid (minimum exceeds maximum).</summary>
    public const string InvalidNumericConstraintBounds = "STM0010";

    /// <summary>STM0011 — Annotation key is malformed or uses non-canonical reserved-namespace casing.</summary>
    public const string MalformedAnnotationKey = "STM0011";

    /// <summary>STM0012 — Enum type contains duplicate value names (case-insensitive).</summary>
    public const string DuplicateEnumValueName = "STM0012";

    /// <summary>STM0013 — Enum type contains duplicate value payloads.</summary>
    public const string DuplicateEnumValuePayload = "STM0013";

    // -------------------------------------------------------------------------
    // STM3xxx — JSON Schema runtime projection
    // Emitted by JsonSchemaRuntimeProjection during the Projection pipeline stage.
    // -------------------------------------------------------------------------

    /// <summary>STM3201 — Model root id did not match a type id; a fallback root type was used.</summary>
    public const string JsonSchemaRuntimeRootFallback = "STM3201";

    /// <summary>STM3202 — Object type contains semantic members (keys, relationships, computed members, allOf composition) not projected by the current JSON Schema runtime adapter.</summary>
    public const string JsonSchemaRuntimeSemanticMembersSkipped = "STM3202";

    /// <summary>STM3203 — Dictionary type projects to a JSON object with string keys; non-string key metadata is not represented.</summary>
    public const string JsonSchemaRuntimeDictionaryKeyMetadataLost = "STM3203";

    /// <summary>STM3204 — Union type with discriminator or anyOf semantics is approximated as oneOf.</summary>
    public const string JsonSchemaRuntimeUnionApproximated = "STM3204";

    /// <summary>STM3205 — Intersection type is approximated as a union because the runtime adapter has no allOf-aware path.</summary>
    public const string JsonSchemaRuntimeIntersectionApproximated = "STM3205";

    /// <summary>STM3206 — Type kind is not supported by the current JSON Schema runtime projection adapter; projected as string.</summary>
    public const string JsonSchemaRuntimeUnsupportedTypeKind = "STM3206";

    /// <summary>STM3207 — Enum value is not a string; serialized using its string representation.</summary>
    public const string JsonSchemaRuntimeNonStringEnumValue = "STM3207";
}
