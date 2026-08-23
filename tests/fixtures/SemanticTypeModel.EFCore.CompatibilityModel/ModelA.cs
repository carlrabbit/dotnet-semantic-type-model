using System.Text.Json;
using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.EFCore.CompatibilityModel.Generated", "InventorySemanticTypeModel")]

namespace SemanticTypeModel.EFCore.CompatibilityModel;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class InventoryItem
{
    [SemanticKey]
    public InventoryItemId Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public InventoryState State { get; set; }
    public byte[] Payload { get; set; } = [];
    public ReadOnlyMemory<byte> ReadOnlyPayload { get; set; }
    public ReadOnlyMemory<byte>? OptionalReadOnlyPayload { get; set; }
    public Uri Endpoint { get; set; } = new("relative", UriKind.Relative);
    public Uri? OptionalEndpoint { get; set; }
    [SemanticOwned]
    public InventoryDetails Details { get; set; } = new();
    [SemanticOwned]
    public InventoryDetails? OptionalDetails { get; set; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)]
    public IReadOnlyList<InventoryDetails> DetailHistory { get; set; } = [];
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)]
    public IReadOnlyList<InventoryDetails>? OptionalDetailHistory { get; set; }
    public string? OptionalDisplayName { get; set; }
    public InventoryState? OptionalState { get; set; }
    public InventoryItemId? OptionalExternalId { get; set; }
    public byte[]? OptionalPayload { get; set; }
    [SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
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
    [SemanticKey]
    public Guid Id { get; set; }

    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)]
    public IReadOnlyList<SpecificationStateEntry> Entries { get; set; } = [];
}

[SemanticStrongScalar]
public readonly record struct SpecificationVersionId(Guid Value);

[SemanticType(SemanticTypeRole.Entity)]
public abstract class InventoryDocument
{
    [SemanticKey]
    public Guid Id { get; set; }
    [SemanticOwned]
    public InventoryDetails? OptionalDetails { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecializedInventoryDocument : InventoryDocument
{
    [SemanticOwned]
    public InventoryDetails RequiredDetails { get; set; } = new();
}

[SemanticType]
public readonly record struct InventoryItemId(Guid Value);

[SemanticType]
public enum InventoryState { Active, Archived }

[SemanticType(SemanticTypeRole.Configuration)]
public sealed class InventoryOptions { public string Region { get; set; } = string.Empty; }

// Deliberately outside the semantic model. The persistence project may still own it as an ordinary EF entity.
public sealed class ModelAExternalEntity
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
}

// Deliberately outside both models and EF; referencing the assembly must not discover this type.
public sealed class ModelAIgnoredPoco { public int Id { get; set; } }
