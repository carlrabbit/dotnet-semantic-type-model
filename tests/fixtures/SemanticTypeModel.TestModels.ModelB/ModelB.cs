using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.TestModels.ModelB.Generated", "ModelBSemanticTypeModel", IncludeInternalTypes = true)]

namespace SemanticTypeModel.TestModels.ModelB;

[SemanticType(SemanticTypeRole.Entity)]
public abstract class BaseEntity
{
    [SemanticKey]
    public string Id { get; set; } = string.Empty;
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecialEntity : BaseEntity
{
    public Guid OtherId { get; set; }
    public State? State { get; set; }
    [SemanticOwned] public Details? Details { get; set; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed class Details
{
    public int Count { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public class BillingRecord
{
    [SemanticKey]
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed class SpecializedBillingRecord : BillingRecord
{
    public string Reference { get; set; } = string.Empty;
}

[SemanticType]
public enum State { Active, Archived }
