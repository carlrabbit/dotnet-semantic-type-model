using System.Linq.Expressions;

namespace SemanticTypeModel.PowerBI;

/// <summary>Configures Power BI envelope analytical projection policies.</summary>
public sealed class PowerBiEnvelopeProjectionOptions
{
    private readonly Dictionary<string, PowerBiEnvelopeProjectionPolicy> _policies = new(StringComparer.Ordinal);

    /// <summary>Gets configured policies keyed by envelope type name.</summary>
    public IReadOnlyDictionary<string, PowerBiEnvelopeProjectionPolicy> Policies => _policies;

    /// <summary>Configures an envelope policy for the specified CLR type.</summary>
    public PowerBiEnvelopePolicyBuilder<TEnvelope> For<TEnvelope>()
    {
        var name = typeof(TEnvelope).Name;
        if (!_policies.TryGetValue(name, out PowerBiEnvelopeProjectionPolicy? policy))
        {
            policy = new PowerBiEnvelopeProjectionPolicy { EnvelopeTypeName = name };
            _policies[name] = policy;
        }

        return new PowerBiEnvelopePolicyBuilder<TEnvelope>(policy);
    }
}

/// <summary>Builds Power BI envelope policies.</summary>
public sealed class PowerBiEnvelopePolicyBuilder<TEnvelope>(PowerBiEnvelopeProjectionPolicy policy)
{
    /// <summary>Projects envelope metadata as the analytical table.</summary>
    public PowerBiEnvelopePolicyBuilder<TEnvelope> UseEnvelopeMetadataTable()
    {
        policy.Policy = PowerBiEnvelopeAnalyticalPolicy.MetadataTable;
        return this;
    }

    /// <summary>Ignores payload body columns.</summary>
    public PowerBiEnvelopePolicyBuilder<TEnvelope> IgnorePayloadBody()
    {
        policy.PayloadPolicy = PowerBiEnvelopePayloadAnalyticalPolicy.Ignored;
        return this;
    }

    /// <summary>Exposes a deterministic payload summary column.</summary>
    public PowerBiEnvelopePolicyBuilder<TEnvelope> SummarizePayload<TPayload>(Expression<Func<TEnvelope, TPayload>> payloadSelector, string? columnName = null)
    {
        policy.PayloadPropertyName = MemberName(payloadSelector);
        policy.PayloadPolicy = PowerBiEnvelopePayloadAnalyticalPolicy.Summary;
        policy.SummaryColumnName = columnName;
        return this;
    }

    private static string MemberName<TPayload>(Expression<Func<TEnvelope, TPayload>> selector)
    {
        return selector.Body is MemberExpression member ? member.Member.Name : throw new ArgumentException("Payload selector must be a member access expression.", nameof(selector));
    }
}

/// <summary>Represents Power BI envelope analytical policy.</summary>
public sealed record PowerBiEnvelopeProjectionPolicy
{
    /// <summary>Gets the envelope type name.</summary>
    public required string EnvelopeTypeName { get; init; }

    /// <summary>Gets the envelope projection policy.</summary>
    public PowerBiEnvelopeAnalyticalPolicy Policy { get; set; } = PowerBiEnvelopeAnalyticalPolicy.MetadataTable;

    /// <summary>Gets the payload property name.</summary>
    public string? PayloadPropertyName { get; set; }

    /// <summary>Gets the payload analytical policy.</summary>
    public PowerBiEnvelopePayloadAnalyticalPolicy PayloadPolicy { get; set; } = PowerBiEnvelopePayloadAnalyticalPolicy.Ignored;

    /// <summary>Gets the payload summary column name.</summary>
    public string? SummaryColumnName { get; set; }
}

/// <summary>Supported Power BI envelope analytical policies.</summary>
public enum PowerBiEnvelopeAnalyticalPolicy
{
    /// <summary>Project envelope metadata as a table.</summary>
    MetadataTable,

    /// <summary>Project payload as a table.</summary>
    PayloadTable,

    /// <summary>Project both envelope and payload when explicit linking exists.</summary>
    EnvelopeAndPayload,
}

/// <summary>Supported Power BI payload analytical policies.</summary>
public enum PowerBiEnvelopePayloadAnalyticalPolicy
{
    /// <summary>Ignore payload body.</summary>
    Ignored,

    /// <summary>Expose deterministic summary information.</summary>
    Summary,

    /// <summary>Flatten explicitly selected payload members.</summary>
    Flattened,
}
