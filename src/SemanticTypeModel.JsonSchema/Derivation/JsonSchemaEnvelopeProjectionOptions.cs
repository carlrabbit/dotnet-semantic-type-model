using System.Linq.Expressions;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>Configures target-specific JSON Schema envelope projection policies.</summary>
public sealed class JsonSchemaEnvelopeProjectionOptions
{
    private readonly Dictionary<string, JsonSchemaEnvelopeProjectionPolicy> _policies = new(StringComparer.Ordinal);

    /// <summary>Gets configured policies keyed by envelope type name.</summary>
    public IReadOnlyDictionary<string, JsonSchemaEnvelopeProjectionPolicy> Policies => _policies;

    /// <summary>Configures a JSON Schema envelope projection policy for the specified CLR type.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> For<TEnvelope>()
    {
        var name = typeof(TEnvelope).Name;
        if (!_policies.TryGetValue(name, out JsonSchemaEnvelopeProjectionPolicy? policy))
        {
            policy = new JsonSchemaEnvelopeProjectionPolicy { EnvelopeTypeName = name };
            _policies[name] = policy;
        }

        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }
}

/// <summary>Builds a JSON Schema policy for one envelope type.</summary>
public sealed class JsonSchemaEnvelopePolicyBuilder<TEnvelope>(JsonSchemaEnvelopeProjectionPolicy policy)
{
    /// <summary>Projects the envelope object as the JSON Schema root.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> UseEnvelopeAsRoot()
    {
        policy.RootPolicy = JsonSchemaEnvelopeRootPolicy.EnvelopeAsRoot;
        return this;
    }

    /// <summary>Projects the payload object as the JSON Schema root.</summary>
    public JsonSchemaEnvelopePayloadPolicyBuilder<TEnvelope> UsePayloadAsRoot<TPayload>(Expression<Func<TEnvelope, TPayload>> payloadSelector)
    {
        policy.RootPolicy = policy.RootPolicy == JsonSchemaEnvelopeRootPolicy.EnvelopeAsRoot ? JsonSchemaEnvelopeRootPolicy.Ambiguous : JsonSchemaEnvelopeRootPolicy.PayloadAsRoot;
        policy.PayloadPropertyName = MemberName(payloadSelector);
        return new JsonSchemaEnvelopePayloadPolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Selects the envelope payload member for representation policy configuration.</summary>
    public JsonSchemaEnvelopePayloadPolicyBuilder<TEnvelope> Payload<TPayload>(Expression<Func<TEnvelope, TPayload>> payloadSelector)
    {
        policy.PayloadPropertyName = MemberName(payloadSelector);
        return new JsonSchemaEnvelopePayloadPolicyBuilder<TEnvelope>(policy);
    }

    private static string MemberName<TPayload>(Expression<Func<TEnvelope, TPayload>> selector)
    {
        return selector.Body is MemberExpression member ? member.Member.Name : throw new ArgumentException("Payload selector must be a member access expression.", nameof(selector));
    }
}

/// <summary>Builds a JSON Schema payload representation policy for one envelope type.</summary>
public sealed class JsonSchemaEnvelopePayloadPolicyBuilder<TEnvelope>(JsonSchemaEnvelopeProjectionPolicy policy)
{
    /// <summary>Represents the payload as a structured JSON Schema reference.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> RepresentAsStructuredReference()
    {
        policy.PayloadRepresentation = JsonSchemaEnvelopePayloadRepresentation.StructuredReference;
        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Represents the payload as an inline structured schema under the payload property.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> RepresentInline()
    {
        policy.PayloadRepresentation = JsonSchemaEnvelopePayloadRepresentation.Inline;
        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Represents the payload as an open JSON document.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> RepresentAsJsonDocument()
    {
        policy.PayloadRepresentation = JsonSchemaEnvelopePayloadRepresentation.JsonDocument;
        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Represents the payload as a serialized JSON string.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> RepresentAsSerializedJsonString()
    {
        policy.PayloadRepresentation = JsonSchemaEnvelopePayloadRepresentation.SerializedJsonString;
        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }

    /// <summary>Represents the payload as opaque JSON content without structural detail.</summary>
    public JsonSchemaEnvelopePolicyBuilder<TEnvelope> RepresentAsOpaquePayload()
    {
        policy.PayloadRepresentation = JsonSchemaEnvelopePayloadRepresentation.Opaque;
        return new JsonSchemaEnvelopePolicyBuilder<TEnvelope>(policy);
    }
}

/// <summary>Represents a configured JSON Schema envelope policy.</summary>
public sealed record JsonSchemaEnvelopeProjectionPolicy
{
    /// <summary>Gets the envelope type name.</summary>
    public required string EnvelopeTypeName { get; init; }

    /// <summary>Gets the selected root policy.</summary>
    public JsonSchemaEnvelopeRootPolicy RootPolicy { get; set; } = JsonSchemaEnvelopeRootPolicy.Unspecified;

    /// <summary>Gets the selected payload property name.</summary>
    public string? PayloadPropertyName { get; set; }

    /// <summary>Gets the selected payload representation.</summary>
    public JsonSchemaEnvelopePayloadRepresentation PayloadRepresentation { get; set; } = JsonSchemaEnvelopePayloadRepresentation.StructuredReference;
}

/// <summary>Supported JSON Schema envelope root policies.</summary>
public enum JsonSchemaEnvelopeRootPolicy
{
    /// <summary>Use target defaults.</summary>
    Unspecified,

    /// <summary>Project the envelope object as root.</summary>
    EnvelopeAsRoot,

    /// <summary>Project the payload object as root.</summary>
    PayloadAsRoot,

    /// <summary>Ambiguous envelope and payload root selection.</summary>
    Ambiguous,
}

/// <summary>Supported JSON Schema payload representation policies.</summary>
public enum JsonSchemaEnvelopePayloadRepresentation
{
    /// <summary>Structured payload schema by reference.</summary>
    StructuredReference,

    /// <summary>Structured payload schema inline under the payload property.</summary>
    Inline,

    /// <summary>Open JSON document payload.</summary>
    JsonDocument,

    /// <summary>Serialized JSON string payload.</summary>
    SerializedJsonString,

    /// <summary>Opaque payload with no structural details.</summary>
    Opaque,
}
