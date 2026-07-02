using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.EFCore.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class EfCoreNullableValueTypeProjectionTests
{
    private static readonly AnnotationBag Empty = new();

    [Test]
    public async Task Nullable_value_type_properties_use_nullable_clr_types_in_projection_and_modelbuilder()
    {
        TypeSchemaModel model = BuildModel();
        EfModelDefinition projection = new EfCoreModelProjection(new EfCoreProjectionOptions { ProjectUnannotatedObjectsAsEntities = true }).Project(model, new SchemaProjectionContext { Target = ProjectionTarget.EfCore });
        var builder = new ModelBuilder(new ConventionSet());
        EfCoreModelBuilderProjectionResult applied = builder.ApplySemanticTypeModel(model, o => o.ProjectUnannotatedObjectsAsEntities = true);

        EfEntityTypeDefinition entity = projection.EntityTypes.Single(e => e.Name == "NullableMatrix");
        foreach ((string Name, Type Type) expected in new[]
        {
            ("OptionalInt", typeof(int?)),
            ("OptionalLong", typeof(long?)),
            ("OptionalDecimal", typeof(decimal?)),
            ("OptionalBool", typeof(bool?)),
            ("OptionalDateTime", typeof(DateTime?)),
            ("OptionalDateTimeOffset", typeof(DateTimeOffset?)),
            ("OptionalGuid", typeof(Guid?)),
            ("OptionalStatus", typeof(long?)),
        })
        {
            EfPropertyDefinition property = entity.Properties.Single(p => p.Name == expected.Name);
            _ = await Assert.That(property.IsNullable).IsTrue();
            _ = await Assert.That(property.ClrType).IsEqualTo(expected.Type);
            IMutableProperty runtimeProperty = builder.Model.FindEntityType("NullableMatrix")!.FindProperty(expected.Name)!;
            _ = await Assert.That(runtimeProperty.IsNullable).IsTrue();
            _ = await Assert.That(runtimeProperty.ClrType).IsEqualTo(expected.Type);
        }

        _ = await Assert.That(entity.Properties.Single(p => p.Name == "RequiredInt").ClrType).IsEqualTo(typeof(int));
        _ = await Assert.That(entity.Properties.Single(p => p.Name == "RequiredText").ClrType).IsEqualTo(typeof(string));
        _ = await Assert.That(entity.Properties.Single(p => p.Name == "OptionalText").ClrType).IsEqualTo(typeof(string));
        _ = await Assert.That(applied.Diagnostics).Count().IsEqualTo(projection.Diagnostics.Count);
    }

    private static TypeSchemaModel BuildModel()
    {
        var types = new List<TypeDefinition>
        {
            Scalar("Int32", ScalarKind.Integer, typeof(int)),
            Scalar("Int64", ScalarKind.Integer, typeof(long)),
            Scalar("Decimal", ScalarKind.Decimal, typeof(decimal)),
            Scalar("Boolean", ScalarKind.Boolean, typeof(bool)),
            Scalar("DateTime", ScalarKind.DateTime, typeof(DateTime)),
            Scalar("DateTimeOffset", ScalarKind.DateTimeOffset, typeof(DateTimeOffset)),
            Scalar("Guid", ScalarKind.Guid, typeof(Guid)),
            Scalar("String", ScalarKind.String, typeof(string)),
            new EnumTypeDefinition { Id = new TypeId("MatrixStatus"), Name = "MatrixStatus", Kind = TypeKind.Enum, Nullability = Nullability.NonNullable, Annotations = Clr(typeof(MatrixStatus)), Values = [], StorageKind = EnumStorageKind.Integer },
        };
        var entity = new ObjectTypeDefinition
        {
            Id = new TypeId("NullableMatrix"),
            Name = "NullableMatrix",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Empty,
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Prop("Id", "Int32", true, false, typeof(int)),
                Prop("OptionalInt", "Int32", false, true, typeof(int)), Prop("OptionalLong", "Int64", false, true, typeof(long)), Prop("OptionalDecimal", "Decimal", false, true, typeof(decimal)), Prop("OptionalBool", "Boolean", false, true, typeof(bool)),
                Prop("OptionalDateTime", "DateTime", false, true, typeof(DateTime)), Prop("OptionalDateTimeOffset", "DateTimeOffset", false, true, typeof(DateTimeOffset)), Prop("OptionalGuid", "Guid", false, true, typeof(Guid)), Prop("OptionalStatus", "MatrixStatus", false, true, null, true),
                Prop("RequiredInt", "Int32", true, false, typeof(int)), Prop("RequiredText", "String", true, false), Prop("OptionalText", "String", false, true),
            ],
            Keys = [new KeyDefinition { Name = "PK_NullableMatrix", Kind = KeyKind.Primary, Properties = [new PropertyRef(new PropertyId("Id"))], Annotations = Empty }],
            Relationships = [],
        };
        types.Add(entity);
        return new TypeSchemaModel { Id = new SchemaModelId("NullableMatrixModel"), Types = types, TypesById = types.ToDictionary(t => t.Id), Annotations = Empty };
    }

    private static ScalarTypeDefinition Scalar(string id, ScalarKind kind, Type clr)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = Clr(clr), ScalarKind = kind };
    }

    private static PropertyDefinition Prop(string name, string type, bool required, bool nullable, Type? clr = null, bool numericEnum = false)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(new TypeId(type)), Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = numericEnum ? EnumNumeric() : clr is null ? Empty : Clr(clr) };
    }

    private static AnnotationBag Clr(Type type)
    {
        return new() { Items = [new Annotation { Key = new AnnotationKey("dotnet.clrType"), Value = type.AssemblyQualifiedName, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }] };
    }

    private static AnnotationBag EnumNumeric()
    {
        return new() { Items = [new Annotation { Key = new AnnotationKey("efCore.enumStorage"), Value = "Numeric", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared }] };
    }

    private enum MatrixStatus { Open, Closed }
}
#pragma warning restore CS1591
