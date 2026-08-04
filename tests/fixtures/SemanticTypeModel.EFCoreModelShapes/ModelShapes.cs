using System.Text.Json;

namespace SemanticTypeModel.EFCoreModelShapes;

#pragma warning disable CS1591
// Deliberately small CLR shapes used by the M0057 member-placement matrix.
public sealed record FlatOrder(Guid Id, string Number);
public abstract record VersionedObject { public int SchemaVersion { get; init; } }
public sealed record VersionedOrder(Guid Id, string Number) : VersionedObject;
public abstract record ExtensibleObject { public Dictionary<string, JsonElement>? ExtensionData { get; init; } }
public sealed record ExtensibleOrder(Guid Id) : ExtensibleObject;
public abstract record SourceConfiguredObject { public SourceOptions? Source { get; init; } }
public sealed record SourceOrder(Guid Id) : SourceConfiguredObject;
public abstract record FieldConfiguredObject { public IReadOnlyList<DerivedField> DerivedFields { get; init; } = []; }
public sealed record FieldConfiguredOrder(Guid Id) : FieldConfiguredObject;
public abstract record VersionedExtensibleObject : ExtensibleObject { public int SchemaVersion { get; init; } }
public abstract record Specification(Guid Id, string DisplayName) : VersionedExtensibleObject;
public sealed record ImportSpecification(Guid Id, string DisplayName, string ImportName) : Specification(Id, DisplayName);
public sealed record WorkflowSpecification(Guid Id, string DisplayName, string WorkflowName) : Specification(Id, DisplayName);
public abstract record VersionedValue { public int Version { get; init; } }
public sealed record SourceOptions(Uri Endpoint, RetryPolicy? Retry) : VersionedValue;
public sealed record RetryPolicy(int Attempts);
public sealed record DerivedField(string Name);
public sealed record SourceConsumer(Guid Id, SourceOptions Source);
public sealed record AlternateSourceConsumer(Guid Id, SourceOptions Source);
public sealed record PollutedValueKind(SourceOptions Source);
public abstract record HiddenBase { public string Code { get; init; } = string.Empty; }
public sealed record HiddenOrder(Guid Id) : HiddenBase { public new string Code { get; init; } = string.Empty; }
public abstract record SemanticDuplicateBase(Guid Id) { public string Name { get; init; } = string.Empty; }
public sealed record SemanticDuplicateDerived(Guid Id) : SemanticDuplicateBase(Id) { public new string Name { get; init; } = string.Empty; }
public abstract record StructuralGrandbase { public string Tenant { get; init; } = string.Empty; }
public abstract record SemanticChainBase(Guid Id) : StructuralGrandbase;
public sealed record SemanticChainDerived(Guid Id) : SemanticChainBase(Id);
public abstract record JsonBase(Guid Id) { public SourceOptions? OptionalSource { get; init; } }
public sealed record JsonDerived(Guid Id, SourceOptions RequiredSource) : JsonBase(Id);

public static class ModelShapeInventory
{
    public static IReadOnlyList<string> RequiredShapes { get; } =
    [
        "FlatEntity", "NonSemanticBaseScalar", "NonSemanticBaseExtensionData",
        "NonSemanticBaseValueKindObject", "NonSemanticBaseValueKindCollection", "SemanticTpt",
        "TptNonSemanticGrandbase", "ReusedValueKind", "InheritedValueKindScalar", "NestedValueKind",
        "PollutedModelBuilder", "HiddenProperty", "SemanticDuplicateProperty", "StructuralSemanticChain",
        "BaseOptionalDerivedRequiredValueKind",
    ];
}
#pragma warning restore CS1591
