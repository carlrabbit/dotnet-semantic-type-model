using System.ComponentModel;
using SemanticTypeModel.Abstractions.Model;
using SchemaDiagnostic = SemanticTypeModel.Abstractions.Hardening.SchemaDiagnostic;

namespace SemanticTypeModel.JsonSchema;

/// <summary>
/// Legacy compatibility result for runtime JSON Schema import. JSON Schema import is not a supported canonical model creation path.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record JsonSchemaImportResult(
    TypeSchemaModel Model,
    IReadOnlyList<SchemaDiagnostic> Diagnostics);
