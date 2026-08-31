using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.TestModels.ModelB.Generated", "ModelBSemanticTypeModel", IncludeInternalTypes = true)]

namespace SemanticTypeModel.TestModels.ModelB;

[SemanticType(SemanticTypeRole.Entity)]
public abstract class BaseEntity
{
    public string Id { get; set; } = string.Empty;
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecialEntity : BaseEntity
{
    public OtherId OtherId { get; set; }
}

[SemanticStrongScalar]
public readonly record struct OtherId(Guid Value);

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Details
{
    public int Count { get; set; }
}
