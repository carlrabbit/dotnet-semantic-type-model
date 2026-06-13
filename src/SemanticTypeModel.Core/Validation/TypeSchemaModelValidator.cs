using System.Text.Json;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Annotations;

namespace SemanticTypeModel.Core.Validation;

/// <summary>
/// Validates a <see cref="TypeSchemaModel"/> against canonical model invariants and returns
/// machine-queryable diagnostics for any violations found.
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
        CheckDuplicateKeyNames(model, diagnostics);
        CheckKeyPropertyRefs(model, diagnostics);
        CheckRelationships(model, diagnostics);
        CheckInvalidCardinality(model, diagnostics);
        CheckInvalidConstraintRanges(model, diagnostics);
        CheckAnnotationKeys(model, diagnostics);
        CheckEnumDuplicates(model, diagnostics);

        return diagnostics;
    }

    private static void CheckDuplicateTypeIds(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (TypeDefinition type in model.Types)
        {
            if (!seen.Add(type.Id.Value))
            {
                diagnostics.Add(Error(
                    "STM0001",
                    $"Duplicate TypeId '{type.Id.Value}' found in the model.",
                    ModelPath.ForType(type.Id)));
            }
        }
    }

    private static void CheckUnresolvedTypeRefs(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            foreach ((TypeRef typeRef, var refPath) in CollectNonRelationshipTypeRefs(type))
            {
                if (!model.TypesById.ContainsKey(typeRef.Id))
                {
                    diagnostics.Add(Error(
                        "STM0002",
                        $"TypeRef '{typeRef.Id.Value}' at '{refPath}' cannot be resolved in the model.",
                        refPath));
                }
            }
        }
    }

    private static IEnumerable<(TypeRef Ref, string Path)> CollectNonRelationshipTypeRefs(TypeDefinition type)
    {
        var typePath = ModelPath.ForType(type.Id);

        switch (type)
        {
            case ObjectTypeDefinition objectType:
                foreach (PropertyDefinition property in objectType.Properties)
                {
                    yield return (property.Type, ModelPath.ForProperty(type.Id, property.Name));
                }

                for (var index = 0; index < objectType.Composition.AllOf.Count; index++)
                {
                    yield return (objectType.Composition.AllOf[index], $"{typePath}/composition/allOf/{index}");
                }

                foreach (ComputedMemberDefinition computedMember in objectType.ComputedMembers)
                {
                    yield return (computedMember.ResultType, ModelPath.ForComputedMember(type.Id, computedMember.Name));
                }

                break;
            case ArrayTypeDefinition arrayType:
                yield return (arrayType.ItemType, $"{typePath}/itemType");
                break;
            case DictionaryTypeDefinition dictionaryType:
                yield return (dictionaryType.KeyType, $"{typePath}/keyType");
                yield return (dictionaryType.ValueType, $"{typePath}/valueType");
                break;
            case UnionTypeDefinition unionType:
                for (var index = 0; index < unionType.Options.Count; index++)
                {
                    yield return (unionType.Options[index], $"{typePath}/options/{index}");
                }

                if (unionType.Discriminator is not null)
                {
                    foreach ((var key, TypeRef mapping) in unionType.Discriminator.Mapping)
                    {
                        yield return (mapping, $"{typePath}/discriminator/mapping/{key}");
                    }
                }

                break;
            case IntersectionTypeDefinition intersectionType:
                for (var index = 0; index < intersectionType.Members.Count; index++)
                {
                    yield return (intersectionType.Members[index], $"{typePath}/members/{index}");
                }

                break;
            case ReferenceTypeDefinition referenceType:
                yield return (referenceType.Target, $"{typePath}/target");
                break;
            default:
                break;
        }
    }

    private static void CheckDuplicatePropertyNames(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyDefinition property in objectType.Properties)
            {
                if (!seen.Add(property.Name))
                {
                    diagnostics.Add(Error(
                        "STM0003",
                        $"Duplicate property name '{property.Name}' exists within object type '{objectType.Id.Value}'.",
                        ModelPath.ForProperty(objectType.Id, property.Name)));
                }
            }
        }
    }

    private static void CheckDuplicateKeyNames(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            foreach (KeyDefinition key in objectType.Keys)
            {
                if (!seen.Add(key.Name))
                {
                    diagnostics.Add(Error(
                        "STM0004",
                        $"Duplicate key name '{key.Name}' exists within object type '{objectType.Id.Value}'.",
                        ModelPath.ForKey(objectType.Id, key.Name)));
                }
            }
        }
    }

    private static void CheckKeyPropertyRefs(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            HashSet<PropertyId> propertyIds = [.. objectType.Properties.Select(static property => property.Id)];

            foreach (KeyDefinition key in objectType.Keys)
            {
                var keyPath = ModelPath.ForKey(objectType.Id, key.Name);

                foreach (PropertyRef propertyRef in key.Properties)
                {
                    if (!propertyIds.Contains(propertyRef.Id))
                    {
                        diagnostics.Add(Error(
                            "STM0005",
                            $"Key '{key.Name}' references missing property '{propertyRef.Id.Value}' on type '{objectType.Id.Value}'.",
                            ModelPath.ForPropertyReference(keyPath, propertyRef.Id)));
                    }
                }
            }
        }
    }

    private static void CheckRelationships(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (RelationshipDefinition relationship in objectType.Relationships)
            {
                var relationshipPath = ModelPath.ForRelationship(objectType.Id, relationship.Id);
                var principalResolved = TryGetObjectType(model, relationship.PrincipalType, out ObjectTypeDefinition? principalType);
                var dependentResolved = TryGetObjectType(model, relationship.DependentType, out ObjectTypeDefinition? dependentType);

                if (!principalResolved)
                {
                    diagnostics.Add(Error(
                        "STM0006",
                        $"Relationship '{relationship.Id.Value}' references missing or non-object principal type '{relationship.PrincipalType.Id.Value}'.",
                        $"{relationshipPath}/principalType"));
                }

                if (!dependentResolved)
                {
                    diagnostics.Add(Error(
                        "STM0006",
                        $"Relationship '{relationship.Id.Value}' references missing or non-object dependent type '{relationship.DependentType.Id.Value}'.",
                        $"{relationshipPath}/dependentType"));
                }

                if (principalResolved)
                {
                    CheckPropertyRefsExist(
                        principalType!,
                        relationship.PrincipalProperties,
                        $"{relationshipPath}/principalProperties",
                        relationship.Id,
                        diagnostics);
                }

                if (dependentResolved)
                {
                    CheckPropertyRefsExist(
                        dependentType!,
                        relationship.DependentProperties,
                        $"{relationshipPath}/dependentProperties",
                        relationship.Id,
                        diagnostics);
                }
            }
        }
    }

    private static void CheckPropertyRefsExist(
        ObjectTypeDefinition ownerType,
        IReadOnlyList<PropertyRef> references,
        string parentPath,
        RelationshipId relationshipId,
        List<SchemaDiagnostic> diagnostics)
    {
        HashSet<PropertyId> propertyIds = [.. ownerType.Properties.Select(static property => property.Id)];

        foreach (PropertyRef propertyRef in references)
        {
            if (!propertyIds.Contains(propertyRef.Id))
            {
                diagnostics.Add(Error(
                    "STM0007",
                    $"Relationship '{relationshipId.Value}' references missing property '{propertyRef.Id.Value}' on type '{ownerType.Id.Value}'.",
                    ModelPath.ForPropertyReference(parentPath, propertyRef.Id)));
            }
        }
    }

    private static void CheckInvalidCardinality(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (TypeDefinition type in model.Types)
        {
            if (type is ObjectTypeDefinition objectType)
            {
                foreach (PropertyDefinition property in objectType.Properties)
                {
                    Cardinality cardinality = property.Cardinality;
                    if (HasInvalidRange(cardinality.MinItems, cardinality.MaxItems) || cardinality.MinItems < 0 || cardinality.MaxItems < 0)
                    {
                        diagnostics.Add(Error(
                            "STM0008",
                            $"Property '{property.Name}' in type '{objectType.Id.Value}' has invalid cardinality bounds.",
                            ModelPath.ForProperty(objectType.Id, property.Name)));
                    }
                }
            }

            if (type is ArrayTypeDefinition arrayType && (HasInvalidRange(arrayType.MinItems, arrayType.MaxItems) || arrayType.MinItems < 0 || arrayType.MaxItems < 0))
            {
                diagnostics.Add(Error(
                    "STM0008",
                    $"Array type '{arrayType.Id.Value}' has invalid cardinality bounds.",
                    ModelPath.ForType(arrayType.Id)));
            }
        }
    }

    private static void CheckInvalidConstraintRanges(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (ObjectTypeDefinition objectType in model.Types.OfType<ObjectTypeDefinition>())
        {
            foreach (PropertyDefinition property in objectType.Properties)
            {
                var propertyPath = ModelPath.ForProperty(objectType.Id, property.Name);
                StringConstraints? stringConstraints = property.Constraints.String;
                NumericConstraints? numericConstraints = property.Constraints.Numeric;

                if (stringConstraints is not null
                    && (stringConstraints.MinLength < 0 || stringConstraints.MaxLength < 0 || HasInvalidRange(stringConstraints.MinLength, stringConstraints.MaxLength)))
                {
                    diagnostics.Add(Error(
                        "STM0009",
                        $"Property '{property.Name}' in type '{objectType.Id.Value}' has invalid string constraints.",
                        propertyPath));
                }

                if (numericConstraints is not null
                    && numericConstraints.Minimum.HasValue
                    && numericConstraints.Maximum.HasValue
                    && numericConstraints.Minimum > numericConstraints.Maximum)
                {
                    diagnostics.Add(Error(
                        "STM0010",
                        $"Property '{property.Name}' in type '{objectType.Id.Value}' has invalid numeric constraints.",
                        propertyPath));
                }
            }
        }
    }

    private static void CheckAnnotationKeys(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        CheckAnnotationBag(model.Annotations, "/", diagnostics);

        foreach (TypeDefinition type in model.Types)
        {
            var typePath = ModelPath.ForType(type.Id);
            CheckAnnotationBag(type.Annotations, typePath, diagnostics);

            if (type is ObjectTypeDefinition objectType)
            {
                foreach (PropertyDefinition property in objectType.Properties)
                {
                    CheckAnnotationBag(property.Annotations, ModelPath.ForProperty(type.Id, property.Name), diagnostics);
                }

                foreach (RelationshipDefinition relationship in objectType.Relationships)
                {
                    CheckAnnotationBag(relationship.Annotations, ModelPath.ForRelationship(type.Id, relationship.Id), diagnostics);
                }

                foreach (KeyDefinition key in objectType.Keys)
                {
                    CheckAnnotationBag(key.Annotations, ModelPath.ForKey(type.Id, key.Name), diagnostics);
                }

                foreach (ComputedMemberDefinition computedMember in objectType.ComputedMembers)
                {
                    CheckAnnotationBag(computedMember.Annotations, ModelPath.ForComputedMember(type.Id, computedMember.Name), diagnostics);
                }
            }

            if (type is EnumTypeDefinition enumType)
            {
                foreach (EnumValueDefinition value in enumType.Values)
                {
                    CheckAnnotationBag(value.Annotations, ModelPath.ForEnumValue(type.Id, value.Name), diagnostics);
                }
            }
        }
    }

    private static void CheckAnnotationBag(AnnotationBag bag, string parentPath, List<SchemaDiagnostic> diagnostics)
    {
        foreach (Annotation annotation in bag.Items)
        {
            AnnotationKeyValidationResult validation = AnnotationKeyRules.Validate(annotation.Key);
            if (!validation.IsValid || validation.NamespaceCaseChanged)
            {
                diagnostics.Add(Warning(
                    "STM0011",
                    validation.IsValid
                        ? $"Annotation key '{annotation.Key.Value}' must use the canonical reserved namespace casing '{validation.NormalizedKey.Value}'."
                        : validation.Error ?? $"Annotation key '{annotation.Key.Value}' is malformed.",
                    ModelPath.ForAnnotation(parentPath, annotation.Key)));
            }
        }
    }

    private static void CheckEnumDuplicates(TypeSchemaModel model, List<SchemaDiagnostic> diagnostics)
    {
        foreach (EnumTypeDefinition enumType in model.Types.OfType<EnumTypeDefinition>())
        {
            HashSet<string> seenNames = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seenValues = new(StringComparer.Ordinal);

            foreach (EnumValueDefinition value in enumType.Values)
            {
                if (!seenNames.Add(value.Name))
                {
                    diagnostics.Add(Error(
                        "STM0012",
                        $"Enum type '{enumType.Id.Value}' contains duplicate value name '{value.Name}'.",
                        ModelPath.ForEnumValue(enumType.Id, value.Name)));
                }

                var canonicalValue = JsonSerializer.Serialize(value.Value);
                if (!seenValues.Add(canonicalValue))
                {
                    diagnostics.Add(Error(
                        "STM0013",
                        $"Enum type '{enumType.Id.Value}' contains duplicate value payload '{canonicalValue}'.",
                        ModelPath.ForEnumValue(enumType.Id, value.Name)));
                }
            }
        }
    }

    private static bool TryGetObjectType(TypeSchemaModel model, TypeRef typeRef, out ObjectTypeDefinition? objectType)
    {
        objectType = model.TryGetType(typeRef.Id) as ObjectTypeDefinition;
        return objectType is not null;
    }

    private static bool HasInvalidRange<T>(T? minimum, T? maximum)
        where T : struct, IComparable<T>
    {
        return minimum.HasValue && maximum.HasValue && minimum.Value.CompareTo(maximum.Value) > 0;
    }

    private static SchemaDiagnostic Error(string code, string message, string? modelPath = null)
    {
        return new SchemaDiagnostic
        {
            Severity = SchemaDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Stage = SchemaDiagnosticStage.Validation,
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
            Stage = SchemaDiagnosticStage.Validation,
            ModelPath = modelPath,
        };
    }
}
