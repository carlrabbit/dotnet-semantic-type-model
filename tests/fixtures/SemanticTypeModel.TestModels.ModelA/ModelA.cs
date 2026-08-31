using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.TestModels.ModelA.Generated", "ModelASemanticTypeModel", IncludeInternalTypes = true)]

namespace SemanticTypeModel.TestModels.ModelA;

[SemanticType(SemanticTypeRole.Entity)]
public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecialEntity : BaseEntity
{
    public SpecialId SpecialId { get; set; }
    [SemanticOwned] public Details Details { get; set; } = new();
}

[SemanticStrongScalar]
public readonly record struct SpecialId(Guid Value);

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Details
{
    public string Label { get; set; } = string.Empty;
}

[SemanticType]
public enum State { Active, Archived }

[SemanticType(SemanticTypeRole.Entity)]
public sealed class RuntimeContainer
{
    public SpecialId? OptionalId { get; set; }
    public State? State { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
