using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.JsonSchema.Export;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable CA1707

public sealed class M0062SemanticAnnotationsTests
{
    [Test]
    public async Task Export_should_preserve_declared_semantics_in_x_stm()
    {
        var annotations = new AnnotationBag
        {
            Items =
            [
                new Annotation { Key = new AnnotationKey("ui.widget"), Value = "custom", Scope = AnnotationScope.Member, Source = AnnotationSource.Declared },
                new Annotation { Key = new AnnotationKey("ui.customThing"), Value = 42, Scope = AnnotationScope.Member, Source = AnnotationSource.Declared },
            ],
        };
        var property = new PropertyDefinition
        {
            Id = new PropertyId("Specification.Cache"),
            Name = "cache",
            Type = new TypeRef(new TypeId("String")),
            Cardinality = new Cardinality(),
            Mutability = SemanticMutability.Mutable,
            UserDescription = "User cache.",
            TechnicalDescription = "Invalidated by the worker.",
            Constraints = new ConstraintSet(),
            Annotations = annotations,
        };
        var specification = new ObjectTypeDefinition
        {
            Id = new TypeId("Specification"),
            Name = "Specification",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            UserDescription = "User specification.",
            TechnicalDescription = "Internal specification contract.",
            Annotations = new AnnotationBag(),
            Properties = [property],
            Keys = [new KeyDefinition { Name = "Primary", Kind = KeyKind.Primary, Properties = [new PropertyRef(property.Id)], IsGenerated = true, Annotations = new AnnotationBag() }],
            Semantics = new EntitySemantics { Role = EntityRole.Entity, IsAggregateRoot = true },
            Mutability = SemanticMutability.Immutable,
        };
        var scalar = new ScalarTypeDefinition { Id = new TypeId("String"), Name = "String", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = ScalarKind.String, Unit = "ms", Annotations = new AnnotationBag() };
        TypeDefinition[] types = [specification, scalar];
        var model = new TypeSchemaModel { Id = new SchemaModelId("Specification"), Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = new AnnotationBag() };

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);
        JsonElement root = result.Document.RootElement;

        _ = await Assert.That(root.GetProperty("description").GetString()).IsEqualTo("User specification.");
        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("technicalDescription").GetString()).IsEqualTo("Internal specification contract.");
        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("mutability").GetString()).IsEqualTo("immutable");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("cache").GetProperty("x-stm").GetProperty("mutability").GetString()).IsEqualTo("mutable");
        _ = await Assert.That(root.GetProperty("properties").GetProperty("cache").GetProperty("x-stm").GetProperty("ui").GetProperty("customThing").GetInt32()).IsEqualTo(42);
        _ = await Assert.That(root.GetProperty("x-stm").GetProperty("keys")[0].GetProperty("properties")[0].GetString()).IsEqualTo("cache");
        _ = await Assert.That(root.GetProperty("$defs").GetProperty("String").GetProperty("x-stm").GetProperty("unit").GetString()).IsEqualTo("ms");
    }

    [Test]
    public async Task Export_can_omit_semantic_annotations()
    {
        var scalar = new ScalarTypeDefinition { Id = new TypeId("Value"), Name = "Value", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = ScalarKind.String, Unit = "ms", Annotations = new AnnotationBag() };
        var model = new TypeSchemaModel { Id = new SchemaModelId("Value"), Types = [scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [scalar.Id] = scalar }, Annotations = new AnnotationBag() };

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model, new JsonSchemaExportOptions { IncludeSemanticAnnotations = false });

        _ = await Assert.That(result.Document.RootElement.TryGetProperty("x-stm", out _)).IsFalse();
    }

    [Test]
    public async Task Effective_mutability_prefers_property_declaration()
    {
        var property = new PropertyDefinition { Id = new PropertyId("T.P"), Name = "p", Type = new TypeRef(new TypeId("String")), Cardinality = new Cardinality(), Mutability = SemanticMutability.Mutable, Constraints = new ConstraintSet(), Annotations = new AnnotationBag() };
        var type = new ObjectTypeDefinition { Id = new TypeId("T"), Name = "T", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Properties = [property], Keys = [], Mutability = SemanticMutability.Immutable, Annotations = new AnnotationBag() };
        _ = await Assert.That(type.GetEffectiveMutability(property)).IsEqualTo(SemanticMutability.Mutable);
    }

    [Test]
    public async Task Invalid_ui_value_produces_diagnostic_instead_of_throwing()
    {
        var cyclic = new CyclicValue();
        cyclic.Self = cyclic;
        var scalar = new ScalarTypeDefinition
        {
            Id = new TypeId("Value"),
            Name = "Value",
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            ScalarKind = ScalarKind.String,
            Annotations = new AnnotationBag { Items = [new Annotation { Key = new AnnotationKey("ui.cyclic"), Value = cyclic, Scope = AnnotationScope.Type, Source = AnnotationSource.Declared }] },
        };
        var model = new TypeSchemaModel { Id = new SchemaModelId("Value"), Types = [scalar], TypesById = new Dictionary<TypeId, TypeDefinition> { [scalar.Id] = scalar }, Annotations = new AnnotationBag() };

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "JSONSCHEMA_UI_VALUE_NOT_JSON_COMPATIBLE")).IsTrue();
    }

    [Test]
    public async Task Reference_type_preserves_technical_description_in_x_stm()
    {
        var scalar = new ScalarTypeDefinition { Id = new TypeId("String"), Name = "String", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, ScalarKind = ScalarKind.String, Annotations = new AnnotationBag() };
        var reference = new ReferenceTypeDefinition { Id = new TypeId("Alias"), Name = "Alias", Kind = TypeKind.Reference, Nullability = Nullability.NonNullable, Target = new TypeRef(scalar.Id), TechnicalDescription = "Technical alias.", Annotations = new AnnotationBag() };
        TypeDefinition[] types = [reference, scalar];
        var model = new TypeSchemaModel { Id = new SchemaModelId("Alias"), Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = new AnnotationBag() };

        JsonSchemaExportResult result = JsonSchemaExporter.Export(model);

        _ = await Assert.That(result.Document.RootElement.GetProperty("x-stm").GetProperty("technicalDescription").GetString()).IsEqualTo("Technical alias.");
    }

    private sealed class CyclicValue
    {
        public CyclicValue? Self { get; set; }
    }

}
