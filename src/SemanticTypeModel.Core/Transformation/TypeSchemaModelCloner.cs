using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.Core.Transformation;

internal static class TypeSchemaModelCloner
{
    public static TypeSchemaModel Clone(TypeSchemaModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        List<TypeDefinition> types = [.. model.Types.Select(CloneTypeDefinition)];

        return new TypeSchemaModel
        {
            Id = model.Id,
            Types = types,
            TypesById = CreateTypeIndex(types),
            Annotations = CloneAnnotationBag(model.Annotations),
        };
    }

    public static IReadOnlyDictionary<TypeId, TypeDefinition> CreateTypeIndex(IEnumerable<TypeDefinition> types)
    {
        ArgumentNullException.ThrowIfNull(types);
        return BuildTypeIndex(types);
    }

    public static AnnotationBag CloneAnnotationBag(AnnotationBag bag)
    {
        ArgumentNullException.ThrowIfNull(bag);

        return new AnnotationBag
        {
            Items = [.. bag.Items.Select(CloneAnnotation)],
        };
    }

    public static Annotation CloneAnnotation(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        return annotation with { };
    }

    public static TypeDefinition CloneTypeDefinition(TypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type switch
        {
            ObjectTypeDefinition objectType => objectType with
            {
                Annotations = CloneAnnotationBag(objectType.Annotations),
                Properties = [.. objectType.Properties.Select(CloneProperty)],
                Keys = [.. objectType.Keys.Select(CloneKey)],
                Relationships = [.. objectType.Relationships.Select(CloneRelationship)],
                Composition = objectType.Composition with { AllOf = [.. objectType.Composition.AllOf] },
                Semantics = objectType.Semantics with { },
                ComputedMembers = [.. objectType.ComputedMembers.Select(CloneComputedMember)],
            },
            ScalarTypeDefinition scalarType => scalarType with
            {
                Annotations = CloneAnnotationBag(scalarType.Annotations),
                Precision = scalarType.Precision is null ? null : scalarType.Precision with { },
            },
            EnumTypeDefinition enumType => enumType with
            {
                Annotations = CloneAnnotationBag(enumType.Annotations),
                Values = [.. enumType.Values.Select(CloneEnumValue)],
            },
            ArrayTypeDefinition arrayType => arrayType with
            {
                Annotations = CloneAnnotationBag(arrayType.Annotations),
            },
            DictionaryTypeDefinition dictionaryType => dictionaryType with
            {
                Annotations = CloneAnnotationBag(dictionaryType.Annotations),
            },
            UnionTypeDefinition unionType => unionType with
            {
                Annotations = CloneAnnotationBag(unionType.Annotations),
                Options = [.. unionType.Options],
                Discriminator = unionType.Discriminator is null
                    ? null
                    : unionType.Discriminator with
                    {
                        Mapping = new Dictionary<string, TypeRef>(unionType.Discriminator.Mapping, StringComparer.Ordinal),
                    },
            },
            IntersectionTypeDefinition intersectionType => intersectionType with
            {
                Annotations = CloneAnnotationBag(intersectionType.Annotations),
                Members = [.. intersectionType.Members],
            },
            ReferenceTypeDefinition referenceType => referenceType with
            {
                Annotations = CloneAnnotationBag(referenceType.Annotations),
            },
            _ => type with
            {
                Annotations = CloneAnnotationBag(type.Annotations),
            },
        };
    }

    private static PropertyDefinition CloneProperty(PropertyDefinition property)
    {
        return property with
        {
            Cardinality = property.Cardinality with { },
            Constraints = CloneConstraintSet(property.Constraints),
            Annotations = CloneAnnotationBag(property.Annotations),
        };
    }

    private static KeyDefinition CloneKey(KeyDefinition key)
    {
        return key with
        {
            Properties = [.. key.Properties],
            Annotations = CloneAnnotationBag(key.Annotations),
        };
    }

    private static RelationshipDefinition CloneRelationship(RelationshipDefinition relationship)
    {
        return relationship with
        {
            PrincipalProperties = [.. relationship.PrincipalProperties],
            DependentProperties = [.. relationship.DependentProperties],
            Annotations = CloneAnnotationBag(relationship.Annotations),
        };
    }

    private static ComputedMemberDefinition CloneComputedMember(ComputedMemberDefinition computedMember)
    {
        return computedMember with
        {
            Expression = computedMember.Expression with { },
            Annotations = CloneAnnotationBag(computedMember.Annotations),
        };
    }

    private static EnumValueDefinition CloneEnumValue(EnumValueDefinition enumValue)
    {
        return enumValue with
        {
            Annotations = CloneAnnotationBag(enumValue.Annotations),
        };
    }

    private static ConstraintSet CloneConstraintSet(ConstraintSet constraints)
    {
        return constraints with
        {
            String = constraints.String is null ? null : constraints.String with { },
            Numeric = constraints.Numeric is null ? null : constraints.Numeric with { },
            Array = constraints.Array is null ? null : constraints.Array with { },
            Object = constraints.Object is null ? null : constraints.Object with { },
            Custom = [.. constraints.Custom.Select(CloneCustomConstraint)],
        };
    }

    private static CustomConstraint CloneCustomConstraint(CustomConstraint customConstraint)
    {
        return customConstraint with
        {
            Annotations = CloneAnnotationBag(customConstraint.Annotations),
        };
    }

    private static Dictionary<TypeId, TypeDefinition> BuildTypeIndex(IEnumerable<TypeDefinition> types)
    {
        Dictionary<TypeId, TypeDefinition> byId = [];

        foreach (TypeDefinition type in types)
        {
            if (!byId.ContainsKey(type.Id))
            {
                byId[type.Id] = type;
            }
        }

        return byId;
    }
}
