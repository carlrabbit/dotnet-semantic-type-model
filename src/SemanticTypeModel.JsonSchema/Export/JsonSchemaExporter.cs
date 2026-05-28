using System.Globalization;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Contracts;
using SemanticTypeModel.Abstractions.Model;
using ProjectionTarget = SemanticTypeModel.Abstractions.Hardening.ProjectionTarget;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;
using SchemaDiagnosticStage = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticStage;

namespace SemanticTypeModel.JsonSchema.Export;

/// <summary>
/// Projects a canonical <see cref="TypeSchemaModel"/> to a JSON Schema Draft 2020-12 document.
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
        JsonSchemaExportResult result = Export(model);
        return result.Document.RootElement.GetRawText();
    }

    /// <summary>
    /// Exports the canonical model to a JSON Schema Draft 2020-12 document.
    /// </summary>
    public static JsonSchemaExportResult Export(TypeSchemaModel model, JsonSchemaExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        options ??= JsonSchemaExportOptions.Default;

        var diagnostics = new List<SchemaDiagnostic>();
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        if (options.Dialect != JsonSchemaDialect.Draft202012)
        {
            diagnostics.Add(new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Error,
                Code = "JSONSCHEMA_EXPORT_UNSUPPORTED_DIALECT",
                Message = $"Dialect '{options.Dialect}' is not supported. Exporting Draft 2020-12.",
                Stage = SchemaDiagnosticStage.Export,
                ModelPath = "/",
                Source = "/",
                ProjectionTarget = ProjectionTarget.JsonSchema,
            });
        }

        writer.WriteString("$schema", JsonSchemaDialectUris.Draft202012);

        if (options.SchemaId is not null)
        {
            writer.WriteString("$id", options.SchemaId.ToString());
        }
        else if (!string.IsNullOrWhiteSpace(model.RootIdentifier) && model.RootIdentifier != "#")
        {
            writer.WriteString("$id", model.RootIdentifier);
        }

        if (model.Root is not null)
        {
            WriteShapeProperties(writer, model.Root, options, diagnostics, "/");
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
                if (name.StartsWith("__remoteRef:", StringComparison.Ordinal))
                {
                    continue;
                }

                writer.WritePropertyName(name);
                writer.WriteStartObject();
                WriteShapeProperties(writer, shape, options, diagnostics, $"/$defs/{name}");
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
        writer.Flush();

        var document = JsonDocument.Parse(stream.ToArray());
        return new JsonSchemaExportResult(document, diagnostics);
    }

    private static void WriteShapeProperties(
        Utf8JsonWriter writer,
        TypeShape shape,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        IReadOnlyList<SchemaAnnotation> normalizedAnnotations = UiHintAnnotationProcessor.Normalize(
            shape.Annotations,
            options.UiHintOptions,
            diagnostics,
            pointer,
            shape,
            shape is EnumShape enumHintShape ? enumHintShape.Values : null);
        WriteAnnotations(writer, normalizedAnnotations, options, diagnostics, pointer);
        WriteConstraints(writer, shape.Constraints);

        switch (shape)
        {
            case ObjectShape obj:
                WriteObjectShape(writer, obj, options, diagnostics, pointer);
                break;
            case ScalarShape scalar:
                WriteScalarShape(writer, scalar);
                break;
            case EnumShape enumShape:
                WriteEnumShape(writer, enumShape);
                break;
            case ArrayShape array:
                WriteArrayShape(writer, array, options, diagnostics, pointer);
                break;
            case DictionaryShape dictionary:
                WriteDictionaryShape(writer, dictionary, options, diagnostics, pointer);
                break;
            case UnionShape union when TryWriteRefWrapper(writer, union):
                break;
            case UnionShape union:
                WriteUnionShape(writer, union, options, diagnostics, pointer);
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

        ShapeRef shapeRef = union.Options[0];
        if (shapeRef.IsRef)
        {
            writer.WriteString("$ref", shapeRef.Identifier == "#" ? "#" : $"#/$defs/{shapeRef.Identifier}");
        }
        else if (shapeRef.Inline is not null)
        {
            WriteShapeProperties(writer, shapeRef.Inline, JsonSchemaExportOptions.Default, [], "/");
        }

        return true;
    }

    private static void WriteObjectShape(
        Utf8JsonWriter writer,
        ObjectShape obj,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        writer.WriteString("type", "object");

        if (obj.Properties.Count > 0)
        {
            writer.WritePropertyName("properties");
            writer.WriteStartObject();

            var orderedProperties = obj.Properties
                .Select((property, index) => new
                {
                    Property = property,
                    Index = index,
                    Order = ResolvePropertyOrder(property.Annotations, diagnostics, $"{pointer}/properties/{property.Name}"),
                })
                .OrderBy(static item => item.Order ?? int.MaxValue)
                .ThenBy(static item => item.Index)
                .ThenBy(static item => item.Property.Name, StringComparer.Ordinal)
                .Select(static item => item.Property)
                .ToList();

            foreach (PropertyShape property in orderedProperties)
            {
                writer.WritePropertyName(property.Name);
                writer.WriteStartObject();
                WritePropertyShapeBody(writer, property, options, diagnostics, $"{pointer}/properties/{property.Name}");
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

    private static void WritePropertyShapeBody(
        Utf8JsonWriter writer,
        PropertyShape property,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        TypeShape? resolvedType = property.Type?.Inline;
        IReadOnlyList<SchemaAnnotation> normalizedAnnotations = UiHintAnnotationProcessor.Normalize(
            property.Annotations,
            options.UiHintOptions,
            diagnostics,
            pointer,
            resolvedType);
        WriteAnnotations(writer, normalizedAnnotations, options, diagnostics, pointer);
        WriteSchemaKeywordAnnotations(writer, normalizedAnnotations);

        if (options.UiExport.UiMode == JsonSchemaUiMode.JsonEditorCompatible &&
            options.UiExport.IncludeJsonEditorCompatibilityAnnotations &&
            !normalizedAnnotations.Any(static annotation => annotation.Key == "jsonEditor.propertyOrder") &&
            ResolvePropertyOrder(normalizedAnnotations, diagnostics, pointer) is { } propertyOrder)
        {
            writer.WriteNumber("propertyOrder", propertyOrder);
        }

        if (property.Type is null)
        {
            return;
        }

        if (property.IsNullable)
        {
            writer.WritePropertyName("oneOf");
            writer.WriteStartArray();

            writer.WriteStartObject();
            WriteShapeRefBody(writer, property.Type, options, diagnostics, pointer);
            writer.WriteEndObject();

            writer.WriteStartObject();
            writer.WriteString("type", "null");
            writer.WriteEndObject();

            writer.WriteEndArray();
            return;
        }

        WriteShapeRefBody(writer, property.Type, options, diagnostics, pointer);
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
            try
            {
                using var document = JsonDocument.Parse(value);
                document.RootElement.WriteTo(writer);
            }
            catch (JsonException)
            {
                writer.WriteStringValue(value);
            }
        }

        writer.WriteEndArray();
    }

    private static void WriteArrayShape(
        Utf8JsonWriter writer,
        ArrayShape array,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        writer.WriteString("type", "array");

        if (array.Items is null)
        {
            return;
        }

        writer.WritePropertyName("items");
        writer.WriteStartObject();
        WriteShapeRefBody(writer, array.Items, options, diagnostics, pointer + "/items");
        writer.WriteEndObject();
    }

    private static void WriteDictionaryShape(
        Utf8JsonWriter writer,
        DictionaryShape dictionary,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        writer.WriteString("type", "object");

        if (dictionary.Values is null)
        {
            return;
        }

        writer.WritePropertyName("additionalProperties");
        writer.WriteStartObject();
        WriteShapeRefBody(writer, dictionary.Values, options, diagnostics, pointer + "/additionalProperties");
        writer.WriteEndObject();
    }

    private static void WriteUnionShape(
        Utf8JsonWriter writer,
        UnionShape union,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        SchemaAnnotation? semantics = union.Annotations.FirstOrDefault(static annotation => annotation.Key == "jsonSchema.unionSemantics");
        var keyword = semantics?.Value;
        var unionKeyword = string.Equals(keyword, "anyOf", StringComparison.Ordinal) ? "anyOf" : "oneOf";

        writer.WritePropertyName(unionKeyword);
        writer.WriteStartArray();

        foreach (ShapeRef option in union.Options)
        {
            writer.WriteStartObject();
            WriteShapeRefBody(writer, option, options, diagnostics, pointer + $"/{unionKeyword}");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteShapeRefBody(
        Utf8JsonWriter writer,
        ShapeRef shapeRef,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        if (shapeRef.IsRef)
        {
            if (shapeRef.Identifier is not null && shapeRef.Identifier.StartsWith("__remoteRef:", StringComparison.Ordinal))
            {
                var externalRef = shapeRef.Identifier["__remoteRef:".Length..];
                writer.WriteString("$ref", externalRef);
                diagnostics.Add(new SchemaDiagnostic
                {
                    Severity = SchemaDiagnosticSeverity.Warning,
                    Code = "JSONSCHEMA_EXPORT_REMOTE_REF_REEMITTED",
                    Message = $"Remote reference '{externalRef}' was re-emitted.",
                    Stage = SchemaDiagnosticStage.Export,
                    ModelPath = pointer,
                    Source = pointer,
                    ProjectionTarget = ProjectionTarget.JsonSchema,
                });
                return;
            }

            writer.WriteString("$ref", shapeRef.Identifier == "#" ? "#" : $"#/$defs/{shapeRef.Identifier}");
            return;
        }

        if (shapeRef.Inline is not null)
        {
            WriteShapeProperties(writer, shapeRef.Inline, options, diagnostics, pointer);
        }
    }

    private static void WriteAnnotations(
        Utf8JsonWriter writer,
        IReadOnlyList<SchemaAnnotation> annotations,
        JsonSchemaExportOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        var title = ResolveDisplayText(annotations, options.UiHintOptions.PreferUiTitleOverDisplayName, "title");
        if (title is not null)
        {
            writer.WriteString("title", title);
        }

        var description = ResolveDisplayText(annotations, true, "description");
        if (description is not null)
        {
            writer.WriteString("description", description);
        }

        foreach (SchemaAnnotation annotation in annotations)
        {
            switch (annotation.Key)
            {
                case "ui.title":
                case "ui.description":
                case "title":
                case "schema.title":
                case "description":
                case "schema.description":
                    break;
                case "default":
                case "schema.default":
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
                case "jsonSchema.allOf":
                    writer.WritePropertyName("allOf");
                    using (var document = JsonDocument.Parse(annotation.Value))
                    {
                        document.RootElement.WriteTo(writer);
                    }
                    diagnostics.Add(new SchemaDiagnostic
                    {
                        Severity = SchemaDiagnosticSeverity.Warning,
                        Code = "JSONSCHEMA_EXPORT_ALLOF_PRESERVED",
                        Message = "allOf annotation was re-emitted as-is.",
                        Stage = SchemaDiagnosticStage.Export,
                        ModelPath = pointer,
                        Source = pointer,
                        ProjectionTarget = ProjectionTarget.JsonSchema,
                    });
                    break;
                default:
                    if (annotation.Key.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal) && options.IncludeProjectionAnnotations)
                    {
                        writer.WritePropertyName(annotation.Key["jsonSchema.keyword.".Length..].Replace('.', ':'));
                        using var document = JsonDocument.Parse(annotation.Value);
                        document.RootElement.WriteTo(writer);
                    }
                    else if (annotation.Key.StartsWith("ui.", StringComparison.Ordinal))
                    {
                        if (ShouldEmitGenericUiAnnotations(options))
                        {
                            writer.WritePropertyName($"ui:{annotation.Key["ui.".Length..]}");
                            using var document = JsonDocument.Parse(annotation.Value);
                            document.RootElement.WriteTo(writer);
                        }
                        else
                        {
                            diagnostics.Add(new SchemaDiagnostic
                            {
                                Severity = SchemaDiagnosticSeverity.Info,
                                Code = "JSONSCHEMA_UI_HINT_NOT_REPRESENTABLE",
                                Message = $"UI annotation '{annotation.Key}' was not emitted in the selected UI mode.",
                                Stage = SchemaDiagnosticStage.Export,
                                ModelPath = pointer,
                                Source = pointer,
                                ProjectionTarget = ProjectionTarget.JsonSchema,
                            });
                        }
                    }
                    else if (annotation.Key.StartsWith("jsonEditor.", StringComparison.Ordinal))
                    {
                        if (ShouldEmitJsonEditorAnnotations(options))
                        {
                            WriteJsonEditorAnnotation(writer, annotation);
                        }
                        else
                        {
                            diagnostics.Add(new SchemaDiagnostic
                            {
                                Severity = SchemaDiagnosticSeverity.Info,
                                Code = "JSONSCHEMA_UI_DOWNSTREAM_MODE_REQUIRED",
                                Message = $"Downstream annotation '{annotation.Key}' was not emitted because JSON-editor-compatible mode is disabled.",
                                Stage = SchemaDiagnosticStage.Export,
                                ModelPath = pointer,
                                Source = pointer,
                                ProjectionTarget = ProjectionTarget.JsonSchema,
                            });
                        }
                    }
                    else if (annotation.Key.StartsWith("jsonSchema.keyword.", StringComparison.Ordinal))
                    {
                        diagnostics.Add(new SchemaDiagnostic
                        {
                            Severity = SchemaDiagnosticSeverity.Info,
                            Code = "JSONSCHEMA_EXPORT_ANNOTATION_SKIPPED",
                            Message = $"Annotation '{annotation.Key}' was not emitted due to export options.",
                            Stage = SchemaDiagnosticStage.Export,
                            ModelPath = pointer,
                            Source = pointer,
                            ProjectionTarget = ProjectionTarget.JsonSchema,
                        });
                    }
                    else if (annotation.Key.StartsWith("ui.", StringComparison.Ordinal) ||
                             annotation.Key.StartsWith("jsonEditor.", StringComparison.Ordinal))
                    {
                        diagnostics.Add(new SchemaDiagnostic
                        {
                            Severity = SchemaDiagnosticSeverity.Info,
                            Code = "JSONSCHEMA_EXPORT_ANNOTATION_SKIPPED",
                            Message = $"Annotation '{annotation.Key}' was not emitted due to export options.",
                            Stage = SchemaDiagnosticStage.Export,
                            ModelPath = pointer,
                            Source = pointer,
                            ProjectionTarget = ProjectionTarget.JsonSchema,
                        });
                    }

                    break;
            }
        }
    }

    private static string? ResolveDisplayText(IReadOnlyList<SchemaAnnotation> annotations, bool preferUiText, string field)
    {
        var uiKey = $"ui.{field}";
        var schemaKey = $"schema.{field}";

        return preferUiText &&
               annotations.LastOrDefault(annotation => annotation.Key == uiKey) is { } ui &&
               UiHintAnnotationProcessor.TryReadString(ui.Value, out var uiValue)
            ? uiValue
            : (annotations.LastOrDefault(annotation => annotation.Key == schemaKey || annotation.Key == field) is { } schema &&
               UiHintAnnotationProcessor.TryReadString(schema.Value, out var schemaValue)
                ? schemaValue
                : (!preferUiText &&
                   annotations.LastOrDefault(annotation => annotation.Key == uiKey) is { } fallbackUi &&
                   UiHintAnnotationProcessor.TryReadString(fallbackUi.Value, out var fallbackUiValue)
                    ? fallbackUiValue
                    : null));
    }

    private static int? ResolvePropertyOrder(
        IReadOnlyList<SchemaAnnotation> annotations,
        List<SchemaDiagnostic> diagnostics,
        string pointer)
    {
        int? uiOrder = null;
        int? jsonEditorOrder = null;

        foreach (SchemaAnnotation annotation in annotations)
        {
            if (annotation.Key == "ui.order" && UiHintAnnotationProcessor.TryReadInt32(annotation.Value, out var parsedUiOrder))
            {
                uiOrder = parsedUiOrder;
            }
            else if (annotation.Key == "jsonEditor.propertyOrder" && UiHintAnnotationProcessor.TryReadInt32(annotation.Value, out var parsedJsonEditorOrder))
            {
                jsonEditorOrder = parsedJsonEditorOrder;
            }
        }

        if (uiOrder.HasValue && jsonEditorOrder.HasValue && uiOrder.Value != jsonEditorOrder.Value)
        {
            diagnostics.Add(new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "JSONSCHEMA_UI_PROPERTY_ORDER_CONFLICT",
                Message = $"Conflicting order hints detected: ui.order={uiOrder.Value} and jsonEditor.propertyOrder={jsonEditorOrder.Value}.",
                Stage = SchemaDiagnosticStage.Export,
                ModelPath = pointer,
                Source = pointer,
                ProjectionTarget = ProjectionTarget.JsonSchema,
            });
        }

        return jsonEditorOrder ?? uiOrder;
    }

    private static bool ShouldEmitGenericUiAnnotations(JsonSchemaExportOptions options)
    {
        return options.IncludeProjectionAnnotations &&
               options.UiExport.IncludeGenericUiAnnotations &&
               options.UiExport.UiMode is JsonSchemaUiMode.GenericExtensions or JsonSchemaUiMode.JsonEditorCompatible;
    }

    private static bool ShouldEmitJsonEditorAnnotations(JsonSchemaExportOptions options)
    {
        return options.IncludeProjectionAnnotations &&
               options.UiExport.IncludeJsonEditorCompatibilityAnnotations &&
               options.UiExport.UiMode == JsonSchemaUiMode.JsonEditorCompatible;
    }

    private static void WriteJsonEditorAnnotation(Utf8JsonWriter writer, SchemaAnnotation annotation)
    {
        var keyword = annotation.Key["jsonEditor.".Length..] switch
        {
            "propertyOrder" => "propertyOrder",
            "format" => "format",
            "options" => "options",
            "watch" => "watch",
            "template" => "template",
            _ => annotation.Key["jsonEditor.".Length..],
        };

        writer.WritePropertyName(keyword);
        using var document = JsonDocument.Parse(annotation.Value);
        document.RootElement.WriteTo(writer);
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
                case "const":
                    writer.WritePropertyName("const");
                    using (var document = JsonDocument.Parse(entry.Value))
                    {
                        document.RootElement.WriteTo(writer);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private static void WriteSchemaKeywordAnnotations(Utf8JsonWriter writer, IReadOnlyList<SchemaAnnotation> annotations)
    {
        foreach (SchemaAnnotation annotation in annotations)
        {
            var key = annotation.Key.StartsWith("schema.", StringComparison.Ordinal)
                ? annotation.Key["schema.".Length..]
                : annotation.Key;

            switch (key)
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
                    if (long.TryParse(annotation.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    {
                        writer.WriteNumber(key, longValue);
                    }
                    else if (double.TryParse(annotation.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
                    {
                        writer.WriteNumber(key, doubleValue);
                    }

                    break;
                case "pattern":
                case "format":
                    writer.WriteString(key, annotation.Value);
                    break;
                case "uniqueItems":
                    if (bool.TryParse(annotation.Value, out var boolValue))
                    {
                        writer.WriteBoolean(key, boolValue);
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
