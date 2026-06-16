using Microsoft.Extensions.DependencyInjection;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Abstractions.Runtime;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema;

// Consumers register runtime canonical semantic model creation, transformations, and projections in their DI container.
using ServiceProvider serviceProvider = new ServiceCollection()
    .AddSemanticTypeModel(CreateModel)
    .AddSemanticTypeModelTransformation<ValidateModelTransformation>()
    .AddSemanticTypeModelJsonSchema()
    .BuildServiceProvider();

ITypeSchemaModelService modelService = serviceProvider.GetRequiredService<ITypeSchemaModelService>();
TypeSchemaModelResult modelResult = await modelService.GetModelAsync();
SchemaProjectionResult<JsonSchemaExportResult> projection = await serviceProvider
    .GetRequiredService<ITypeSchemaProjectionService<JsonSchemaExportResult>>()
    .ProjectAsync();

Console.WriteLine($"runtime diagnostics: {modelResult.Diagnostics.Count}");
Console.WriteLine($"projection diagnostics: {projection.Diagnostics.Count}");
Console.WriteLine(projection.Projection!.Document.RootElement.GetRawText());

static TypeSchemaModel CreateModel()
{
    ScalarTypeDefinition stringType = new() { Id = new TypeId("String"), Name = "String", Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), ScalarKind = ScalarKind.String };
    var order = new ObjectTypeDefinition { Id = new TypeId("Order"), Name = "Order", Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = new AnnotationBag(), Properties = [Property("orderId", "OrderId", stringType.Id)], Keys = [], Relationships = [] };
    return new TypeSchemaModel { Id = new SchemaModelId(order.Id.Value), Types = [order, stringType], TypesById = new Dictionary<TypeId, TypeDefinition> { [order.Id] = order, [stringType.Id] = stringType }, Annotations = new AnnotationBag() };
}

static PropertyDefinition Property(string name, string id, TypeId typeId)
{
    return new PropertyDefinition { Id = new PropertyId(id), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = true }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = new AnnotationBag() };
}
