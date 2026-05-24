using System.Globalization;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using ProjectionTarget = SemanticTypeModel.Abstractions.Hardening.ProjectionTarget;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;
using SchemaDiagnosticSeverity = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticSeverity;
using SchemaDiagnosticStage = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnosticStage;

namespace SemanticTypeModel.JsonSchema.Export;

internal static class UiHintAnnotationProcessor
{
    private static readonly HashSet<string> KnownUiHints =
    [
        "ui.order",
        "ui.category",
        "ui.group",
        "ui.title",
        "ui.description",
        "ui.placeholder",
        "ui.hidden",
        "ui.readOnly",
        "ui.widget",
        "ui.width",
        "ui.defaultExpanded",
        "ui.enumLabels",
    ];

    private static readonly HashSet<string> KnownJsonEditorHints =
    [
        "jsonEditor.propertyOrder",
        "jsonEditor.format",
        "jsonEditor.options",
        "jsonEditor.watch",
        "jsonEditor.template",
    ];

    private static readonly HashSet<string> KnownWidgets =
    [
        "text",
        "textarea",
        "select",
        "radio",
        "checkbox",
        "date",
        "datetime",
        "number",
        "password",
        "markdown",
        "code",
        "uri",
    ];

    internal static IReadOnlyList<SchemaAnnotation> Normalize(
        IReadOnlyList<SchemaAnnotation> annotations,
        UiHintOptions options,
        List<SchemaDiagnostic> diagnostics,
        string pointer,
        TypeShape? ownerShape = null,
        IReadOnlyList<string>? enumValues = null)
    {
        List<SchemaAnnotation> normalized = [];
        int? uiOrder = null;
        int? jsonEditorPropertyOrder = null;
        var hasWidget = false;

        foreach (SchemaAnnotation annotation in annotations)
        {
            if (annotation.Key.StartsWith("ui.", StringComparison.Ordinal))
            {
                if (!KnownUiHints.Contains(annotation.Key) && options.StrictKnownHintsOnly)
                {
                    diagnostics.Add(Diagnostic(
                        SchemaDiagnosticSeverity.Error,
                        "JSONSCHEMA_UI_INVALID_KEY",
                        $"Annotation key '{annotation.Key}' is not a supported UI hint.",
                        pointer));
                }

                switch (annotation.Key)
                {
                    case "ui.order":
                        if (TryReadInt32(annotation.Value, out var uiOrderValue))
                        {
                            uiOrder = uiOrderValue;
                            normalized.Add(new SchemaAnnotation(annotation.Key, uiOrderValue.ToString(CultureInfo.InvariantCulture)));
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic(
                                SchemaDiagnosticSeverity.Error,
                                "JSONSCHEMA_UI_INVALID_ORDER",
                                "UI hint 'ui.order' must be an integer.",
                                pointer));
                            normalized.Add(annotation);
                        }

                        break;
                    case "ui.hidden":
                    case "ui.readOnly":
                    case "ui.defaultExpanded":
                        if (TryReadBoolean(annotation.Value, out var boolValue))
                        {
                            normalized.Add(new SchemaAnnotation(annotation.Key, boolValue ? "true" : "false"));
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic(
                                SchemaDiagnosticSeverity.Error,
                                "JSONSCHEMA_UI_INVALID_BOOLEAN",
                                $"UI hint '{annotation.Key}' must be a boolean value.",
                                pointer));
                            normalized.Add(annotation);
                        }

                        break;
                    case "ui.widget":
                        hasWidget = true;
                        if (TryReadString(annotation.Value, out var widgetName))
                        {
                            if (!KnownWidgets.Contains(widgetName) && options.StrictKnownHintsOnly)
                            {
                                diagnostics.Add(Diagnostic(
                                    SchemaDiagnosticSeverity.Error,
                                    "JSONSCHEMA_UI_UNSUPPORTED_WIDGET",
                                    $"UI widget '{widgetName}' is not supported in strict mode.",
                                    pointer));
                            }

                            normalized.Add(new SchemaAnnotation(annotation.Key, JsonSerializer.Serialize(widgetName)));
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic(
                                SchemaDiagnosticSeverity.Error,
                                "JSONSCHEMA_UI_INVALID_WIDGET",
                                "UI hint 'ui.widget' must be a string value.",
                                pointer));
                            normalized.Add(annotation);
                        }

                        break;
                    case "ui.enumLabels":
                        if (TryReadStringArray(annotation.Value, out var labels))
                        {
                            if (enumValues is not null && labels.Count != enumValues.Count)
                            {
                                diagnostics.Add(Diagnostic(
                                    SchemaDiagnosticSeverity.Error,
                                    "JSONSCHEMA_UI_ENUM_LABEL_MISMATCH",
                                    "UI hint 'ui.enumLabels' must have the same number of labels as enum values.",
                                    pointer));
                            }

                            normalized.Add(new SchemaAnnotation(annotation.Key, JsonSerializer.Serialize(labels)));
                        }
                        else
                        {
                            diagnostics.Add(Diagnostic(
                                SchemaDiagnosticSeverity.Error,
                                "JSONSCHEMA_UI_INVALID_ENUM_LABELS",
                                "UI hint 'ui.enumLabels' must be an array of strings.",
                                pointer));
                            normalized.Add(annotation);
                        }

                        break;
                    default:
                        normalized.Add(NormalizeUiStringAnnotation(annotation));
                        break;
                }

                continue;
            }

            if (annotation.Key.StartsWith("jsonEditor.", StringComparison.Ordinal))
            {
                if (!KnownJsonEditorHints.Contains(annotation.Key) && options.StrictKnownHintsOnly)
                {
                    diagnostics.Add(Diagnostic(
                        SchemaDiagnosticSeverity.Error,
                        "JSONSCHEMA_UI_UNSUPPORTED_JSONEDITOR_KEY",
                        $"Annotation key '{annotation.Key}' is not a supported jsonEditor hint in strict mode.",
                        pointer));
                }

                if (annotation.Key == "jsonEditor.propertyOrder")
                {
                    if (TryReadInt32(annotation.Value, out var propertyOrderValue))
                    {
                        jsonEditorPropertyOrder = propertyOrderValue;
                        normalized.Add(new SchemaAnnotation(annotation.Key, propertyOrderValue.ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic(
                            SchemaDiagnosticSeverity.Error,
                            "JSONSCHEMA_UI_INVALID_PROPERTY_ORDER",
                            "UI hint 'jsonEditor.propertyOrder' must be an integer.",
                            pointer));
                        normalized.Add(annotation);
                    }

                    continue;
                }

                normalized.Add(annotation.Key switch
                {
                    "jsonEditor.format" => NormalizeUiStringAnnotation(annotation),
                    "jsonEditor.template" => NormalizeUiStringAnnotation(annotation),
                    "jsonEditor.watch" => NormalizeUiStringAnnotation(annotation),
                    _ => annotation,
                });
                continue;
            }

            normalized.Add(annotation);
        }

        if (uiOrder.HasValue && jsonEditorPropertyOrder.HasValue && uiOrder.Value != jsonEditorPropertyOrder.Value)
        {
            diagnostics.Add(Diagnostic(
                SchemaDiagnosticSeverity.Warning,
                "JSONSCHEMA_UI_PROPERTY_ORDER_CONFLICT",
                $"Conflicting order hints detected: ui.order={uiOrder.Value} and jsonEditor.propertyOrder={jsonEditorPropertyOrder.Value}.",
                pointer));
        }

        if (options.InferWidgetHints && ownerShape is not null && (!hasWidget || options.OverwriteExplicitWidgetHint))
        {
            var inferredWidget = InferWidget(ownerShape);
            if (inferredWidget is not null)
            {
                normalized =
                [
                    .. normalized.Where(static annotation => annotation.Key != "ui.widget"),
                ];
                normalized.Add(new SchemaAnnotation("ui.widget", JsonSerializer.Serialize(inferredWidget)));
            }
        }

        return normalized;
    }

    internal static bool IsKnownUiHint(string key) => KnownUiHints.Contains(key);

    internal static bool IsKnownJsonEditorHint(string key) => KnownJsonEditorHints.Contains(key);

    internal static bool TryReadInt32(string value, out int result)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.Number && document.RootElement.TryGetInt32(out result))
            {
                return true;
            }

            if (document.RootElement.ValueKind == JsonValueKind.String &&
                int.TryParse(document.RootElement.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
        }
        catch (JsonException)
        {
        }

        result = default;
        return false;
    }

    internal static bool TryReadString(string value, out string result)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                result = document.RootElement.GetString() ?? string.Empty;
                return true;
            }
        }
        catch (JsonException)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                result = value;
                return true;
            }
        }

        result = string.Empty;
        return false;
    }

    private static bool TryReadBoolean(string value, out bool result)
    {
        if (bool.TryParse(value, out result))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                result = document.RootElement.GetBoolean();
                return true;
            }

            if (document.RootElement.ValueKind == JsonValueKind.String &&
                bool.TryParse(document.RootElement.GetString(), out result))
            {
                return true;
            }
        }
        catch (JsonException)
        {
        }

        result = default;
        return false;
    }

    private static bool TryReadStringArray(string value, out IReadOnlyList<string> result)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                List<string> items = [];
                foreach (JsonElement item in document.RootElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                    {
                        result = [];
                        return false;
                    }

                    items.Add(item.GetString() ?? string.Empty);
                }

                result = items;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        result = [];
        return false;
    }

    private static SchemaAnnotation NormalizeUiStringAnnotation(SchemaAnnotation annotation)
    {
        return TryReadString(annotation.Value, out var stringValue)
            ? new SchemaAnnotation(annotation.Key, JsonSerializer.Serialize(stringValue))
            : annotation;
    }

    private static string? InferWidget(TypeShape shape)
    {
        return shape switch
        {
            EnumShape => "select",
            ArrayShape => "array",
            ScalarShape { Kind: ScalarKind.Boolean } => "checkbox",
            ScalarShape { Kind: ScalarKind.Integer or ScalarKind.Number } => "number",
            ScalarShape scalar when HasStringFormat(scalar, "date") => "date",
            ScalarShape scalar when HasStringFormat(scalar, "date-time") => "datetime",
            ScalarShape scalar when HasStringFormat(scalar, "uri") => "uri",
            ScalarShape => "text",
            _ => null,
        };
    }

    private static bool HasStringFormat(ScalarShape scalar, string expectedFormat)
    {
        if (scalar.Kind != ScalarKind.String)
        {
            return false;
        }

        ConstraintEntry? format = scalar.Constraints.Entries.FirstOrDefault(static entry => entry.Key == "format");
        return string.Equals(format?.Value, expectedFormat, StringComparison.Ordinal);
    }

    private static SchemaDiagnostic Diagnostic(
        SchemaDiagnosticSeverity severity,
        string code,
        string message,
        string pointer)
    {
        return new SchemaDiagnostic
        {
            Severity = severity,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Export,
            ModelPath = pointer,
            Source = pointer,
            ProjectionTarget = ProjectionTarget.JsonSchema,
        };
    }
}
