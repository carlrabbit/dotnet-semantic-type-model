using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.DependencyInjection;

internal static class RuntimeDiagnostics
{
    public static SchemaDiagnostic Error(string code, string message, SchemaDiagnosticStage stage, string? modelPath = "/", ProjectionTarget? projectionTarget = null)
    {
        return new SchemaDiagnostic
        {
            Severity = SchemaDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Stage = stage,
            ModelPath = modelPath,
            ProjectionTarget = projectionTarget,
        };
    }
}
