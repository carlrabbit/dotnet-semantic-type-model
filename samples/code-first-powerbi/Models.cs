using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.Samples.CodeFirstPowerBi;

// A fact-like domain object is enough to demonstrate local Power BI metadata projection.
[SemanticType]
[SemanticName("SalesRecord")]
public sealed class SalesRecord
{
    [SemanticKey]
    public int SalesKey { get; init; }

    public decimal Amount { get; init; }
}
