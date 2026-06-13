using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Export;

TypeSchemaModel canonicalModel = CreateModel();

var jsonSchemaModel = canonicalModel.DeriveJsonSchemaModel(options => _ = options.UseDefaultTransformations());
JsonSchemaExportResult exported = JsonSchemaExporter.Export(jsonSchemaModel.Model);

string outputDirectory = Path.Combine("artifacts", "samples", "code-first-json-schema");
Directory.CreateDirectory(outputDirectory);
string outputPath = Path.Combine(outputDirectory, "customer.schema.json");
File.WriteAllText(outputPath, exported.Document.RootElement.GetRawText());

Console.WriteLine($"root: {canonicalModel.Id.Value}");
Console.WriteLine($"types: {canonicalModel.Types.Count}");
Console.WriteLine($"derivation diagnostics: {jsonSchemaModel.Diagnostics.Count}");
Console.WriteLine($"derivation trace: {string.Join(", ", jsonSchemaModel.Trace.Entries.Select(static entry => entry.TransformationId))}");
Console.WriteLine($"export diagnostics: {exported.Diagnostics.Count}");
Console.WriteLine($"artifacts: {outputPath}");

static TypeSchemaModel CreateModel()
{
    ScalarTypeDefinition stringType = new() { Id = new TypeId("String"), Name = "String", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.String };
    var customer = new ObjectTypeDefinition { Id = new TypeId("Customer"), Name = "Customer", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), Properties = [Property("id", "CustomerId", stringType.Id), Property("name", "CustomerName", stringType.Id)], Keys = [], Relationships = [] };
    return new TypeSchemaModel { Id = new SchemaModelId(customer.Id.Value), Types = [customer, stringType], TypesById = new Dictionary<TypeId, TypeDefinition> { [customer.Id] = customer, [stringType.Id] = stringType }, Annotations = new AnnotationBag() };
}

static PropertyDefinition Property(string name, string id, TypeId typeId)
{
    return new PropertyDefinition { Id = new PropertyId(id), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = true }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = new AnnotationBag() };
}
