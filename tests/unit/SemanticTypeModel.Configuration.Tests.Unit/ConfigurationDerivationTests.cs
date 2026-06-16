#pragma warning disable CS1591
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Semantics;

namespace SemanticTypeModel.Configuration.Tests.Unit;

public sealed class ConfigurationDerivationTests
{
    [Test]
    public async Task DeriveConfigurationModelReadsSectionAndRequiredWhen()
    {
        TypeSchemaModel model = BuildModel(Scalar("String"), Scalar("Provider"), Object("ColdStorageOptions", [
            Property("Provider", new TypeId("Provider")),
            Property("TargetFilePath", new TypeId("String"), Annotations(
                Annotation(CoreSemanticAnnotationKeys.RequiredWhen, "true"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenSource, "Provider"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenOperator, "equals"),
                Annotation(CoreSemanticAnnotationKeys.RequiredWhenValue, "File")))
        ], Annotations(
            Annotation("schema.role", "Configuration"),
            Annotation(ConfigurationAnnotationKeys.SectionName, "ColdStorage"),
            Annotation(ConfigurationAnnotationKeys.ValidateOnStart, "true"),
            Annotation(ConfigurationAnnotationKeys.ValidateDataAnnotations, "true"))));

        ConfigurationSemanticModel configuration = model.DeriveConfigurationModel();

        _ = await Assert.That(configuration.ConfigurationTypes).Count().IsEqualTo(1);
        ConfigurationType options = configuration.ConfigurationTypes.Single();
        _ = await Assert.That(options.Section).IsEqualTo("ColdStorage");
        _ = await Assert.That(options.ValidateOnStart).IsTrue();
        _ = await Assert.That(options.RequiredWhenConstraints.Single().TargetProperty).IsEqualTo("TargetFilePath");
        _ = await Assert.That(configuration.Inspect()).Contains("requiredWhen target=TargetFilePath source=Provider operator=equals value=File");
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new() { Id = new SchemaModelId("ColdStorageOptions"), Types = types, TypesById = types.ToDictionary(static t => t.Id, static t => t), Annotations = new AnnotationBag() };
    }

    private static ScalarTypeDefinition Scalar(string id)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.String };
    }

    private static ObjectTypeDefinition Object(string id, IReadOnlyList<PropertyDefinition> properties, AnnotationBag annotations)
    {
        return new() { Id = new TypeId(id), Name = id, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = annotations, Properties = properties, Keys = [], Relationships = [] };
    }

    private static PropertyDefinition Property(string name, TypeId type, AnnotationBag? annotations = null)
    {
        return new() { Id = new PropertyId(name), Name = name, Type = new TypeRef(type), Cardinality = new Cardinality(), Mutability = Mutability.InitOnly, Constraints = new ConstraintSet(), Annotations = annotations ?? new AnnotationBag() };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new() { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new() { Key = new AnnotationKey(key), Value = value, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared };
    }
}

#pragma warning restore CS1591
