using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.TestData;

#pragma warning disable CS1591, IDE0011, IDE0019, IDE0046, IDE0048, IDE0072, IDE0078, CA1859

public sealed record TerminologyProfileContext
{
    public string OwnerTypeId { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string PropertyId { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? UserDescription { get; init; }
    public string? TechnicalDescription { get; init; }
    public string? LogicalType { get; init; }
    public string ScalarTypeId { get; init; } = string.Empty;
    public ScalarKind ScalarKind { get; init; }
    public string? Format { get; init; }
    public string? Unit { get; init; }
    public bool IsRequired { get; init; }
    public bool AllowsNull { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
    public string? Pattern { get; init; }
    public decimal? Minimum { get; init; }
    public decimal? Maximum { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string? OwnerRole { get; init; }
}

public sealed record TerminologyLogicalTypeEntry
{
    public string Name { get; init; } = string.Empty;
    public string ScalarTypeId { get; init; } = string.Empty;
    public TerminologyProfileContext Context { get; init; } = new();
    public IReadOnlyList<JsonElement> Values { get; init; } = [];
}

public sealed record TerminologyPropertyEntry
{
    public string OwnerTypeId { get; init; } = string.Empty;
    public string PropertyId { get; init; } = string.Empty;
    public TerminologyProfileContext Context { get; init; } = new();
    public IReadOnlyList<JsonElement> Values { get; init; } = [];
}

public sealed record SemanticTerminologyProfile
{
    public const string CurrentFormat = "stm-testdata-terminology-profile";
    public const int CurrentFormatVersion = 1;
    public string Format { get; init; } = CurrentFormat;
    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string ModelId { get; init; } = string.Empty;
    public IReadOnlyList<string> Instructions { get; init; } = [];
    public IReadOnlyList<TerminologyLogicalTypeEntry> LogicalTypes { get; init; } = [];
    public IReadOnlyList<TerminologyPropertyEntry> Properties { get; init; } = [];

    internal IReadOnlyList<JsonElement> FindCandidates(ObjectTypeDefinition owner, PropertyDefinition property)
    {
        TerminologyPropertyEntry? specific = Properties.FirstOrDefault(p => p.OwnerTypeId == owner.Id.Value && p.PropertyId == property.Id.Value);
        if (specific is { Values.Count: > 0 })
        {
            return specific.Values;
        }

        var logical = property.Annotations.Items.FirstOrDefault(a => a.Key.Value == CoreSemanticAnnotationKeys.LogicalType)?.Value as string;
        return logical is null ? [] : LogicalTypes.FirstOrDefault(p => p.Name == logical)?.Values ?? [];
    }
}

public sealed record TerminologyProfileResult<T>(T? Profile, IReadOnlyList<SchemaDiagnostic> Diagnostics)
{
    public bool Succeeded => Profile is not null && Diagnostics.All(d => d.Severity != SchemaDiagnosticSeverity.Error);
}

public static class SemanticTerminologyProfileJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    static SemanticTerminologyProfileJson()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public static string Export(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return JsonSerializer.Serialize(Create(model), JsonOptions) + Environment.NewLine;
    }

    public static SemanticTerminologyProfile Create(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var instructions = new[]
        {
            "Populate candidate values only; do not change model, property, Logical Type, or semantic context.",
            "Prefer reusable Logical Type values when a Logical Type exists; use property values for use-site meaning.",
            "Use synthetic representative values only; never include secrets, production identifiers, or real personal/customer data.",
            "Keep values consistent with the supplied scalar shape, format, and constraints; leave values empty when uncertain.",
        };
        var logical = new List<TerminologyLogicalTypeEntry>();
        var properties = new List<TerminologyPropertyEntry>();
        foreach (ObjectTypeDefinition owner in model.Types.OfType<ObjectTypeDefinition>().OrderBy(t => t.Id.Value, StringComparer.Ordinal))
        {
            foreach (PropertyDefinition property in owner.Properties.OrderBy(p => p.Id.Value, StringComparer.Ordinal))
            {
                if (!model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? type) || type is not ScalarTypeDefinition scalar)
                {
                    continue;
                }

                TerminologyProfileContext context = Context(owner, property, scalar);
                properties.Add(new TerminologyPropertyEntry { OwnerTypeId = owner.Id.Value, PropertyId = property.Id.Value, Context = context });
                var logicalName = context.LogicalType;
                if (logicalName is not null && logical.All(entry => entry.Name != logicalName))
                {
                    logical.Add(new TerminologyLogicalTypeEntry { Name = logicalName, ScalarTypeId = scalar.Id.Value, Context = context });
                }
            }
        }

        return new SemanticTerminologyProfile { ModelId = model.Id.Value, Instructions = instructions, LogicalTypes = logical, Properties = properties };
    }

    internal static SemanticTerminologyProfile ValidateForConsumption(TypeSchemaModel model, SemanticTerminologyProfile profile)
    {
        TerminologyProfileResult<SemanticTerminologyProfile> result = Import(model, JsonSerializer.Serialize(profile, JsonOptions));
        if (!result.Succeeded)
        {
            throw new TestDataGenerationException("Terminology profile validation failed.", result.Diagnostics);
        }

        return result.Profile!;
    }

    public static TerminologyProfileResult<SemanticTerminologyProfile> Import(TypeSchemaModel model, string json)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(json);
        var diagnostics = new List<SchemaDiagnostic>();
        SemanticTerminologyProfile? profile;
        try
        {
            profile = JsonSerializer.Deserialize<SemanticTerminologyProfile>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            return new(null, [Error("TESTDATA_PROFILE_INVALID_JSON", exception.Message, "/")]);
        }

        if (profile is null)
        {
            return new(null, [Error("TESTDATA_PROFILE_INVALID", "Terminology profile is empty.", "/")]);
        }
        if (profile.Format != SemanticTerminologyProfile.CurrentFormat)
        {
            diagnostics.Add(Error("TESTDATA_PROFILE_FORMAT_UNSUPPORTED", "Terminology profile format is unsupported.", "/format"));
        }
        if (profile.FormatVersion != SemanticTerminologyProfile.CurrentFormatVersion)
        {
            diagnostics.Add(Error("TESTDATA_PROFILE_VERSION_UNSUPPORTED", "Terminology profile version is unsupported.", "/formatVersion"));
        }
        if (profile.ModelId != model.Id.Value)
        {
            diagnostics.Add(Error("TESTDATA_PROFILE_MODEL_MISMATCH", "Terminology profile modelId does not match the current model.", "/modelId"));
        }

        var normalizedLogical = new List<TerminologyLogicalTypeEntry>();
        var logicalIdentities = new HashSet<string>(StringComparer.Ordinal);
        foreach (TerminologyLogicalTypeEntry entry in profile.LogicalTypes.OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            var path = "/logicalTypes/" + entry.Name;
            if (!logicalIdentities.Add(entry.Name))
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_DUPLICATE", $"Logical Type '{entry.Name}' is declared more than once.", path));
                continue;
            }
            List<(ScalarTypeDefinition Scalar, PropertyDefinition Property)> matches = FindLogical(model, entry.Name);
            if (matches.Count == 0)
            {
                diagnostics.Add(Warning("TESTDATA_PROFILE_STALE", $"Logical Type '{entry.Name}' is not present in the current model and was ignored.", path));
                continue;
            }
            TypeId expected = matches[0].Scalar.Id;
            if (matches.Any(match => match.Scalar.Id != expected))
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_SCALAR_MISMATCH", $"Logical Type '{entry.Name}' is not consistent in the current model.", path));
            }
            if (entry.ScalarTypeId != expected.Value)
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_SCALAR_MISMATCH", $"Logical Type '{entry.Name}' records scalar '{entry.ScalarTypeId}' but the model uses '{expected.Value}'.", path));
            }
            IReadOnlyList<JsonElement> values = NormalizeValues(entry.Values, matches[0].Scalar, new ConstraintSet(), path, diagnostics);
            normalizedLogical.Add(entry with { ScalarTypeId = expected.Value, Values = values });
        }

        var normalizedProperties = new List<TerminologyPropertyEntry>();
        var propertyIdentities = new HashSet<string>(StringComparer.Ordinal);
        foreach (TerminologyPropertyEntry entry in profile.Properties.OrderBy(e => e.OwnerTypeId, StringComparer.Ordinal).ThenBy(e => e.PropertyId, StringComparer.Ordinal))
        {
            var path = $"/properties/{entry.OwnerTypeId}/{entry.PropertyId}";
            if (!propertyIdentities.Add(path))
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_DUPLICATE", "Property terminology is declared more than once.", path));
                continue;
            }
            if (!model.TypesById.TryGetValue(new TypeId(entry.OwnerTypeId), out TypeDefinition? ownerType) || ownerType is not ObjectTypeDefinition owner)
            {
                diagnostics.Add(Warning("TESTDATA_PROFILE_STALE", "Property terminology owner is not present in the current model and was ignored.", path));
                continue;
            }
            PropertyDefinition? property = owner.Properties.FirstOrDefault(p => p.Id.Value == entry.PropertyId);
            if (property is null)
            {
                diagnostics.Add(Warning("TESTDATA_PROFILE_STALE", "Property terminology is not present in the current model and was ignored.", path));
                continue;
            }
            if (!model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? type) || type is not ScalarTypeDefinition scalar)
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_NON_SCALAR", "Property terminology must target an ordinary scalar property.", path));
                continue;
            }
            normalizedProperties.Add(entry with { Values = NormalizeValues(entry.Values, scalar, property.Constraints, path, diagnostics) });
        }

        if (diagnostics.Any(d => d.Severity == SchemaDiagnosticSeverity.Error))
        {
            return new(null, diagnostics);
        }
        return new(profile with { LogicalTypes = normalizedLogical, Properties = normalizedProperties }, diagnostics);
    }

    private static List<(ScalarTypeDefinition Scalar, PropertyDefinition Property)> FindLogical(TypeSchemaModel model, string name)
    {
        var matches = new List<(ScalarTypeDefinition, PropertyDefinition)>();
        foreach (ObjectTypeDefinition owner in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (PropertyDefinition property in owner.Properties)
            {
                if (property.Annotations.Items.Any(a => a.Key.Value == CoreSemanticAnnotationKeys.LogicalType && a.Value as string == name)
                    && model.TypesById.TryGetValue(property.Type.Id, out TypeDefinition? type) && type is ScalarTypeDefinition scalar)
                {
                    matches.Add((scalar, property));
                }
            }
        }
        return matches;
    }

    private static IReadOnlyList<JsonElement> NormalizeValues(IReadOnlyList<JsonElement> values, ScalarTypeDefinition scalar, ConstraintSet constraints, string path, List<SchemaDiagnostic> diagnostics)
    {
        var result = new List<JsonElement>();
        foreach (JsonElement value in values)
        {
            if (!TerminologyCandidate.TryRead(value, scalar, constraints, out _, out var error))
            {
                diagnostics.Add(Error("TESTDATA_PROFILE_CANDIDATE_INVALID", error ?? "Terminology candidate is invalid.", path));
                continue;
            }
            if (result.All(existing => existing.GetRawText() != value.GetRawText()))
            {
                result.Add(value.Clone());
            }
        }
        result.Sort(static (left, right) => string.CompareOrdinal(left.GetRawText(), right.GetRawText()));
        return result;
    }

    private static TerminologyProfileContext Context(ObjectTypeDefinition owner, PropertyDefinition property, ScalarTypeDefinition scalar)
    {
        var logical = property.Annotations.Items.FirstOrDefault(a => a.Key.Value == CoreSemanticAnnotationKeys.LogicalType)?.Value as string;
        NumericConstraints? numeric = property.Constraints.Numeric;
        StringConstraints? text = property.Constraints.String;
        return new() { OwnerTypeId = owner.Id.Value, OwnerName = owner.Name, PropertyId = property.Id.Value, PropertyName = property.Name, DisplayName = property.DisplayName, UserDescription = property.UserDescription, TechnicalDescription = property.TechnicalDescription, LogicalType = logical, ScalarTypeId = scalar.Id.Value, ScalarKind = scalar.ScalarKind, Format = scalar.Format, Unit = scalar.Unit, IsRequired = property.Cardinality.IsRequired, AllowsNull = property.Cardinality.AllowsNull, MinLength = text?.MinLength, MaxLength = text?.MaxLength, Pattern = text?.Pattern, Minimum = numeric?.Minimum, Maximum = numeric?.Maximum, IsPrimaryKey = owner.Keys.Any(k => k.Properties.Any(p => p.Id == property.Id)), OwnerRole = owner.Semantics.Role.ToString() };
    }

    private static SchemaDiagnostic Error(string code, string message, string path)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Error, Code = code, Message = message, Stage = SchemaDiagnosticStage.Validation, ModelPath = path, PipelineStage = "TestData" };
    }

    private static SchemaDiagnostic Warning(string code, string message, string path)
    {
        return new() { Severity = SchemaDiagnosticSeverity.Warning, Code = code, Message = message, Stage = SchemaDiagnosticStage.Validation, ModelPath = path, PipelineStage = "TestData" };
    }
}

internal static class TerminologyCandidate
{
    internal static bool TryRead(JsonElement value, ScalarTypeDefinition scalar, ConstraintSet constraints, out object? result, out string? error)
    {
        result = null; error = null;
        if (value.ValueKind == JsonValueKind.Null)
        {
            error = "Terminology candidates cannot be null."; return false;
        }
        try
        {
            result = scalar.ScalarKind switch
            {
                ScalarKind.Boolean when value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False => value.GetBoolean(),
                ScalarKind.String when value.ValueKind == JsonValueKind.String => value.GetString()!,
                ScalarKind.Guid when value.ValueKind == JsonValueKind.String => Guid.Parse(value.GetString()!),
                ScalarKind.Date when value.ValueKind == JsonValueKind.String => DateOnly.Parse(value.GetString()!, CultureInfo.InvariantCulture),
                ScalarKind.Time when value.ValueKind == JsonValueKind.String => TimeOnly.Parse(value.GetString()!, CultureInfo.InvariantCulture),
                ScalarKind.DateTime when value.ValueKind == JsonValueKind.String => DateTime.Parse(value.GetString()!, CultureInfo.InvariantCulture),
                ScalarKind.DateTimeOffset when value.ValueKind == JsonValueKind.String => DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture),
                ScalarKind.Duration when value.ValueKind == JsonValueKind.String => TimeSpan.Parse(value.GetString()!, CultureInfo.InvariantCulture),
                ScalarKind.Binary when value.ValueKind == JsonValueKind.String => Convert.FromBase64String(value.GetString()!),
                ScalarKind.Integer or ScalarKind.Number or ScalarKind.Decimal when value.ValueKind == JsonValueKind.Number => value.GetDecimal(),
                ScalarKind.Json => value.Clone(),
                _ => throw new FormatException($"Candidate representation does not match scalar kind '{scalar.ScalarKind}'.")
            };
            if (result is string text)
            {
                StringConstraints c = constraints.String ?? new();
                if (c.MinLength is int min && text.Length < min || c.MaxLength is int max && text.Length > max)
                    throw new FormatException("Candidate violates string length constraints.");
                if (c.Pattern is { Length: > 0 } pattern && !Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant))
                    throw new FormatException("Candidate violates the string pattern constraint.");
                if (scalar.Format is { } format && !FormatValid(format, text))
                    throw new FormatException($"Candidate violates format '{format}'.");
            }
            if (result is decimal number && !NumericValid(number, constraints.Numeric))
                throw new FormatException("Candidate violates numeric constraints.");
            return true;
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException or JsonException)
        {
            error = exception.Message; return false;
        }
    }

    private static bool NumericValid(decimal value, NumericConstraints? c)
    {
        return c is null || (!c.Minimum.HasValue || (c.ExclusiveMinimum ? value > c.Minimum : value >= c.Minimum)) && (!c.Maximum.HasValue || (c.ExclusiveMaximum ? value < c.Maximum : value <= c.Maximum)) && (!c.MultipleOf.HasValue || c.MultipleOf == 0 || value % c.MultipleOf == 0);
    }

    private static bool FormatValid(string format, string value)
    {
        return format.ToLowerInvariant() switch
        {
            "email" => IsEmail(value),
            "uri-reference" => Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out _),
            "uri" => Uri.TryCreate(value, UriKind.Absolute, out _),
            "hostname" => IsHostname(value),
            "ipv4" => IsIp(value, System.Net.Sockets.AddressFamily.InterNetwork),
            "ipv6" => IsIp(value, System.Net.Sockets.AddressFamily.InterNetworkV6),
            "date" => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "time" => DateTimeOffset.TryParseExact(value, ["HH:mm:ss'Z'", "HH:mm:ss.FFFFFFF'Z'", "HH:mm:sszzz", "HH:mm:ss.FFFFFFFzzz"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "date-time" => DateTimeOffset.TryParseExact(value, ["yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mm:sszzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"], CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "duration" => IsDuration(value),
            "uuid" => !string.IsNullOrWhiteSpace(value) && !value.Any(char.IsWhiteSpace) && Guid.TryParse(value, out _),
            _ => false,
        };
    }

    private static bool IsEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace) || value.Count(c => c == '@') != 1)
        {
            return false;
        }

        var at = value.IndexOf('@');
        if (at <= 0 || at == value.Length - 1)
        {
            return false;
        }

        try
        {
            var address = new MailAddress(value);
            return string.Equals(address.Address, value, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsIp(string value, System.Net.Sockets.AddressFamily family)
    {
        return IPAddress.TryParse(value, out IPAddress? address) && address.AddressFamily == family;
    }

    private static bool IsHostname(string value)
    {
        if (value.Length is 0 or > 253 || value.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return value.Split('.').All(label => label.Length is > 0 and <= 63
            && label[0] != '-'
            && label[^1] != '-'
            && label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'));
    }

    private static bool IsDuration(string value)
    {
        try { _ = XmlConvert.ToTimeSpan(value); return true; }
        catch (FormatException) { return false; }
    }
}

#pragma warning restore CS1591, IDE0011, IDE0019, IDE0046, IDE0048, IDE0072, IDE0078, CA1859
