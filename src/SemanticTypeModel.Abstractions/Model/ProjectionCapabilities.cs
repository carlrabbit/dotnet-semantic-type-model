// CS1591 is disabled in this contracts file because the capability matrix contains many enum members whose intent
// is documented by the corresponding spec and matrix docs rather than per-member XML comments.
#pragma warning disable CS1591
// IDE0305 and CA1859 are disabled to keep matrix declarations explicit and deterministic for test snapshots.
#pragma warning disable IDE0305
#pragma warning disable CA1859

namespace SemanticTypeModel.Abstractions.Model;

/// <summary>
/// Identifies canonical semantic-model features tracked in projection capability contracts.
/// </summary>
public enum SemanticModelFeature
{
    ObjectType,
    ScalarProperty,
    RequiredProperty,
    NullableProperty,
    Array,
    Dictionary,
    Enum,
    Union,
    Reference,
    ValueObject,
    EntityRole,
    PrimaryKey,
    AlternateKey,
    Relationship,
    ComputedMember,
    ValidationConstraints,
    DisplayMetadata,
    UiHints,
    ProjectionSpecificAnnotations,
    RecursiveType,
    ClosedGenericType,
    OpenGenericType,
}

/// <summary>
/// Declares support level for a semantic-model feature in a projection target.
/// </summary>
public enum ProjectionFeatureSupportLevel
{
    Supported,
    SupportedWithOptions,
    PartiallySupported,
    RepresentedAsAnnotation,
    Ignored,
    Unsupported,
    UnsupportedWithDiagnostic,
}

/// <summary>
/// Describes how a projection target handles a specific semantic-model feature.
/// </summary>
public sealed record ProjectionFeatureCapability
{
    /// <summary>
    /// Gets the semantic-model feature.
    /// </summary>
    public required SemanticModelFeature Feature { get; init; }

    /// <summary>
    /// Gets the declared support level.
    /// </summary>
    public required ProjectionFeatureSupportLevel SupportLevel { get; init; }

    /// <summary>
    /// Gets optional notes describing degradation behavior or compatibility details.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets annotations required for projection behavior.
    /// </summary>
    public IReadOnlyList<string> RequiredAnnotations { get; init; } = [];

    /// <summary>
    /// Gets optional annotations that influence projection behavior.
    /// </summary>
    public IReadOnlyList<string> OptionalAnnotations { get; init; } = [];

    /// <summary>
    /// Gets diagnostic codes relevant to this feature capability.
    /// </summary>
    public IReadOnlyList<string> DiagnosticCodes { get; init; } = [];
}

/// <summary>
/// Describes projection compatibility metadata for one target projection.
/// </summary>
public sealed record ProjectionCompatibilityContract
{
    /// <summary>
    /// Gets the projection target.
    /// </summary>
    public required ProjectionTarget Projection { get; init; }

    /// <summary>
    /// Gets feature-level capabilities for the projection target.
    /// </summary>
    public required IReadOnlyList<ProjectionFeatureCapability> Features { get; init; }

    /// <summary>
    /// Gets capability metadata for the specified feature.
    /// </summary>
    /// <param name="feature">The feature to inspect.</param>
    /// <returns>The feature capability metadata.</returns>
    public ProjectionFeatureCapability GetSupport(SemanticModelFeature feature)
    {
        ProjectionFeatureCapability? capability = Features.FirstOrDefault(candidate => candidate.Feature == feature);
        return capability ?? throw new KeyNotFoundException($"Projection '{Projection}' does not declare capability for feature '{feature}'.");
    }
}

/// <summary>
/// Exposes projection capability metadata for runtime inspection and documentation synchronization.
/// </summary>
public interface IProjectionCapabilityProvider
{
    /// <summary>
    /// Gets projection capability metadata.
    /// </summary>
    ProjectionCompatibilityContract GetCapabilities();
}

/// <summary>
/// Provides deterministic projection capability contracts for supported projection targets.
/// </summary>
public static class ProjectionCapabilityCatalog
{
    private static readonly IReadOnlyList<SemanticModelFeature> CoreFeatures = Enum.GetValues<SemanticModelFeature>().OrderBy(static feature => feature).ToArray();

    private static readonly IReadOnlyDictionary<ProjectionTarget, ProjectionCompatibilityContract> Contracts =
        new Dictionary<ProjectionTarget, ProjectionCompatibilityContract>
        {
            [ProjectionTarget.JsonSchema] = new()
            {
                Projection = ProjectionTarget.JsonSchema,
                Features = CreateFeatures(
                    [Supported(SemanticModelFeature.ObjectType), Supported(SemanticModelFeature.ScalarProperty), Supported(SemanticModelFeature.RequiredProperty), Supported(SemanticModelFeature.NullableProperty), Supported(SemanticModelFeature.Array), Partial(SemanticModelFeature.Dictionary, "Dictionary keys project as strings.", "STM3203"), Partial(SemanticModelFeature.Enum, "Non-string enum values degrade to string payloads.", "STM3207"), Partial(SemanticModelFeature.Union, "AnyOf/discriminator unions degrade to oneOf semantics.", "STM3204"), Supported(SemanticModelFeature.Reference), Annotation(SemanticModelFeature.ValueObject, "Value-object semantics are preserved through annotations.", "STM3202"), Annotation(SemanticModelFeature.EntityRole, "Entity role metadata remains annotation-based.", "STM3202"), Annotation(SemanticModelFeature.PrimaryKey, "Key metadata is not emitted as schema keywords.", "STM3202"), Annotation(SemanticModelFeature.AlternateKey, "Alternate key metadata is not emitted as schema keywords.", "STM3202"), Annotation(SemanticModelFeature.Relationship, "Relationship metadata is annotation-preserved.", "STM3202"), Annotation(SemanticModelFeature.ComputedMember, "Computed members are not represented as JSON Schema keywords.", "STM3202"), SupportedWithOptions(SemanticModelFeature.ValidationConstraints, "Constraint fidelity depends on the represented keyword set."), Supported(SemanticModelFeature.DisplayMetadata), SupportedWithOptions(SemanticModelFeature.UiHints, "UI hints require explicit UI export mode.", "JSONSCHEMA_UI_DOWNSTREAM_MODE_REQUIRED"), SupportedWithOptions(SemanticModelFeature.ProjectionSpecificAnnotations, "Projection-specific annotation emission is option-controlled."), Supported(SemanticModelFeature.RecursiveType), Supported(SemanticModelFeature.ClosedGenericType), Unsupported(SemanticModelFeature.OpenGenericType, "Open generic contracts are not representable in JSON Schema projections.")]),
            },
            [ProjectionTarget.JsonEditor] = new()
            {
                Projection = ProjectionTarget.JsonEditor,
                Features = CreateFeatures(
                    [Supported(SemanticModelFeature.ObjectType), Supported(SemanticModelFeature.ScalarProperty), Supported(SemanticModelFeature.RequiredProperty), Supported(SemanticModelFeature.NullableProperty), Supported(SemanticModelFeature.Array), Partial(SemanticModelFeature.Dictionary, "Dictionaries follow JSON object semantics and may require UI-specific adaptation."), SupportedWithOptions(SemanticModelFeature.Enum, "Enum labels and editor options depend on UI hint mappings."), Supported(SemanticModelFeature.Union), Supported(SemanticModelFeature.Reference), Ignored(SemanticModelFeature.ValueObject, "Value-object semantics are not a first-class editor concern."), Ignored(SemanticModelFeature.EntityRole, "Entity role metadata is ignored by editor projections."), Ignored(SemanticModelFeature.PrimaryKey, "Key semantics are ignored by editor projections."), Ignored(SemanticModelFeature.AlternateKey, "Key semantics are ignored by editor projections."), Ignored(SemanticModelFeature.Relationship, "Relationship semantics are ignored by editor projections."), Ignored(SemanticModelFeature.ComputedMember, "Computed members are ignored by editor projections."), SupportedWithOptions(SemanticModelFeature.ValidationConstraints, "Supported for schema-backed validations represented in JSON Schema."), SupportedWithOptions(SemanticModelFeature.DisplayMetadata, "Display metadata depends on available ui.* hints."), Supported(SemanticModelFeature.UiHints), SupportedWithOptions(SemanticModelFeature.ProjectionSpecificAnnotations, "jsonEditor.* hints are validated by UI options.", "JSONSCHEMA_UI_UNSUPPORTED_JSONEDITOR_KEY"), Supported(SemanticModelFeature.RecursiveType), Supported(SemanticModelFeature.ClosedGenericType), Unsupported(SemanticModelFeature.OpenGenericType, "Open generic contracts are not representable in editor projections.")]),
            },
            [ProjectionTarget.EfCore] = new()
            {
                Projection = ProjectionTarget.EfCore,
                Features = CreateFeatures(
                    [SupportedWithOptions(SemanticModelFeature.ObjectType, "Requires entity semantics or explicit EF options.", "EFCORE_OBJECT_NOT_PROJECTED"), Supported(SemanticModelFeature.ScalarProperty), Supported(SemanticModelFeature.RequiredProperty), Supported(SemanticModelFeature.NullableProperty), SupportedWithOptions(SemanticModelFeature.Array, "Array handling depends on unsupported-shape behavior options.", "EFCORE_ARRAY_UNSUPPORTED"), SupportedWithOptions(SemanticModelFeature.Dictionary, "Dictionary handling depends on unsupported-shape behavior options.", "EFCORE_DICTIONARY_UNSUPPORTED"), Supported(SemanticModelFeature.Enum), SupportedWithOptions(SemanticModelFeature.Union, "Union handling depends on unsupported-shape behavior options.", "EFCORE_UNION_UNSUPPORTED"), Supported(SemanticModelFeature.Reference), SupportedWithOptions(SemanticModelFeature.ValueObject, "Owned, flatten, serialize, or diagnose behaviors are option-controlled.", "EFCORE_VALUE_OBJECT_REQUIRES_MODE"), SupportedWithOptions(SemanticModelFeature.EntityRole, "Entity-role projection can be explicit or convention/option based.", "EFCORE_OBJECT_NOT_PROJECTED"), Supported(SemanticModelFeature.PrimaryKey), SupportedWithOptions(SemanticModelFeature.AlternateKey, "Alternate keys can project as alternate keys or unique indexes."), SupportedWithOptions(SemanticModelFeature.Relationship, "Many-to-many relationships are diagnosed and skipped.", "EFCORE_MANY_TO_MANY_UNSUPPORTED"), UnsupportedWithDiagnostic(SemanticModelFeature.ComputedMember, "Computed members are not projected to EF metadata."), Annotation(SemanticModelFeature.ValidationConstraints, "Constraint details are preserved as annotations when not directly representable.", "EFCORE_STRING_PATTERN_PRESERVED_AS_ANNOTATION"), SupportedWithOptions(SemanticModelFeature.DisplayMetadata, "Display name preference is option-controlled for table/column names."), Ignored(SemanticModelFeature.UiHints, "UI hints are not projected to EF metadata."), Supported(SemanticModelFeature.ProjectionSpecificAnnotations), UnsupportedWithDiagnostic(SemanticModelFeature.RecursiveType, "Recursive flattening/value-object paths are diagnosed.", "EFCORE_VALUE_OBJECT_FLATTEN_UNSUPPORTED"), Supported(SemanticModelFeature.ClosedGenericType), Unsupported(SemanticModelFeature.OpenGenericType, "Open generic contracts are not representable in EF projection.")]),
            },
            [ProjectionTarget.PowerBi] = new()
            {
                Projection = ProjectionTarget.PowerBi,
                Features = CreateFeatures(
                    [SupportedWithOptions(SemanticModelFeature.ObjectType, "Requires table-role metadata or explicit projection options.", "POWERBI_OBJECT_NOT_PROJECTED"), Supported(SemanticModelFeature.ScalarProperty), Supported(SemanticModelFeature.RequiredProperty), Supported(SemanticModelFeature.NullableProperty), SupportedWithOptions(SemanticModelFeature.Array, "Array handling depends on unsupported-shape behavior options.", "POWERBI_UNSUPPORTED_SHAPE"), SupportedWithOptions(SemanticModelFeature.Dictionary, "Dictionary handling depends on unsupported-shape behavior options.", "POWERBI_UNSUPPORTED_SHAPE"), Supported(SemanticModelFeature.Enum), SupportedWithOptions(SemanticModelFeature.Union, "Union handling depends on unsupported-shape behavior options.", "POWERBI_UNSUPPORTED_SHAPE"), Supported(SemanticModelFeature.Reference), SupportedWithOptions(SemanticModelFeature.ValueObject, "Flatten/serialize/diagnose behavior is option-controlled.", "POWERBI_VALUE_OBJECT_UNSUPPORTED"), Supported(SemanticModelFeature.EntityRole), SupportedWithOptions(SemanticModelFeature.PrimaryKey, "Relationship projection expects key metadata on endpoint tables.", "POWERBI_RELATIONSHIP_MISSING_KEY"), PartiallySupported(SemanticModelFeature.AlternateKey, "Alternate keys are not independently modeled beyond relationship endpoint resolution."), SupportedWithOptions(SemanticModelFeature.Relationship, "Many-to-many relationships are diagnosed and skipped.", "POWERBI_MANY_TO_MANY_RELATIONSHIP_UNSUPPORTED"), SupportedWithOptions(SemanticModelFeature.ComputedMember, "Computed members require DAX expressions.", "POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE"), Ignored(SemanticModelFeature.ValidationConstraints, "Validation constraints are not projected as tabular metadata."), SupportedWithOptions(SemanticModelFeature.DisplayMetadata, "Display metadata maps to table and measure display folders/options."), Annotation(SemanticModelFeature.UiHints, "UI hints are preserved as annotations, not tabular metadata."), Supported(SemanticModelFeature.ProjectionSpecificAnnotations), UnsupportedWithDiagnostic(SemanticModelFeature.RecursiveType, "Recursive value-object flattening cycles are diagnosed.", "POWERBI_VALUE_OBJECT_FLATTEN_CYCLE_UNSUPPORTED"), Supported(SemanticModelFeature.ClosedGenericType), Unsupported(SemanticModelFeature.OpenGenericType, "Open generic contracts are not representable in tabular projection.")]),
            },
        };

    /// <summary>
    /// Gets the core feature list used by projection capability contracts.
    /// </summary>
    public static IReadOnlyList<SemanticModelFeature> GetCoreFeatures()
    {
        return CoreFeatures;
    }

    /// <summary>
    /// Gets all declared projection capability contracts in deterministic order.
    /// </summary>
    public static IReadOnlyList<ProjectionCompatibilityContract> GetAll()
    {
        return Contracts.Values.OrderBy(static contract => contract.Projection).ToArray();
    }

    /// <summary>
    /// Gets the declared projection capability contract for a target projection.
    /// </summary>
    /// <param name="target">The projection target.</param>
    /// <returns>The projection compatibility contract.</returns>
    public static ProjectionCompatibilityContract ForTarget(ProjectionTarget target)
    {
        return Contracts.TryGetValue(target, out ProjectionCompatibilityContract? contract)
            ? contract
            : throw new KeyNotFoundException($"Projection capability contract is not declared for target '{target}'.");
    }

    private static ProjectionFeatureCapability[] CreateFeatures(IReadOnlyList<ProjectionFeatureCapability> declared)
    {
        var byFeature = declared.ToDictionary(static capability => capability.Feature);
        return CoreFeatures.Select(feature => byFeature.TryGetValue(feature, out ProjectionFeatureCapability? capability)
            ? capability
            : Unsupported(feature, "Not declared for this projection target.")).ToArray();
    }

    private static ProjectionFeatureCapability Supported(SemanticModelFeature feature)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.Supported };
    }

    private static ProjectionFeatureCapability SupportedWithOptions(SemanticModelFeature feature, string notes, params string[] diagnosticCodes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.SupportedWithOptions, Notes = notes, DiagnosticCodes = diagnosticCodes };
    }

    private static ProjectionFeatureCapability PartiallySupported(SemanticModelFeature feature, string notes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.PartiallySupported, Notes = notes };
    }

    private static ProjectionFeatureCapability Partial(SemanticModelFeature feature, string notes, params string[] diagnosticCodes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.PartiallySupported, Notes = notes, DiagnosticCodes = diagnosticCodes };
    }

    private static ProjectionFeatureCapability Annotation(SemanticModelFeature feature, string notes, params string[] diagnosticCodes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.RepresentedAsAnnotation, Notes = notes, DiagnosticCodes = diagnosticCodes };
    }

    private static ProjectionFeatureCapability Ignored(SemanticModelFeature feature, string notes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.Ignored, Notes = notes };
    }

    private static ProjectionFeatureCapability Unsupported(SemanticModelFeature feature, string notes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.Unsupported, Notes = notes };
    }

    private static ProjectionFeatureCapability UnsupportedWithDiagnostic(SemanticModelFeature feature, string notes, params string[] diagnosticCodes)
    {
        return new ProjectionFeatureCapability { Feature = feature, SupportLevel = ProjectionFeatureSupportLevel.UnsupportedWithDiagnostic, Notes = notes, DiagnosticCodes = diagnosticCodes };
    }
}

#pragma warning restore CA1859
#pragma warning restore IDE0305
#pragma warning restore CS1591
