using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.TestModels.ModelA.Generated", "ModelASemanticTypeModel", IncludeInternalTypes = true)]

namespace SemanticTypeModel.TestModels.ModelA;

[SemanticType(SemanticTypeRole.Entity)]
public abstract class BaseEntity
{
    [SemanticKey]
    public Guid Id { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecialEntity : BaseEntity
{
    public Guid SpecialId { get; set; }
    [SemanticOwned] public Details Details { get; set; } = new();
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Details
{
    public string Label { get; set; } = string.Empty;
}

[SemanticType]
public enum State { Active, Archived }

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class RuntimeContainer
{
    public Guid? OptionalId { get; set; }
    public State? State { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class TestDataScenario
{
    public List<string> Items { get; set; } = [];
    public State Status { get; set; }
    public Guid Id { get; set; }
}
