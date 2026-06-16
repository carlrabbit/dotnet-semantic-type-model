using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Annotations;

internal static class AnnotationKeyRules
{
    private static readonly Dictionary<string, string> ReservedNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        ["schema"] = "schema",
        ["jsonSchema"] = "jsonSchema",
        ["jsonEditor"] = "jsonEditor",
        ["dotnet"] = "dotnet",
        ["efCore"] = "efCore",
        ["powerBi"] = "powerBi",
        ["tom"] = "tom",
        ["ui"] = "ui",
    };

    public static AnnotationKeyValidationResult Validate(AnnotationKey key)
    {
        var value = key.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return AnnotationKeyValidationResult.Invalid(key, "Annotation keys must be non-empty namespaced values.");
        }

        var separatorIndex = value.IndexOf('.', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            return AnnotationKeyValidationResult.Invalid(key, "Annotation keys must use the form 'namespace.name'.");
        }

        var namespacePart = value[..separatorIndex];
        var localName = value[(separatorIndex + 1)..];

        if (!IsValidNamespace(namespacePart))
        {
            return AnnotationKeyValidationResult.Invalid(key, $"Annotation namespace '{namespacePart}' is malformed.");
        }

        if (string.IsNullOrWhiteSpace(localName) || localName.Any(char.IsWhiteSpace))
        {
            return AnnotationKeyValidationResult.Invalid(key, $"Annotation local name '{localName}' is malformed.");
        }

        if (ReservedNamespaces.TryGetValue(namespacePart, out var canonicalNamespace))
        {
            AnnotationKey normalizedKey = new($"{canonicalNamespace}.{localName}");
            return new AnnotationKeyValidationResult(true, normalizedKey, namespacePart != canonicalNamespace, namespacePart, localName, null);
        }

        return new AnnotationKeyValidationResult(true, key, false, namespacePart, localName, null);
    }

    public static bool IsReservedNamespace(string namespacePart)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespacePart);
        return ReservedNamespaces.ContainsKey(namespacePart);
    }

    private static bool IsValidNamespace(string namespacePart)
    {
        return char.IsLetter(namespacePart[0])
            && namespacePart.All(static character => char.IsLetterOrDigit(character));
    }
}

internal readonly record struct AnnotationKeyValidationResult(
    bool IsValid,
    AnnotationKey NormalizedKey,
    bool NamespaceCaseChanged,
    string Namespace,
    string LocalName,
    string? Error)
{
    public static AnnotationKeyValidationResult Invalid(AnnotationKey key, string error)
    {
        return new AnnotationKeyValidationResult(false, key, false, string.Empty, string.Empty, error);
    }
}
