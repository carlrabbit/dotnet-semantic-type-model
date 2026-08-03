using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticTypeModel.DotNet;

#pragma warning disable CS1591

namespace SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;

public interface IConfigurationKind<TSelf> where TSelf : IConfigurationKind<TSelf>
{
    static abstract string Kind { get; }
}

public abstract record VersionedExtensibleObject
{
    [SemanticVersion]
    public required int SchemaVersion { get; init; }

    [JsonExtensionData, SemanticExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

[SemanticType(SemanticTypeRole.Entity)]
public abstract record Specification : VersionedExtensibleObject
{
    [SemanticKey]
    public required Guid Id { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed record WorkflowSpecification : Specification;

[SemanticType(SemanticTypeRole.Entity)]
public sealed record ImportSpecification : Specification, IConfigurationKind<ImportSpecification>
{
    public static string Kind => "order-intake";
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required DeliveryContract DeliveryContract { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required ScheduleContract Schedule { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required SourcePollingPolicy Polling { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public CsvSourceSpecification? CsvSource { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public XmlSourceSpecification? XmlSource { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public PrimaryApiSource? PrimaryApiSource { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public SecondaryApiSource? SecondaryApiSource { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required PostProcessingContract PostProcessing { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<DerivedProperty> DerivedProperties { get; init; } = [];
}

[SemanticType(SemanticTypeRole.ValueObject)] public sealed record DeliveryContract(string PartnerCode, Guid AgreementId);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record ScheduleContract(DateOnly StartDate, TimeOnly StartTime, TimeSpan Interval);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SourcePollingPolicy(TimeSpan Interval, DateTimeOffset? LastSuccessfulPoll);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record CsvSourceSpecification(Uri Location, char Delimiter);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record XmlSourceSpecification(Uri Location, string RootElement);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record PrimaryApiSource(Uri Endpoint, string? Token);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SecondaryApiSource(Uri Endpoint, string? Token);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record PostProcessingContract(bool Enabled, string Mode);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record DerivedProperty(string Name, [property: SemanticRequiredWhen("Name", "custom")] string? Expression);
