using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Validation;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Emits validation diagnostics for canonical model invariants without mutating the model.
/// </summary>
public sealed class ValidateModelTransformation : ISchemaTransformation
{
    /// <inheritdoc />
    public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        context.Diagnostics.ReportRange(TypeSchemaModelValidator.Validate(model.Model));
        return ValueTask.CompletedTask;
    }
}
