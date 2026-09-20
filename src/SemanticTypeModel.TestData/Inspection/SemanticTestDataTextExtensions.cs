#pragma warning disable IDE0046, IDE0058, IDE0072
using System.Globalization;
using System.Text;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.TestData.Inspection;

/// <summary>Provides deterministic human-readable inspection for semantic TestData graphs.</summary>
public static class SemanticTestDataTextExtensions
{
    /// <summary>
    /// Renders a semantic TestData graph using the supplied canonical model for type and property identities.
    /// The result is development/test text, not a serialization or persistence format.
    /// </summary>
    public static string ToSemanticText(this SemanticTestValue value, TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        AppendValue(builder, model, value, string.Empty);
        return builder.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    private static void AppendValue(StringBuilder builder, TypeSchemaModel model, SemanticTestValue value, string indent)
    {
        TypeDefinition type = model.TryGetType(value.TypeId)
            ?? throw new InvalidOperationException($"Semantic TestData type '{value.TypeId.Value}' was not found in the supplied model.");

        switch (value)
        {
            case ScalarTestValue scalar:
                if (type.Kind is not (TypeKind.Scalar or TypeKind.Any))
                {
                    EnsureType(type, TypeKind.Scalar, value);
                }
                builder.Append(type.Id.Value).Append(" (").Append(scalar.ScalarKind).Append("): ");
                builder.AppendLine(FormatScalar(scalar));
                break;
            case EnumTestValue @enum:
                if (type is not EnumTypeDefinition enumType)
                {
                    throw new InvalidOperationException($"Semantic TestData enum '{value.TypeId.Value}' does not match the supplied model.");
                }

                builder.Append(enumType.Id.Value).Append(" (Enum): ")
                    .AppendLine(FormatInvariant(@enum.Value));
                break;
            case NullTestValue:
                builder.Append(value.TypeId.Value).AppendLine(" (null): null");
                break;
            case ObjectTestValue obj:
                if (type is not ObjectTypeDefinition objectType)
                {
                    throw new InvalidOperationException($"Semantic TestData object '{value.TypeId.Value}' does not match the supplied model.");
                }

                builder.Append(objectType.Id.Value).AppendLine(" (Object):");
                foreach (KeyValuePair<PropertyId, SemanticTestValue> property in obj.Properties.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
                {
                    PropertyDefinition definition = FindProperty(model, objectType, property.Key);
                    builder.Append(indent).Append("  ").Append(definition.Name).Append(" [")
                        .Append(definition.Id.Value).AppendLine("]:");
                    AppendValue(builder, model, property.Value, indent + "  ");
                }
                break;
            case ArrayTestValue array:
                EnsureType(type, TypeKind.Array, value);
                builder.Append(array.TypeId.Value).AppendLine(" (Array):");
                for (var index = 0; index < array.Items.Count; index++)
                {
                    builder.Append(indent).Append("  [").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine("]:");
                    AppendValue(builder, model, array.Items[index], indent + "  ");
                }
                break;
            case DictionaryTestValue dictionary:
                EnsureType(type, TypeKind.Dictionary, value);
                builder.Append(dictionary.TypeId.Value).AppendLine(" (Dictionary):");
                for (var index = 0; index < dictionary.Entries.Count; index++)
                {
                    KeyValuePair<SemanticTestValue, SemanticTestValue> entry = dictionary.Entries[index];
                    builder.Append(indent).Append("  entry ").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(" key:");
                    AppendValue(builder, model, entry.Key, indent + "  ");
                    builder.Append(indent).Append("  entry ").Append(index.ToString(CultureInfo.InvariantCulture)).AppendLine(" value:");
                    AppendValue(builder, model, entry.Value, indent + "  ");
                }
                break;
            default:
                throw new InvalidOperationException($"Semantic TestData value node '{value.GetType().Name}' is unsupported by inspection.");
        }
    }

    private static PropertyDefinition FindProperty(TypeSchemaModel model, ObjectTypeDefinition owner, PropertyId id)
    {
        PropertyDefinition? property = owner.Properties.FirstOrDefault(candidate => candidate.Id == id);
        if (property is not null)
        {
            return property;
        }

        foreach (TypeRef baseRef in owner.Composition.AllOf)
        {
            if (model.TryGetType(baseRef.Id) is ObjectTypeDefinition baseObject)
            {
                try
                {
                    return FindProperty(model, baseObject, id);
                }
                catch (InvalidOperationException)
                {
                    // Continue searching the remaining composed objects.
                }
            }
        }

        throw new InvalidOperationException($"Semantic TestData property '{id.Value}' was not found on canonical object '{owner.Id.Value}'.");
    }

    private static void EnsureType(TypeDefinition actual, TypeKind expected, SemanticTestValue value)
    {
        if (actual.Kind != expected)
        {
            throw new InvalidOperationException($"Semantic TestData value '{value.TypeId.Value}' is a {value.GetType().Name}, but the supplied model declares '{actual.Kind}'.");
        }
    }

    private static string FormatScalar(ScalarTestValue scalar)
    {
        if (scalar.Value is null)
        {
            return "null";
        }

        return scalar.ScalarKind switch
        {
            ScalarKind.String => JsonSerializer.Serialize(Convert.ToString(scalar.Value, CultureInfo.InvariantCulture)),
            ScalarKind.Boolean => Convert.ToBoolean(scalar.Value, CultureInfo.InvariantCulture).ToString().ToLowerInvariant(),
            ScalarKind.Integer or ScalarKind.Number or ScalarKind.Decimal => FormatInvariant(scalar.Value),
            ScalarKind.Date => scalar.Value is DateOnly date ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.Time => scalar.Value is TimeOnly time ? time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.DateTime => scalar.Value is DateTime dateTime ? dateTime.ToString("O", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.DateTimeOffset => scalar.Value is DateTimeOffset offset ? offset.ToString("O", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.Duration => scalar.Value is TimeSpan duration ? duration.ToString("c", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.Guid => scalar.Value is Guid guid ? guid.ToString("D", CultureInfo.InvariantCulture) : FormatInvariant(scalar.Value),
            ScalarKind.Binary => scalar.Value is byte[] bytes ? Convert.ToBase64String(bytes) : FormatInvariant(scalar.Value),
            ScalarKind.Json => scalar.Value is JsonElement element ? element.GetRawText() : Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) ?? "null",
            _ => FormatInvariant(scalar.Value),
        };
    }

    private static string FormatInvariant(object value)
    {
        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
        };
    }
}
#pragma warning restore IDE0046, IDE0058, IDE0072
