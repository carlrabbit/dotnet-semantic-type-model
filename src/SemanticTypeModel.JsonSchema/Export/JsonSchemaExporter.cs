using System.Globalization;
using System.Text;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Contracts;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.JsonSchema.Export;

/// <summary>
/// Projects a canonical <see cref="TypeSchemaModel"/> to a JSON Schema Draft 2020-12 document string.
/// Named shapes defined in the model are exported under <c>$defs</c>.
/// The root shape is emitted at the schema root.
/// </summary>
public sealed class JsonSchemaExporter : ISchemaProjection<string>
{
    /// <summary>
    /// Projects the canonical model to a JSON Schema Draft 2020-12 string.
    /// </summary>
    /// <param name="model">The canonical model to export.</param>
    /// <returns>A JSON Schema Draft 2020-12 document as a JSON string.</returns>
    public string Project(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("$schema", "https://json-schema.org/draft/2020-12/schema");

        if (model.Root is not null)
        {
            WriteShapeProperties(writer, model.Root);
        }

        var definitions = model.Shapes
            .Where(static pair => pair.Key != "#")
            .Where(pair => pair.Key != model.RootIdentifier)
            .ToList();

        if (definitions.Count > 0)
        {
            writer.WritePropertyName("$defs");
            writer.WriteStartObject();

            foreach ((var name, TypeShape? shape) in definitions)
            {
                writer.WritePropertyName(name);
                writer.WriteStartObject();
                WriteShapeProperties(writer, shape);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteShapeProperties(Utf8JsonWriter writer, TypeShape shape)
    {
        WriteAnnotations(writer, shape.Annotations);
        WriteConstraints(writer, shape.Constraints);

        switch (shape)
        {
            case ObjectShape obj:
                WriteObjectShape(writer, obj);
                break;
            case ScalarShape scalar:
                WriteScalarShape(writer, scalar);
                break;
            case EnumShape enumShape:
                WriteEnumShape(writer, enumShape);
                break;
            case ArrayShape array:
                WriteArrayShape(writer, array);
                break;
            case DictionaryShape dictionary:
                WriteDictionaryShape(writer, dictionary);
                break;
            case UnionShape union when TryWriteRefWrapper(writer, union):
                break;
            case UnionShape union:
                WriteUnionShape(writer, union);
                break;
            default:
                break;
        }
    }

    private static bool TryWriteRefWrapper(Utf8JsonWriter writer, UnionShape union)
    {
        if (union.Options.Count != 1)
        {
            return false;
        }

        WriteShapeRefBody(writer, union.Options[0]);
        return true;
    }

    private static void WriteObjectShape(Utf8JsonWriter writer, ObjectShape obj)
    {
        writer.WriteString("type", "object");

        if (obj.Properties.Count > 0)
        {
            writer.WritePropertyName("properties");
            writer.WriteStartObject();

            foreach (PropertyShape property in obj.Properties)
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartObject();
                WritePropertyShapeBody(writer, property);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();

            var required = obj.Properties.Where(static property => property.IsRequired).Select(static property => property.Name).ToList();
            if (required.Count > 0)
            {
                writer.WritePropertyName("required");
                writer.WriteStartArray();
                foreach (var name in required)
                {
                    writer.WriteStringValue(name);
                }

                writer.WriteEndArray();
            }
        }

        if (!obj.AdditionalPropertiesAllowed)
        {
            writer.WriteBoolean("additionalProperties", false);
        }
    }

    private static void WritePropertyShapeBody(Utf8JsonWriter writer, PropertyShape property)
    {
        WriteAnnotations(writer, property.Annotations);

        if (property.Type is null)
        {
            return;
        }

        if (property.IsNullable)
        {
            writer.WritePropertyName("oneOf");
            writer.WriteStartArray();

            writer.WriteStartObject();
            WriteShapeRefBody(writer, property.Type);
            writer.WriteEndObject();

            writer.WriteStartObject();
            writer.WriteString("type", "null");
            writer.WriteEndObject();

            writer.WriteEndArray();
            return;
        }

        WriteShapeRefBody(writer, property.Type);
    }

    private static void WriteScalarShape(Utf8JsonWriter writer, ScalarShape scalar)
    {
        if (scalar.IsNullable && scalar.Kind != ScalarKind.Null)
        {
            writer.WritePropertyName("oneOf");
            writer.WriteStartArray();

            writer.WriteStartObject();
            writer.WriteString("type", GetScalarTypeName(scalar.Kind));
            writer.WriteEndObject();

            writer.WriteStartObject();
            writer.WriteString("type", "null");
            writer.WriteEndObject();

            writer.WriteEndArray();
            return;
        }

        writer.WriteString("type", GetScalarTypeName(scalar.Kind));
    }

    private static void WriteEnumShape(Utf8JsonWriter writer, EnumShape enumShape)
    {
        writer.WritePropertyName("enum");
        writer.WriteStartArray();
        foreach (var value in enumShape.Values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteArrayShape(Utf8JsonWriter writer, ArrayShape array)
    {
        writer.WriteString("type", "array");

        if (array.Items is null)
        {
            return;
        }

        writer.WritePropertyName("items");
        writer.WriteStartObject();
        WriteShapeRefBody(writer, array.Items);
        writer.WriteEndObject();
    }

    private static void WriteDictionaryShape(Utf8JsonWriter writer, DictionaryShape dictionary)
    {
        writer.WriteString("type", "object");

        if (dictionary.Values is null)
        {
            return;
        }

        writer.WritePropertyName("additionalProperties");
        writer.WriteStartObject();
        WriteShapeRefBody(writer, dictionary.Values);
        writer.WriteEndObject();
    }

    private static void WriteUnionShape(Utf8JsonWriter writer, UnionShape union)
    {
        writer.WritePropertyName("oneOf");
        writer.WriteStartArray();

        foreach (ShapeRef option in union.Options)
        {
            writer.WriteStartObject();
            WriteShapeRefBody(writer, option);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteShapeRefBody(Utf8JsonWriter writer, ShapeRef shapeRef)
    {
        if (shapeRef.IsRef)
        {
            writer.WriteString("$ref", shapeRef.Identifier == "#" ? "#" : $"#/$defs/{shapeRef.Identifier}");
            return;
        }

        if (shapeRef.Inline is not null)
        {
            WriteShapeProperties(writer, shapeRef.Inline);
        }
    }

    private static void WriteAnnotations(Utf8JsonWriter writer, IReadOnlyList<SchemaAnnotation> annotations)
    {
        foreach (SchemaAnnotation annotation in annotations)
        {
            switch (annotation.Key)
            {
                case "title":
                    writer.WriteString("title", annotation.Value);
                    break;
                case "description":
                    writer.WriteString("description", annotation.Value);
                    break;
                case "default":
                    writer.WritePropertyName("default");
                    using (var document = JsonDocument.Parse(annotation.Value))
                    {
                        document.RootElement.WriteTo(writer);
                    }
                    break;
                case "examples":
                    writer.WritePropertyName("examples");
                    using (var document = JsonDocument.Parse(annotation.Value))
                    {
                        document.RootElement.WriteTo(writer);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private static void WriteConstraints(Utf8JsonWriter writer, ConstraintSet constraints)
    {
        foreach (ConstraintEntry entry in constraints.Entries)
        {
            switch (entry.Key)
            {
                case "minLength":
                case "maxLength":
                case "minimum":
                case "maximum":
                case "exclusiveMinimum":
                case "exclusiveMaximum":
                case "multipleOf":
                case "minItems":
                case "maxItems":
                case "minProperties":
                case "maxProperties":
                    if (long.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    {
                        writer.WriteNumber(entry.Key, longValue);
                    }
                    else if (double.TryParse(entry.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        writer.WriteNumber(entry.Key, doubleValue);
                    }
                    break;
                case "pattern":
                case "format":
                    writer.WriteString(entry.Key, entry.Value);
                    break;
                case "uniqueItems":
                    if (bool.TryParse(entry.Value, out var boolValue))
                    {
                        writer.WriteBoolean(entry.Key, boolValue);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private static string GetScalarTypeName(ScalarKind kind)
    {
        return kind switch
        {
            ScalarKind.String => "string",
            ScalarKind.Integer => "integer",
            ScalarKind.Number => "number",
            ScalarKind.Boolean => "boolean",
            ScalarKind.Null => "null",
            _ => "string",
        };
    }
}
