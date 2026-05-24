using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.Core.Validation;

/// <summary>
/// Validates a <see cref="TypeSchemaModel"/> against the canonical model invariants and returns
/// a list of <see cref="SchemaDiagnostic"/> entries describing any violations found.
/// </summary>
public sealed class TypeSchemaModelValidator
{
    /// <summary>
    /// Validates the given <paramref name="model"/> and returns all diagnostics found.
    /// An empty list means the model is clean.
    /// </summary>
    public static IReadOnlyList<SchemaDiagnostic> Validate(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var diagnostics = new List<SchemaDiagnostic>();

        CheckDuplicateTypeIds(model, diagnostics);
        CheckUnresolvedTypeRefs(model, diagnostics);
        CheckDuplicatePropertyNames(model, diagnostics);
        CheckRelationshipPropertyRefs(model, diagnostics);
        CheckInvalidCardinality(model, diagnostics);
        CheckAnnotationKeyFormat(model, diagnostics);

        return diagnostics;
    }

    // -------------------------------------------------------------------------
    // 1. Duplicate TypeId
    // -------------------------------------------------------------------------

    private static void CheckDuplicateTypeIds(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (TypeDefinition type in model.Types)
        {
            if (!seen.Add(type.Id.Value))
            {
                diagnostics.Add(Error(
                    "MODEL_DUPLICATE_TYPE_ID",
                    $"Duplicate TypeId '{type.Id.Value}' found in the model.",
                    ModelPath.ForType(type.Id)));
            }
        }
    }

    // -------------------------------------------------------------------------
    // 2. Unresolved TypeRef
    // -------------------------------------------------------------------------

    private static void CheckUnresolvedTypeRefs(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            foreach ((TypeRef typeRef, var refPath) in CollectTypeRefs(type))
            {
                if (!model.TypesById.ContainsKey(typeRef.Id))
                {
                    diagnostics.Add(Error(
                        "MODEL_UNRESOLVED_TYPE_REF",
                        $"TypeRef '{typeRef.Id.Value}' at '{refPath}' cannot be resolved in the model.",
                        refPath));
                }
            }
        }
    }

    private static IEnumerable<(TypeRef Ref, string Path)> CollectTypeRefs(TypeDefinition type)
    {
        var typePath = ModelPath.ForType(type.Id);
        switch (type)
        {
            case ObjectTypeDefinition obj:
                foreach (PropertyDefinition prop in obj.Properties)
                {
                    yield return (prop.Type, ModelPath.ForProperty(type.Id, prop.Name));
                }

                foreach (TypeRef allOfRef in obj.Composition.AllOf)
                {
                    yield return (allOfRef, $"{typePath}/composition/allOf");
                }

                foreach (RelationshipDefinition rel in obj.Relationships)
                {
                    var relPath = ModelPath.ForRelationship(type.Id, rel.Id);
                    yield return (rel.PrincipalType, $"{relPath}/principalType");
                    yield return (rel.DependentType, $"{relPath}/dependentType");
                }

                foreach (ComputedMemberDefinition cm in obj.ComputedMembers)
                {
                    yield return (cm.ResultType, ModelPath.ForComputedMember(type.Id, cm.Name));
                }

                break;

            case ArrayTypeDefinition array:
                yield return (array.ItemType, $"{typePath}/itemType");
                break;

            case DictionaryTypeDefinition dict:
                yield return (dict.KeyType, $"{typePath}/keyType");
                yield return (dict.ValueType, $"{typePath}/valueType");
                break;

            case UnionTypeDefinition union:
                for (var i = 0; i < union.Options.Count; i++)
                {
                    yield return (union.Options[i], $"{typePath}/options/{i}");
                }

                if (union.Discriminator is not null)
                {
                    foreach (KeyValuePair<string, TypeRef> mapping in union.Discriminator.Mapping)
                    {
                        yield return (mapping.Value, $"{typePath}/discriminator/mapping/{mapping.Key}");
                    }
                }

                break;

            case IntersectionTypeDefinition intersection:
                for (var i = 0; i < intersection.Members.Count; i++)
                {
                    yield return (intersection.Members[i], $"{typePath}/members/{i}");
                }

                break;

            case ReferenceTypeDefinition reference:
                yield return (reference.Target, $"{typePath}/target");
                break;

            case ScalarTypeDefinition:
            case EnumTypeDefinition:
            default:
                // No TypeRefs to collect for scalar, enum, or unknown types.
                break;
        }
    }

    // -------------------------------------------------------------------------
    // 3. Duplicate property names within an object
    // -------------------------------------------------------------------------

    private static void CheckDuplicatePropertyNames(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            if (type is not ObjectTypeDefinition obj)
            {
                continue;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyDefinition prop in obj.Properties)
            {
                if (!seen.Add(prop.Name))
                {
                    diagnostics.Add(Error(
                        "MODEL_DUPLICATE_PROPERTY_NAME",
                        $"Duplicate property name '{prop.Name}' in object type '{type.Id.Value}'.",
                        ModelPath.ForProperty(type.Id, prop.Name)));
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // 4. Relationship references to missing properties
    // -------------------------------------------------------------------------

    private static void CheckRelationshipPropertyRefs(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            if (type is not ObjectTypeDefinition obj)
            {
                continue;
            }

            foreach (RelationshipDefinition rel in obj.Relationships)
            {
                var relPath = ModelPath.ForRelationship(type.Id, rel.Id);

                CheckPropertyRefsExist(
                    model,
                    rel.PrincipalProperties,
                    rel.PrincipalType,
                    $"{relPath}/principalProperties",
                    diagnostics);

                CheckPropertyRefsExist(
                    model,
                    rel.DependentProperties,
                    rel.DependentType,
                    $"{relPath}/dependentProperties",
                    diagnostics);
            }
        }
    }

    private static void CheckPropertyRefsExist(
        TypeSchemaModel model,
        IReadOnlyList<PropertyRef> refs,
        TypeRef ownerType,
        string parentPath,
        List<SchemaDiagnostic> diagnostics)
    {
        if (!model.TypesById.TryGetValue(ownerType.Id, out TypeDefinition? typeDef)
            || typeDef is not ObjectTypeDefinition objectType)
        {
            // Unresolved type ref already covered by rule 2.
            return;
        }

        var propertyIds = new HashSet<PropertyId>(objectType.Properties.Select(p => p.Id));

        foreach (PropertyRef propRef in refs)
        {
            if (!propertyIds.Contains(propRef.Id))
            {
                diagnostics.Add(Warning(
                    "MODEL_UNRESOLVED_PROPERTY_REF",
                    $"PropertyRef '{propRef.Id.Value}' at '{parentPath}' does not match any property in type '{ownerType.Id.Value}'.",
                    parentPath));
            }
        }
    }

    // -------------------------------------------------------------------------
    // 5. Invalid cardinality (MinItems > MaxItems)
    // -------------------------------------------------------------------------

    private static void CheckInvalidCardinality(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            if (type is ObjectTypeDefinition obj)
            {
                foreach (PropertyDefinition prop in obj.Properties)
                {
                    Cardinality c = prop.Cardinality;
                    if (c.MinItems.HasValue && c.MaxItems.HasValue && c.MinItems > c.MaxItems)
                    {
                        diagnostics.Add(Error(
                            "MODEL_INVALID_CARDINALITY",
                            $"Property '{prop.Name}' in type '{type.Id.Value}' has MinItems ({c.MinItems}) > MaxItems ({c.MaxItems}).",
                            ModelPath.ForProperty(type.Id, prop.Name)));
                    }
                }
            }

            if (type is ArrayTypeDefinition array)
            {
                if (array.MinItems.HasValue && array.MaxItems.HasValue && array.MinItems > array.MaxItems)
                {
                    diagnostics.Add(Error(
                        "MODEL_INVALID_CARDINALITY",
                        $"Array type '{type.Id.Value}' has MinItems ({array.MinItems}) > MaxItems ({array.MaxItems}).",
                        ModelPath.ForType(type.Id)));
                }
            }
        }
    }

    // -------------------------------------------------------------------------
    // 6. Invalid annotation key (must be "namespace.name")
    // -------------------------------------------------------------------------

    private static void CheckAnnotationKeyFormat(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        CheckAnnotationBag(model.Annotations, "/", diagnostics);

        foreach (TypeDefinition type in model.Types)
        {
            var typePath = ModelPath.ForType(type.Id);
            CheckAnnotationBag(type.Annotations, typePath, diagnostics);

            if (type is ObjectTypeDefinition obj)
            {
                foreach (PropertyDefinition prop in obj.Properties)
                {
                    CheckAnnotationBag(prop.Annotations, ModelPath.ForProperty(type.Id, prop.Name), diagnostics);
                }

                foreach (RelationshipDefinition rel in obj.Relationships)
                {
                    CheckAnnotationBag(rel.Annotations, ModelPath.ForRelationship(type.Id, rel.Id), diagnostics);
                }

                foreach (KeyDefinition key in obj.Keys)
                {
                    CheckAnnotationBag(key.Annotations, ModelPath.ForKey(type.Id, key.Name), diagnostics);
                }

                foreach (ComputedMemberDefinition cm in obj.ComputedMembers)
                {
                    CheckAnnotationBag(cm.Annotations, ModelPath.ForComputedMember(type.Id, cm.Name), diagnostics);
                }
            }

            if (type is EnumTypeDefinition enumType)
            {
                foreach (EnumValueDefinition val in enumType.Values)
                {
                    CheckAnnotationBag(val.Annotations, $"{typePath}/values/{val.Name}", diagnostics);
                }
            }
        }
    }

    private static void CheckAnnotationBag(AnnotationBag bag, string parentPath, List<SchemaDiagnostic> diagnostics)
    {
        foreach (Annotation annotation in bag.Items)
        {
            if (!IsValidAnnotationKey(annotation.Key))
            {
                diagnostics.Add(Warning(
                    "MODEL_INVALID_ANNOTATION_KEY",
                    $"Annotation key '{annotation.Key.Value}' at '{parentPath}' is not a valid namespaced key (expected 'namespace.name').",
                    ModelPath.ForAnnotation(parentPath, annotation.Key)));
            }
        }
    }

    private static bool IsValidAnnotationKey(AnnotationKey key)
    {
        var value = key.Value;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var dotIndex = value.IndexOf('.', StringComparison.Ordinal);
        return dotIndex > 0 && dotIndex < value.Length - 1;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SchemaDiagnostic Error(string code, string message, string? modelPath = null)
    {
        return new SchemaDiagnostic
        {
            Severity = SchemaDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            ModelPath = modelPath,
        };
    }

    private static SchemaDiagnostic Warning(string code, string message, string? modelPath = null)
    {
        return new SchemaDiagnostic
        {
            Severity = SchemaDiagnosticSeverity.Warning,
            Code = code,
            Message = message,
            ModelPath = modelPath,
        };
    }
}
