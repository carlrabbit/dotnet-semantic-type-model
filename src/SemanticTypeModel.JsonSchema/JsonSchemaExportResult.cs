using System.Text.Json;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;

namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Result for runtime JSON Schema export.
/// </summary>
public sealed record JsonSchemaExportResult(
    JsonDocument Document,
    IReadOnlyList<SchemaDiagnostic> Diagnostics);
