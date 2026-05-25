using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.EFCore;
using SemanticTypeModel.JsonSchema.Import;

const string schema = """
{
  "$id": "Customer",
  "type": "object",
  "properties": {
    "id": { "type": "integer" },
    "name": { "type": "string", "maxLength": 100 }
  },
  "required": ["id", "name"],
  "x-efCore-entity": "true"
}
""";

var imported = JsonSchemaImporter.Import(schema);
var adapted = LegacyTypeSchemaModelAdapter.Adapt(imported.Model);
var projection = new EfCoreModelProjection(new EfCoreProjectionOptions { ProjectUnannotatedObjectsAsEntities = true });
var context = new SchemaProjectionContext { Target = ProjectionTarget.EfCore };
EfModelDefinition model = projection.Project(adapted.Model!, context);

Console.WriteLine($"entities: {model.EntityTypes.Count}");
Console.WriteLine($"diagnostics: {model.Diagnostics.Count}");
