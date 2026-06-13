using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SemanticTypeModel.Abstractions.Canonical;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.EFCore;

TypeSchemaModel model = CreateModel();
SemanticDerivationResult<EfCoreSemanticModel> derived = model.DeriveEfCoreModel(options => options.Projection = options.Projection with { ProjectUnannotatedObjectsAsEntities = true });

var modelBuilder = new ModelBuilder(new ConventionSet());
modelBuilder.ApplyEfCoreSemanticModel(derived.Model, defaultSchema: "sample");

Console.WriteLine($"root: {model.Id.Value}");
Console.WriteLine($"derivation diagnostics: {derived.Diagnostics.Count}");
Console.WriteLine($"modelBuilder entities: {derived.Model.EntityTypes.Count}");
Console.WriteLine($"trace steps: {derived.Trace.Entries.Count}");

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
