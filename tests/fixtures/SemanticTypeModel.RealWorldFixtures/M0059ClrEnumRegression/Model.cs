using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.RealWorldFixtures.M0059ClrEnumRegression;

public enum ImportSourceKind { CsvFile, XmlFile }

[SemanticType(SemanticTypeRole.Entity)]
public sealed record ImportJob
{
    [SemanticKey] public required Guid Id { get; init; }
    public required ImportSourceKind SourceKind { get; init; }
    public ImportSourceKind? OptionalSourceKind { get; init; }

    [SemanticOwned(Kind = SemanticOwnershipKind.Object)]
    [SemanticRequiredWhen(nameof(SourceKind), nameof(ImportSourceKind.CsvFile))]
    public CsvSource? CsvSource { get; init; }
}

[SemanticType(SemanticTypeRole.ValueObject)]
public sealed record CsvSource(string Location, string Delimiter);
