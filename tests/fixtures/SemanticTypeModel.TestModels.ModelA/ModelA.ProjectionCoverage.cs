using System.Text.Json;
using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.TestModels.ModelA;

[SemanticStrongScalar] public readonly record struct BooleanId(bool Value);
[SemanticStrongScalar] public readonly record struct StringId(string Value);
[SemanticStrongScalar] public readonly record struct IntegerId(long Value);
[SemanticStrongScalar] public readonly record struct NumberId(double Value);
[SemanticStrongScalar] public readonly record struct DecimalId(decimal Value);
[SemanticStrongScalar] public readonly record struct DateId(DateOnly Value);
[SemanticStrongScalar] public readonly record struct TimeId(TimeOnly Value);
[SemanticStrongScalar] public readonly record struct DateTimeId(DateTime Value);
[SemanticStrongScalar] public readonly record struct DateTimeOffsetId(DateTimeOffset Value);
[SemanticStrongScalar] public readonly record struct DurationId(TimeSpan Value);
[SemanticStrongScalar] public readonly record struct GuidId(Guid Value);
[SemanticStrongScalar] public readonly record struct BinaryId(ReadOnlyMemory<byte> Value);

[SemanticType(SemanticTypeRole.Entity)]
public sealed class InventoryItem
{
    [SemanticKey] public InventoryItemId Id { get; set; }
    [SemanticDisplayIdentity] public string DisplayName { get; set; } = string.Empty;
    public InventoryState State { get; set; }
    public byte[] Payload { get; set; } = [];
    public ReadOnlyMemory<byte> ReadOnlyPayload { get; set; }
    public ReadOnlyMemory<byte>? OptionalReadOnlyPayload { get; set; }
    public Uri Endpoint { get; set; } = new("relative", UriKind.Relative);
    public Uri? OptionalEndpoint { get; set; }
    [SemanticOwned] public required InventoryDetails Details { get; set; } = new();
    [SemanticOwned] public InventoryDetails? OptionalDetails { get; set; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public required IReadOnlyList<InventoryDetails> DetailHistory { get; set; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<InventoryDetails>? OptionalDetailHistory { get; set; }
    public string? OptionalDisplayName { get; set; }
    public InventoryState? OptionalState { get; set; }
    public InventoryItemId? OptionalExternalId { get; set; }
    public byte[]? OptionalPayload { get; set; }
    [SemanticExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class InventoryDetails
{
    public string Warehouse { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class SpecificationStateEntry
{
    public SpecificationVersionId SpecificationVersionId { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecificationState
{
    [SemanticKey] public Guid Id { get; set; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<SpecificationStateEntry> Entries { get; set; } = [];
}

[SemanticStrongScalar] public readonly record struct InventoryItemId(Guid Value);
[SemanticStrongScalar] public readonly record struct SpecificationVersionId(Guid Value);
[SemanticType] public enum InventoryState { Active, Archived }

[SemanticType(SemanticTypeRole.Entity)]
public abstract class InventoryDocument
{
    [SemanticKey] public Guid Id { get; set; }
    [SemanticOwned] public InventoryDetails? OptionalDetails { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecializedInventoryDocument : InventoryDocument
{
    [SemanticOwned] public InventoryDetails RequiredDetails { get; set; } = new();
}

[SemanticType(SemanticTypeRole.Configuration)]
public sealed class InventoryOptions
{
    public string Region { get; set; } = string.Empty;
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class StorageMatrixEntity
{
    [SemanticKey] public Guid Id { get; set; }
    public required string RequiredText { get; set; }
    public string? OptionalText { get; set; }
    public required MatrixState RequiredState { get; set; }
    public MatrixState? OptionalState { get; set; }
    public required MatrixStrongId RequiredStrongId { get; set; }
    public MatrixStrongId? OptionalStrongId { get; set; }
    public required Uri RequiredUri { get; set; }
    public Uri? OptionalUri { get; set; }
    public required byte[] RequiredBinary { get; set; }
    public byte[]? OptionalBinary { get; set; }
    public required ReadOnlyMemory<byte> RequiredReadOnlyMemory { get; set; }
    public ReadOnlyMemory<byte>? OptionalReadOnlyMemory { get; set; }
    [SemanticOwned] public required MatrixDetails RequiredDetails { get; set; } = new("");
    [SemanticOwned] public MatrixDetails? OptionalDetails { get; set; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public required IReadOnlyList<MatrixDetails> RequiredDetailsCollection { get; set; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<MatrixDetails>? OptionalDetailsCollection { get; set; }
    [SemanticExtensionData] public Dictionary<string, JsonElement>? MatrixExtensionData { get; set; }
}

[SemanticStrongScalar] public readonly record struct MatrixStrongId(Guid Value);
[SemanticType] public enum MatrixState { Active, Archived }
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record MatrixDetails(string Name);

[SemanticType]
public enum CoverageState { Draft, Active, Retired }

[SemanticType(SemanticTypeRole.ValueObject)]
[SemanticUserDescription("Shared projection coverage metadata")]
[SemanticTechnicalDescription("Projection-neutral dimension coverage fixture")]
[SemanticMutable]
public sealed class CoverageMetadata
{
    [SemanticKey]
    [SemanticDisplayName("Coverage name")]
    [SemanticDisplayIdentity(Order = 0)]
    [SemanticAccessPath("by-name", Order = 0)]
    [SemanticStringConstraints(MinLength = 1, MaxLength = 80, Pattern = "^[A-Za-z].*")]
    [SemanticFormat(SemanticScalarFormat.Hostname)]
    [SemanticAnnotation("ui.widget", "text")]
    public required string Name { get; set; }

    [SemanticNumericConstraints(Minimum = 0, Maximum = 1000, MultipleOf = 0.5)]
    [SemanticAnnotation("ui.format", "currency")]
    public decimal Amount { get; set; }

    [SemanticCollectionConstraints(MinItems = 1, MaxItems = 5, UniqueItems = true)]
    public required IReadOnlyList<string> Tags { get; set; } = [];

    [SemanticLifecycleState]
    public CoverageState State { get; set; }

    [SemanticVersion]
    public int SchemaVersion { get; set; }

    [SemanticRequiredWhen(nameof(State), nameof(CoverageState.Active))]
    public string? ActivationNote { get; set; }
}

[SemanticEnvelope("coverage")]
[SemanticVersioned]
[SemanticTemporalValidity]
[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class CoverageEnvelope
{
    [SemanticEnvelopePayload]
    public required CoverageMetadata Payload { get; set; } = new() { Name = "coverage", Tags = [] };

    [SemanticEnvelopeMetadata]
    [SemanticCurrentVersion]
    public required string EnvelopeVersion { get; set; }

    [SemanticRevision]
    public string? Revision { get; set; }

    public DateTime? ValidityMarker { get; set; }

    [SemanticValidFrom]
    public DateTime ValidFrom { get; set; }

    [SemanticValidTo]
    public DateTime? ValidTo { get; set; }
}
