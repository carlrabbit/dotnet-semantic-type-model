using System.Linq.Expressions;

namespace SemanticTypeModel.EFCore;

/// <summary>Configures target-specific EF Core envelope payload storage policies.</summary>
public sealed class EfCoreEnvelopeProjectionOptions
{
    private readonly Dictionary<string, EfCoreEnvelopePayloadPolicy> _policies = new(StringComparer.Ordinal);

    /// <summary>Gets configured envelope storage policies keyed by envelope type name.</summary>
    public IReadOnlyDictionary<string, EfCoreEnvelopePayloadPolicy> Policies => _policies;

    /// <summary>Configures EF Core envelope storage policy for the specified CLR envelope type.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> For<TEnvelope>()
    {
        var name = typeof(TEnvelope).Name;
        if (!_policies.TryGetValue(name, out EfCoreEnvelopePayloadPolicy? policy))
        {
            policy = new EfCoreEnvelopePayloadPolicy { EnvelopeTypeName = name };
            _policies[name] = policy;
        }

        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }
}

/// <summary>Builds an EF Core envelope policy for one type.</summary>
public sealed class EfCoreEnvelopePolicyBuilder<TEnvelope>(EfCoreEnvelopePayloadPolicy policy)
{
    /// <summary>Projects the envelope as an EF Core entity.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> UseEnvelopeAsEntity()
    {
        policy.UseEnvelopeAsEntity = true;
        return this;
    }

    /// <summary>Selects the envelope payload member.</summary>
    public EfCoreEnvelopePayloadPolicyBuilder<TEnvelope> Payload<TPayload>(Expression<Func<TEnvelope, TPayload>> payloadSelector)
    {
        policy.PayloadPropertyName = MemberName(payloadSelector);
        return new EfCoreEnvelopePayloadPolicyBuilder<TEnvelope>(policy);
    }

    private static string MemberName<TPayload>(Expression<Func<TEnvelope, TPayload>> selector)
    {
        return selector.Body is MemberExpression member ? member.Member.Name : throw new ArgumentException("Payload selector must be a member access expression.", nameof(selector));
    }
}

/// <summary>Builds an EF Core payload storage policy for one envelope type.</summary>
public sealed class EfCoreEnvelopePayloadPolicyBuilder<TEnvelope>(EfCoreEnvelopePayloadPolicy policy)
{
    /// <summary>Stores the payload as serialized JSON in one scalar column.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> StoreAsSerializedJson(string? columnName = null)
    {
        policy.StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.SerializedJson;
        policy.ColumnName = columnName;
        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Stores the payload using EF Core owned JSON mapping metadata.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> StoreAsOwnedJson(string? columnName = null)
    {
        policy.StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.OwnedJson;
        policy.ColumnName = columnName;
        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Stores the payload as owned same-table columns using the supplied prefix.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> StoreAsOwnedColumns(string? prefix = null)
    {
        policy.StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.OwnedSameTable;
        policy.ColumnName = prefix;
        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Stores the payload as an owned separate-table aggregate.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> StoreAsOwnedSeparateTable(string? tableName = null)
    {
        policy.StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.OwnedSeparateTable;
        policy.ColumnName = tableName;
        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Ignores the payload in EF Core mapping.</summary>
    public EfCoreEnvelopePolicyBuilder<TEnvelope> IgnorePayload()
    {
        policy.StoragePolicy = EfCoreEnvelopePayloadStoragePolicy.Ignored;
        return new EfCoreEnvelopePolicyBuilder<TEnvelope>(policy);
    }
}

/// <summary>Represents an EF Core envelope payload storage policy.</summary>
public sealed record EfCoreEnvelopePayloadPolicy
{
    /// <summary>Gets the envelope type name.</summary>
    public required string EnvelopeTypeName { get; init; }

    /// <summary>Gets a value indicating whether the envelope should be projected as an entity.</summary>
    public bool UseEnvelopeAsEntity { get; set; }

    /// <summary>Gets the selected payload property name.</summary>
    public string? PayloadPropertyName { get; set; }

    /// <summary>Gets the selected storage policy.</summary>
    public EfCoreEnvelopePayloadStoragePolicy StoragePolicy { get; set; } = EfCoreEnvelopePayloadStoragePolicy.SerializedJson;

    /// <summary>Gets the configured column, prefix, or table name depending on storage policy.</summary>
    public string? ColumnName { get; set; }
}

/// <summary>Supported EF Core envelope payload storage policies.</summary>
public enum EfCoreEnvelopePayloadStoragePolicy
{
    /// <summary>Store payload as serialized JSON in one scalar column.</summary>
    SerializedJson,

    /// <summary>Store payload as owned JSON aggregate metadata.</summary>
    OwnedJson,

    /// <summary>Store owned payload scalar members as same-table columns.</summary>
    OwnedSameTable,

    /// <summary>Store owned payload aggregate in separate table metadata.</summary>
    OwnedSeparateTable,

    /// <summary>Ignore payload in EF Core mapping.</summary>
    Ignored,
}

/// <summary>Supported EF Core value-object storage policies.</summary>
public enum EfCoreValueObjectStoragePolicy
{
    /// <summary>Owned reference in same-table columns.</summary>
    OwnedReferenceSameTable,

    /// <summary>Owned reference JSON aggregate.</summary>
    OwnedReferenceJson,

    /// <summary>Owned reference separate table.</summary>
    OwnedReferenceSeparateTable,

    /// <summary>Owned collection JSON aggregate.</summary>
    OwnedCollectionJson,

    /// <summary>Owned collection separate table.</summary>
    OwnedCollectionSeparateTable,

    /// <summary>Serialized JSON scalar column.</summary>
    SerializedJson,

    /// <summary>Ignore the value-object member.</summary>
    Ignored,
}
