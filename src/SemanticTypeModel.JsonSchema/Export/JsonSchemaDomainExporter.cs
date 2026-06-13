using System.Text.Json;
using SemanticTypeModel.JsonSchema.Domain;
using Canonical = SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.JsonSchema.Export;

/// <summary>Exports a JSON Schema domain semantic model to deterministic Draft 2020-12 JSON Schema.</summary>
public static class JsonSchemaDomainExporter
{
    /// <summary>Exports the derived JSON Schema domain semantic model.</summary>
    public static JsonSchemaExportResult Export(JsonSchemaSemanticModel model, JsonSchemaExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= JsonSchemaExportOptions.Default;
        List<Canonical.SchemaDiagnostic> diagnostics = [.. model.Diagnostics];

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });
        writer.WriteStartObject();
        writer.WriteString("$schema", model.DialectUri);
        if (options.SchemaId is not null)
        {
            writer.WriteString("$id", options.SchemaId.ToString());
        }
        else if (model.Id is not null)
        {
            writer.WriteString("$id", model.Id.ToString());
        }

        WriteNode(writer, model.Root, diagnostics, "/");

        var definitions = model.Definitions.OrderBy(static pair => pair.Key, StringComparer.Ordinal).ToList();
        if (definitions.Count > 0)
        {
            writer.WritePropertyName("$defs");
            writer.WriteStartObject();
            foreach ((var name, JsonSchemaNode node) in definitions)
            {
                writer.WritePropertyName(name);
                writer.WriteStartObject();
                WriteNode(writer, node, diagnostics, $"/$defs/{name}");
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();
        return new JsonSchemaExportResult(JsonDocument.Parse(stream.ToArray()), diagnostics);
    }

    private static void WriteNode(Utf8JsonWriter writer, JsonSchemaNode node, List<Canonical.SchemaDiagnostic> diagnostics, string pointer)
    {
        if (!string.IsNullOrWhiteSpace(node.Title))
        {
            writer.WriteString("title", node.Title);
        }

        if (!string.IsNullOrWhiteSpace(node.Description))
        {
            writer.WriteString("description", node.Description);
        }

        foreach ((var key, JsonElement value) in node.Annotations.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            writer.WritePropertyName(key.Replace('.', ':'));
            value.WriteTo(writer);
        }

        switch (node)
        {
            case JsonSchemaObjectNode obj:
                WriteObject(writer, obj, diagnostics, pointer);
                break;
            case JsonSchemaScalarNode scalar:
                WriteScalar(writer, scalar);
                break;
            case JsonSchemaArrayNode array:
                writer.WriteString("type", "array");
                WriteConstraints(writer, array.Constraints);
                writer.WritePropertyName("items");
                writer.WriteStartObject();
                WriteRef(writer, array.Items, diagnostics, pointer + "/items");
                writer.WriteEndObject();
                break;
            case JsonSchemaDictionaryNode dictionary:
                writer.WriteString("type", "object");
                writer.WritePropertyName("additionalProperties");
                writer.WriteStartObject();
                WriteRef(writer, dictionary.Values, diagnostics, pointer + "/additionalProperties");
                writer.WriteEndObject();
                break;
            case JsonSchemaEnumNode enumNode:
                writer.WritePropertyName("enum");
                writer.WriteStartArray();
                foreach (JsonElement value in enumNode.Values)
                {
                    value.WriteTo(writer);
                }
                writer.WriteEndArray();
                break;
            case JsonSchemaCompositionNode composition:
                WriteComposition(writer, composition, diagnostics, pointer);
                break;
            default:
                break;
        }
    }

    private static void WriteObject(Utf8JsonWriter writer, JsonSchemaObjectNode obj, List<Canonical.SchemaDiagnostic> diagnostics, string pointer)
    {
        writer.WriteString("type", "object");
        if (obj.Properties.Count > 0)
        {
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            foreach (JsonSchemaProperty property in obj.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartObject();
                if (!string.IsNullOrWhiteSpace(property.Title))
                {
                    writer.WriteString("title", property.Title);
                }

                if (!string.IsNullOrWhiteSpace(property.Description))
                {
                    writer.WriteString("description", property.Description);
                }

                foreach ((var key, JsonElement value) in property.Annotations.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(key.Replace('.', ':'));
                    value.WriteTo(writer);
                }

                WriteConstraints(writer, property.Constraints);
                if (property.IsNullable)
                {
                    writer.WritePropertyName("oneOf");
                    writer.WriteStartArray();
                    writer.WriteStartObject();
                    WriteRef(writer, property.Schema, diagnostics, pointer + "/properties/" + property.Name);
                    writer.WriteEndObject();
                    writer.WriteStartObject();
                    writer.WriteString("type", "null");
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                }
                else
                {
                    WriteRef(writer, property.Schema, diagnostics, pointer + "/properties/" + property.Name);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            string[] required = [.. obj.Properties.Where(static property => property.IsRequired).Select(static property => property.Name).Order(StringComparer.Ordinal)];
            if (required.Length > 0)
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

    private static void WriteScalar(Utf8JsonWriter writer, JsonSchemaScalarNode scalar)
    {
        if (scalar.IsNullable && scalar.Type != "null")
        {
            writer.WritePropertyName("oneOf");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("type", scalar.Type);
            if (scalar.Format is not null)
            {
                writer.WriteString("format", scalar.Format);
            }
            writer.WriteEndObject();
            writer.WriteStartObject();
            writer.WriteString("type", "null");
            writer.WriteEndObject();
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteString("type", scalar.Type);
            if (scalar.Format is not null)
            {
                writer.WriteString("format", scalar.Format);
            }
        }

        WriteConstraints(writer, scalar.Constraints);
    }

    private static void WriteComposition(Utf8JsonWriter writer, JsonSchemaCompositionNode composition, List<Canonical.SchemaDiagnostic> diagnostics, string pointer)
    {
        var keyword = composition.Kind == JsonSchemaCompositionKind.AnyOf ? "anyOf" : "oneOf";
        if (composition.Alternatives.Count == 0)
        {
            diagnostics.Add(Diagnostic("JSONSCHEMA_EXPORT_EMPTY_ALTERNATIVES", "Composition node has no alternatives.", pointer));
        }

        writer.WritePropertyName(keyword);
        writer.WriteStartArray();
        foreach (JsonSchemaSchemaRef alternative in composition.Alternatives.OrderBy(static alternative => alternative.Reference, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            WriteRef(writer, alternative, diagnostics, pointer + "/" + keyword);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteRef(Utf8JsonWriter writer, JsonSchemaSchemaRef schemaRef, List<Canonical.SchemaDiagnostic> diagnostics, string pointer)
    {
        if (schemaRef.Reference is not null)
        {
            writer.WriteString("$ref", schemaRef.Reference == "#" ? "#" : $"#/$defs/{schemaRef.Reference}");
            return;
        }

        if (schemaRef.Inline is not null)
        {
            if (schemaRef.Inline is JsonSchemaCompositionNode)
            {
                diagnostics.Add(Diagnostic("JSONSCHEMA_EXPORT_UNSUPPORTED_NESTED_COMPOSITION", "Nested inline composition is not part of baseline JSON Schema projection.", pointer));
            }

            WriteNode(writer, schemaRef.Inline, diagnostics, pointer);
            return;
        }

        diagnostics.Add(Diagnostic("JSONSCHEMA_EXPORT_UNRESOLVED_ALTERNATIVE", "Schema reference was unresolved.", pointer));
        writer.WriteBoolean("not", true);
    }

    private static void WriteConstraints(Utf8JsonWriter writer, JsonSchemaConstraintSet constraints)
    {
        if (constraints.MinLength is { } minLength)
        {
            writer.WriteNumber("minLength", minLength);
        }

        if (constraints.MaxLength is { } maxLength)
        {
            writer.WriteNumber("maxLength", maxLength);
        }

        if (constraints.Pattern is not null)
        {
            writer.WriteString("pattern", constraints.Pattern);
        }

        if (constraints.Minimum is { } minimum)
        {
            writer.WriteNumber("minimum", minimum);
        }

        if (constraints.Maximum is { } maximum)
        {
            writer.WriteNumber("maximum", maximum);
        }

        if (constraints.ExclusiveMinimum)
        {
            writer.WriteBoolean("exclusiveMinimum", true);
        }

        if (constraints.ExclusiveMaximum)
        {
            writer.WriteBoolean("exclusiveMaximum", true);
        }

        if (constraints.MultipleOf is { } multipleOf)
        {
            writer.WriteNumber("multipleOf", multipleOf);
        }

        if (constraints.MinItems is { } minItems)
        {
            writer.WriteNumber("minItems", minItems);
        }

        if (constraints.MaxItems is { } maxItems)
        {
            writer.WriteNumber("maxItems", maxItems);
        }

        if (constraints.UniqueItems)
        {
            writer.WriteBoolean("uniqueItems", true);
        }

        if (constraints.MinProperties is { } minProperties)
        {
            writer.WriteNumber("minProperties", minProperties);
        }

        if (constraints.MaxProperties is { } maxProperties)
        {
            writer.WriteNumber("maxProperties", maxProperties);
        }
    }

    private static Canonical.SchemaDiagnostic Diagnostic(string code, string message, string pointer)
    {
        return new()
        {
            Severity = Canonical.SchemaDiagnosticSeverity.Warning,
            Code = code,
            Message = message,
            Stage = Canonical.SchemaDiagnosticStage.Export,
            ModelPath = pointer,
            Source = pointer,
            ProjectionTarget = Canonical.ProjectionTarget.JsonSchema,
        };
    }
}
