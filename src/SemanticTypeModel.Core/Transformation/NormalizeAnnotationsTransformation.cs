using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Annotations;

namespace SemanticTypeModel.Core.Transformation;

/// <summary>
/// Normalizes annotation keys and applies deterministic duplicate-key merge behavior.
/// </summary>
public sealed class NormalizeAnnotationsTransformation : ISchemaTransformation
{
    /// <inheritdoc />
    public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        TypeSchemaModel current = model.Model;
        AnnotationPolicy policy = context.AnnotationPolicy;

        AnnotationBag NormalizeBag(AnnotationBag bag, string parentPath)
        {
            List<Annotation> normalizedItems = [];
            Dictionary<string, int> indicesByKey = new(StringComparer.Ordinal);

            foreach (Annotation annotation in bag.Items)
            {
                AnnotationKeyValidationResult validation = AnnotationKeyRules.Validate(annotation.Key);
                string diagnosticPath = ModelPath.ForAnnotation(parentPath, annotation.Key);

                if (!validation.IsValid)
                {
                    context.Diagnostics.Report(new SchemaDiagnostic
                    {
                        Severity = SchemaDiagnosticSeverity.Warning,
                        Code = "STM1001",
                        Message = validation.Error ?? $"Annotation key '{annotation.Key.Value}' is malformed.",
                        Stage = SchemaDiagnosticStage.Transformation,
                        PipelineStage = context.PipelineStage,
                        ModelPath = diagnosticPath,
                    });

                    if (!policy.RemoveMalformedAnnotations)
                    {
                        normalizedItems.Add(annotation);
                    }

                    continue;
                }

                Annotation normalized = annotation with { Key = validation.NormalizedKey };
                string normalizedKey = normalized.Key.Value;
                string normalizedPath = ModelPath.ForAnnotation(parentPath, normalized.Key);

                if (validation.NamespaceCaseChanged)
                {
                    context.Diagnostics.Report(new SchemaDiagnostic
                    {
                        Severity = SchemaDiagnosticSeverity.Info,
                        Code = "STM1003",
                        Message = $"Annotation namespace '{annotation.Key.Value}' was normalized to '{normalized.Key.Value}'.",
                        Stage = SchemaDiagnosticStage.Transformation,
                        PipelineStage = context.PipelineStage,
                        ModelPath = normalizedPath,
                    });
                }

                if (!indicesByKey.TryGetValue(normalizedKey, out int existingIndex))
                {
                    indicesByKey[normalizedKey] = normalizedItems.Count;
                    normalizedItems.Add(normalized);
                    continue;
                }

                SchemaDiagnosticSeverity severity = AnnotationKeyRules.IsReservedNamespace(validation.Namespace)
                    ? SchemaDiagnosticSeverity.Warning
                    : SchemaDiagnosticSeverity.Info;

                context.Diagnostics.Report(new SchemaDiagnostic
                {
                    Severity = severity,
                    Code = "STM1002",
                    Message = $"Duplicate annotation key '{normalizedKey}' was merged using '{policy.MergeBehavior}'.",
                    Stage = SchemaDiagnosticStage.Transformation,
                    PipelineStage = context.PipelineStage,
                    ModelPath = normalizedPath,
                });

                switch (policy.MergeBehavior)
                {
                    case AnnotationMergeBehavior.LastWins:
                        normalizedItems[existingIndex] = normalized;
                        break;
                    case AnnotationMergeBehavior.FirstWins:
                        break;
                    case AnnotationMergeBehavior.Error:
                        context.Diagnostics.Report(new SchemaDiagnostic
                        {
                            Severity = SchemaDiagnosticSeverity.Error,
                            Code = "STM1004",
                            Message = $"Duplicate annotation key '{normalizedKey}' is not allowed by the current policy.",
                            Stage = SchemaDiagnosticStage.Transformation,
                            PipelineStage = context.PipelineStage,
                            ModelPath = normalizedPath,
                        });
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported annotation merge behavior '{policy.MergeBehavior}'.");
                }
            }

            return new AnnotationBag { Items = normalizedItems };
        }

        TypeDefinition NormalizeType(TypeDefinition type)
        {
            TypeDefinition clone = TypeSchemaModelCloner.CloneTypeDefinition(type);
            string typePath = ModelPath.ForType(type.Id);

            return clone switch
            {
                ObjectTypeDefinition objectType => objectType with
                {
                    Annotations = NormalizeBag(objectType.Annotations, typePath),
                    Properties = [.. objectType.Properties.Select(property => property with
                    {
                        Annotations = NormalizeBag(property.Annotations, ModelPath.ForProperty(type.Id, property.Name)),
                    })],
                    Keys = [.. objectType.Keys.Select(key => key with
                    {
                        Annotations = NormalizeBag(key.Annotations, ModelPath.ForKey(type.Id, key.Name)),
                    })],
                    Relationships = [.. objectType.Relationships.Select(relationship => relationship with
                    {
                        Annotations = NormalizeBag(relationship.Annotations, ModelPath.ForRelationship(type.Id, relationship.Id)),
                    })],
                    ComputedMembers = [.. objectType.ComputedMembers.Select(member => member with
                    {
                        Annotations = NormalizeBag(member.Annotations, ModelPath.ForComputedMember(type.Id, member.Name)),
                    })],
                },
                EnumTypeDefinition enumType => enumType with
                {
                    Annotations = NormalizeBag(enumType.Annotations, typePath),
                    Values = [.. enumType.Values.Select(value => value with
                    {
                        Annotations = NormalizeBag(value.Annotations, ModelPath.ForEnumValue(type.Id, value.Name)),
                    })],
                },
                _ => clone with
                {
                    Annotations = NormalizeBag(clone.Annotations, typePath),
                },
            };
        }

        List<TypeDefinition> normalizedTypes = [.. current.Types.Select(NormalizeType)];
        model.Replace(new TypeSchemaModel
        {
            Id = current.Id,
            Types = normalizedTypes,
            TypesById = TypeSchemaModelCloner.CreateTypeIndex(normalizedTypes),
            Annotations = NormalizeBag(TypeSchemaModelCloner.CloneAnnotationBag(current.Annotations), "/"),
        });

        return ValueTask.CompletedTask;
    }
}
