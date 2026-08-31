using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.TestModels.ModelA.M0059;

[SemanticType] public enum ImportSourceKind { Csv, Xml }
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record CsvSource(string Location, string Delimiter);

[SemanticType(SemanticTypeRole.Entity)]
public sealed record ImportJob
{
    [SemanticKey] public required Guid Id { get; init; }
    public required ImportSourceKind SourceKind { get; init; }
    public ImportSourceKind? OptionalSourceKind { get; init; }
    [SemanticOwned] public CsvSource? CsvSource { get; init; }
}
