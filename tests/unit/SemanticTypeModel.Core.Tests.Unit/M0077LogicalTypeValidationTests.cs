using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;
using SemanticTypeModel.Core.Validation;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591, CA1707, IDE0055
public sealed class M0077LogicalTypeValidationTests
{
    [Test]
    public async Task Logical_type_identity_is_model_wide()
    {
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        ScalarTypeDefinition text = Scalar("Text");
        TypeSchemaModel model = Model(Object("A", Property("a", guid.Id, "CustomerId")), Object("B", Property("b", text.Id, "CustomerId")), guid, text);
        _ = await Assert.That(TypeSchemaModelValidator.Validate(model).Any(d => d.Code == "STM0014")).IsTrue();
    }

    [Test]
    public async Task Logical_type_rejects_wrong_scope_and_duplicate_metadata()
    {
        ScalarTypeDefinition guid = Scalar("Guid", ScalarKind.Guid);
        PropertyDefinition duplicate = new()
        {
            Id = new PropertyId("p"), Name = "id", Type = new TypeRef(guid.Id), Cardinality = new Cardinality { IsRequired = true },
            Constraints = new ConstraintSet(), Annotations = new AnnotationBag { Items = [Annotation("CustomerId"), Annotation("OtherId")] },
        };
        ObjectTypeDefinition owner = Object("A", duplicate) with { Annotations = new AnnotationBag { Items = [Annotation("WrongScope")] } };
        IReadOnlyList<SchemaDiagnostic> diagnostics = TypeSchemaModelValidator.Validate(Model(owner, guid));
        _ = await Assert.That(diagnostics.Count(d => d.Code == "STM0014")).IsEqualTo(2);
    }

    private static TypeSchemaModel Model(params TypeDefinition[] types)
    {
        return new TypeSchemaModel { Id = new SchemaModelId("M0077"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = new() };
    }

    private static ObjectTypeDefinition Object(string name, params PropertyDefinition[] properties)
    {
        return new ObjectTypeDefinition
        {
            Id = new TypeId(name), Name = name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new(), Properties = properties, Keys = [],
        };
    }

    private static PropertyDefinition Property(string name, TypeId typeId, string logicalName)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(name), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = true }, Constraints = new(),
            Annotations = new AnnotationBag { Items = [Annotation(logicalName)] },
        };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind = ScalarKind.String)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(name), Name = name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new(), ScalarKind = kind,
        };
    }

    private static Annotation Annotation(string value)
    {
        return new Annotation { Key = new(CoreSemanticAnnotationKeys.LogicalType), Value = value, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared };
    }
}
