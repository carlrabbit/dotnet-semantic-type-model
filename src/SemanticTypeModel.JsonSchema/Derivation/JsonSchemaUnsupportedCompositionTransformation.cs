using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.JsonSchema.Derivation;

/// <summary>Diagnoses canonical composition shapes that JSON Schema M0029 derivation intentionally does not lower.</summary>
public sealed class JsonSchemaUnsupportedCompositionTransformation : ISemanticModelTransformation
{
    /// <inheritdoc />
    public string Id => "json-schema.diagnose-unsupported-composition";

    /// <inheritdoc />
    public string DisplayName => nameof(JsonSchemaUnsupportedCompositionTransformation);

    /// <inheritdoc />
    public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);

        foreach (TypeDefinition type in model.Types.OrderBy(static type => type.Id.Value, StringComparer.Ordinal))
        {
            switch (type)
            {
                case IntersectionTypeDefinition intersection:
                    context.Diagnostics.Report(Diagnostic(
                        "JSONSCHEMA_DERIVE_UNSUPPORTED_ALLOF",
                        "Intersection/allOf reduction is not supported by JSON Schema code-first projection.",
                        $"/types/{intersection.Id.Value}"));
                    break;
                case ObjectTypeDefinition { Composition.AllOf.Count: > 0 } objectType:
                    context.Diagnostics.Report(Diagnostic(
                        "JSONSCHEMA_DERIVE_UNSUPPORTED_ALLOF",
                        "Object allOf composition is not reduced by JSON Schema code-first projection.",
                        $"/types/{objectType.Id.Value}/composition/allOf"));
                    break;
                case UnionTypeDefinition { Options.Count: 0 } union:
                    context.Diagnostics.Report(Diagnostic(
                        "JSONSCHEMA_DERIVE_EMPTY_ALTERNATIVES",
                        "Union type has no alternatives and cannot be exported as oneOf or anyOf.",
                        $"/types/{union.Id.Value}/options"));
                    break;
                case UnionTypeDefinition { Discriminator: not null } union:
                    context.Diagnostics.Report(Diagnostic(
                        "JSONSCHEMA_DERIVE_UNSUPPORTED_DISCRIMINATOR",
                        "Full discriminator behavior is not supported by JSON Schema code-first projection.",
                        $"/types/{union.Id.Value}/discriminator"));
                    break;
                default:
                    break;
            }
        }

        return SemanticModelTransformationStepResult.Unchanged(model);

        SchemaDiagnostic Diagnostic(string code, string message, string path)
        {
            return new SchemaDiagnostic
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = code,
                Message = message,
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.TransformationId,
                ModelPath = path,
                Source = path,
                ProjectionTarget = ProjectionTarget.JsonSchema,
            };
        }
    }
}
