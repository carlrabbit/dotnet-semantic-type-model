using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.M0060.ModelA.Generated", "InventorySemanticTypeModel")]

namespace SemanticTypeModel.M0060.ModelA;

[SemanticType(SemanticTypeRole.Entity)]
public sealed class InventoryItem
{
    [SemanticKey]
    public InventoryItemId Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public InventoryState State { get; set; }
    public byte[] Payload { get; set; } = [];
    public ReadOnlyMemory<byte> ReadOnlyPayload { get; set; }
    public Uri Endpoint { get; set; } = new("relative", UriKind.Relative);
    public Uri? OptionalEndpoint { get; set; }
    [SemanticOwned]
    public InventoryDetails Details { get; set; } = new();
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class InventoryDetails
{
    public string Warehouse { get; set; } = string.Empty;
    public int Quantity { get; set; }
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
