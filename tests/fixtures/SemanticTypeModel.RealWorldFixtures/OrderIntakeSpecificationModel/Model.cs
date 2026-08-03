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
public abstract record ConfigurableSpecification : VersionedExtensibleObject
{
    [SemanticKey]
    public required Guid Id { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

[SemanticType(SemanticTypeRole.Entity)]
public sealed record OrderIntakeSpecification : ConfigurableSpecification, IConfigurationKind<OrderIntakeSpecification>
{
    public static string Kind => "order-intake";
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required PartnerDeliveryAgreement Delivery { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required OrderIntakeSchedule Schedule { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required SourcePollingPolicy Polling { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public DelimitedFileSource? DelimitedFile { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public StructuredFileSource? StructuredFile { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public PrimaryApiSource? PrimaryApi { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public SecondaryApiSource? SecondaryApi { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Object)] public required NormalizationPipeline Normalization { get; init; }
    [SemanticOwned(Kind = SemanticOwnershipKind.Collection)] public IReadOnlyList<DerivedOrderField> DerivedFields { get; init; } = [];
}

[SemanticType(SemanticTypeRole.ValueObject)] public sealed record PartnerDeliveryAgreement(string PartnerCode, Guid AgreementId);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record OrderIntakeSchedule(DateOnly StartDate, TimeOnly StartTime, TimeSpan Interval);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SourcePollingPolicy(TimeSpan Interval, DateTimeOffset? LastSuccessfulPoll);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record DelimitedFileSource(Uri Location, char Delimiter);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record StructuredFileSource(Uri Location, string RootElement);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record PrimaryApiSource(Uri Endpoint, string? Token);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record SecondaryApiSource(Uri Endpoint, string? Token);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record NormalizationPipeline(bool Enabled, string Mode);
[SemanticType(SemanticTypeRole.ValueObject)] public sealed record DerivedOrderField(string Name, [property: SemanticRequiredWhen("Name", "custom")] string? Expression);
