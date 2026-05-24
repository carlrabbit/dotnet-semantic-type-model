using SemanticTypeModel.Abstractions.Model;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;

namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Result for runtime JSON Schema import.
/// </summary>
public sealed record JsonSchemaImportResult(
    TypeSchemaModel Model,
    IReadOnlyList<SchemaDiagnostic> Diagnostics);
