using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707

public sealed class M0066SystemTextJsonFidelityTests
{
    [Test]
    public async Task Derivation_should_expose_imported_contract_metadata_without_inferring_wire_shapes()
    {
        var property = new PropertyDefinition
        {
            Id = new PropertyId("M0066Contract.Value"),
            Name = "value",
            Type = new TypeRef(new TypeId("String")),
            Cardinality = new Cardinality { IsRequired = true },
            Constraints = new ConstraintSet(),
            Annotations = Annotations(
                (SystemTextJsonAnnotationNames.PropertyName, "wire_value"),
                (SystemTextJsonAnnotationNames.Ignore, "true"),
                (SystemTextJsonAnnotationNames.IgnoreCondition, "WhenWritingDefault"),
                (SystemTextJsonAnnotationNames.Include, "true"),
                (SystemTextJsonAnnotationNames.Converter, "global::Example.Converter"),
                (SystemTextJsonAnnotationNames.NumberHandling, "AllowReadingFromString"),
                (SystemTextJsonAnnotationNames.Required, "true"),
                (SystemTextJsonAnnotationNames.ExtensionData, "true"),
                (SystemTextJsonAnnotationNames.ObjectCreationHandling, "Populate"),
                (SystemTextJsonAnnotationNames.UnmappedMemberHandling, "Disallow"),
                (SystemTextJsonAnnotationNames.Polymorphism, "true")),
        };
        var type = new ObjectTypeDefinition
        {
            Id = new TypeId("Contract"),
            Name = "Contract",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotations(
                (SystemTextJsonAnnotationNames.ObjectCreationHandling, "Populate"),
                (SystemTextJsonAnnotationNames.UnmappedMemberHandling, "Disallow"),
                (SystemTextJsonAnnotationNames.Polymorphism, "true")),
            Properties = [property],
            Keys = [],
        };
        var scalar = new ScalarTypeDefinition
        {
            Id = new TypeId("String"),
            Name = "String",
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            ScalarKind = ScalarKind.String,
            Annotations = new AnnotationBag(),
        };
        TypeDefinition[] types = [type, scalar];
        var model = new TypeSchemaModel
        {
            Id = new SchemaModelId(type.Id.Value),
            Types = types,
            TypesById = types.ToDictionary(static item => item.Id),
            Annotations = new AnnotationBag(),
        };

        SystemTextJsonSemanticModel projected = model.DeriveSystemTextJsonModel().Model;
        SystemTextJsonTypeDefinition projectedType = projected.TypesById[type.Id];
        SystemTextJsonPropertyDefinition projectedProperty = projectedType.Properties.Single();

        _ = await Assert.That(projectedProperty.SystemTextJsonPropertyName).IsEqualTo("wire_value");
        _ = await Assert.That(projectedProperty.IsIgnored).IsTrue();
        _ = await Assert.That(projectedProperty.IgnoreCondition).IsEqualTo("WhenWritingDefault");
        _ = await Assert.That(projectedProperty.IsIncluded).IsTrue();
        _ = await Assert.That(projectedProperty.Converter).IsEqualTo("global::Example.Converter");
        _ = await Assert.That(projectedProperty.NumberHandling).IsEqualTo("AllowReadingFromString");
        _ = await Assert.That(projectedProperty.IsRequired).IsTrue();
        _ = await Assert.That(projectedProperty.IsExtensionData).IsTrue();
        _ = await Assert.That(projectedProperty.ObjectCreationHandling).IsEqualTo("Populate");
        _ = await Assert.That(projectedProperty.UnmappedMemberHandling).IsEqualTo("Disallow");
        _ = await Assert.That(projectedProperty.HasPolymorphism).IsTrue();
        _ = await Assert.That(projectedType.ObjectCreationHandling).IsEqualTo("Populate");
        _ = await Assert.That(projectedType.UnmappedMemberHandling).IsEqualTo("Disallow");
        _ = await Assert.That(projectedType.HasPolymorphism).IsTrue();
        _ = await Assert.That(projected.Diagnostics.Any(static diagnostic => diagnostic.Code == "STJ102")).IsTrue();
    }

    private static AnnotationBag Annotations(params (string Key, object Value)[] values) => new()
    {
        Items = [.. values.Select(static value => new Annotation
        {
            Key = new AnnotationKey(value.Key),
            Value = value.Value,
            Scope = AnnotationScope.Member,
            Source = AnnotationSource.Imported,
        })],
    };
}
