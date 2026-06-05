using System.Text;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.Core.Inspection;

/// <summary>
/// Deterministic text inspection helpers for transformation results and traces.
/// </summary>
public static class TransformationTextExtensions
{
    /// <summary>
    /// Renders a deterministic transformation trace for a pipeline result.
    /// </summary>
    public static string ToTransformationText(this SemanticModelTransformationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Trace.ToTransformationText();
    }

    /// <summary>
    /// Renders a deterministic transformation trace.
    /// </summary>
    public static string ToTransformationText(this SemanticTransformationTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        StringBuilder builder = new();
        _ = builder.Append("Transformation Pipeline: ").AppendLine(trace.PipelineName);

        foreach (SemanticTransformationTraceEntry entry in trace.Entries.OrderBy(static entry => entry.Sequence))
        {
            _ = builder.AppendLine();
            _ = builder.Append('[').Append(entry.Sequence).Append("] ").AppendLine(entry.DisplayName);
            _ = builder.Append("    Id: ").AppendLine(entry.TransformationId);

            foreach (var change in entry.ChangeSummary.Order(StringComparer.Ordinal))
            {
                _ = builder.Append("    Derived: ").AppendLine(change);
            }

            _ = builder.Append("    Diagnostics: ").Append(entry.DiagnosticCount);
            if (entry.DiagnosticCodes.Count > 0)
            {
                _ = builder.Append(" (").Append(string.Join(", ", entry.DiagnosticCodes)).Append(')');
            }

            _ = builder.AppendLine();
        }

        return builder.ToString().ReplaceLineEndings("\n");
    }
}
