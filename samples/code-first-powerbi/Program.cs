using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.PowerBI;

TypeSchemaModel model = CreateModel();
var result = model.DerivePowerBiModel(options =>
{
    _ = options.UseDefaultTransformations();
    options.Projection.ProjectUnannotatedObjectsAsTables = true;
});

Console.WriteLine($"root: {model.Id.Value}");
Console.WriteLine($"tables: {result.Model.Tables.Count}");
Console.WriteLine($"calculated tables: {result.Model.CalculatedTables.Count}");
Console.WriteLine($"diagnostics: {result.Diagnostics.Count}");
Console.WriteLine(PowerBiLocalMetadataExporter.Inspect(result.Model));

static TypeSchemaModel CreateModel()
{
    ScalarTypeDefinition stringType = new() { Id = new TypeId("String"), Name = "String", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.String };
    ScalarTypeDefinition decimalType = new() { Id = new TypeId("Decimal"), Name = "Decimal", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.Decimal };
    var sales = new ObjectTypeDefinition { Id = new TypeId("SalesRecord"), Name = "SalesRecord", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), Properties = [Property("id", "SalesRecordId", stringType.Id), Property("amount", "SalesRecordAmount", decimalType.Id)], Keys = [], Relationships = [] };
    return new TypeSchemaModel { Id = new SchemaModelId(sales.Id.Value), Types = [sales, stringType, decimalType], TypesById = new Dictionary<TypeId, TypeDefinition> { [sales.Id] = sales, [stringType.Id] = stringType, [decimalType.Id] = decimalType }, Annotations = new AnnotationBag() };
}
static PropertyDefinition Property(string name, string id, TypeId typeId)
{
    return new PropertyDefinition { Id = new PropertyId(id), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = true }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = new AnnotationBag() };
}
