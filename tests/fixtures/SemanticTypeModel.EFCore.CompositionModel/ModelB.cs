using SemanticTypeModel.DotNet;

[assembly: SemanticTypeModelGeneratorOptions("SemanticTypeModel.EFCore.CompositionModel.Generated", "BillingSemanticTypeModel")]

namespace SemanticTypeModel.EFCore.CompositionModel;

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

// Deliberately outside the semantic model. The persistence project may still own it as an ordinary EF entity.
public sealed class ModelBExternalEntity
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
}

// Deliberately outside both models and EF; referencing the assembly must not discover this type.
public sealed class ModelBIgnoredPoco { public int Id { get; set; } }
