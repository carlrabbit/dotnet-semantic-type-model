using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;
using SemanticTypeModel.JsonSchema.Runtime;

ScalarTypeDefinition stringType = new()
{
    Id = new TypeId("String"),
    Name = "String",
    Kind = TypeKind.Scalar,
    Nullability = Nullability.NonNullable,
    Annotations = new AnnotationBag(),
    ScalarKind = ScalarKind.String,
};

var customer = new ObjectTypeDefinition
{
    Id = new TypeId("Customer"),
    Name = "Customer",
    Kind = TypeKind.Object,
    Nullability = Nullability.NonNullable,
    Annotations = new AnnotationBag(),
    Properties =
    [
        Property("id", "CustomerId", stringType.Id),
        Property("name", "CustomerName", stringType.Id),
    ],
    Keys = [],
    Relationships = [],
};

var model = new TypeSchemaModel
{
    Id = new SchemaModelId(customer.Id.Value),
    Types = [customer, stringType],
    TypesById = new Dictionary<TypeId, TypeDefinition>
    {
        [customer.Id] = customer,
        [stringType.Id] = stringType,
    },
    Annotations = new AnnotationBag(),
};

// Transformations are deterministic validation/normalization steps before projection.
SchemaTransformationPipeline pipeline = SchemaTransformationPipeline.Create()
    .Use(new NormalizeNamesTransformation())
    .Use(new NormalizeAnnotationsTransformation())
    .Use(new ValidateModelTransformation());

SchemaPipelineResult transformed = await pipeline.RunAsync(model);
var projectionContext = new SchemaProjectionContext { Target = ProjectionTarget.JsonSchema };
var exporter = new JsonSchemaRuntimeProjection();
JsonSchemaExportResult exported = exporter.Project(transformed.Model, projectionContext);
JsonSchemaExportResult domainExported = JsonSchemaExporter.Export(model.DeriveJsonSchemaModel().Model);

string outputDirectory = Path.Combine("artifacts", "samples", "json-schema-roundtrip");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"transform diagnostics: {transformed.Diagnostics.Count}");
Console.WriteLine($"projection diagnostics: {projectionContext.Diagnostics.Count}");
Console.WriteLine($"domain export bytes: {domainExported.Document.RootElement.GetRawText().Length}");
Console.WriteLine($"artifacts: {outputPath}");

static PropertyDefinition Property(string name, string id, TypeId typeId)
{
    return new PropertyDefinition
    {
        Id = new PropertyId(id),
        Name = name,
        Type = new TypeRef(typeId),
        Cardinality = new Cardinality { IsRequired = true },
        Mutability = Mutability.Immutable,
        Constraints = new ConstraintSet(),
        Annotations = new AnnotationBag(),
    };
}
