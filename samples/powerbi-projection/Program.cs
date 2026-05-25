using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Runtime;
using SemanticTypeModel.JsonSchema.Import;
using SemanticTypeModel.PowerBI;

const string schema = """
{
  "$id": "SalesModel",
  "type": "object",
  "properties": {
    "salesKey": { "type": "integer" },
    "amount": { "type": "number" }
  },
  "required": ["salesKey"],
  "x-stm-semantic-role": "Fact",
  "x-powerBi-tableRole": "Fact"
}
""";

var imported = JsonSchemaImporter.Import(schema);
var adapted = LegacyTypeSchemaModelAdapter.Adapt(imported.Model);
var projection = new PowerBiTabularProjection(new PowerBiProjectionOptions { ProjectUnannotatedObjectsAsTables = true });
var context = new SchemaProjectionContext { Target = ProjectionTarget.PowerBi };
TabularModelDefinition model = projection.Project(adapted.Model!, context);

Console.WriteLine($"tables: {model.Tables.Count}");
Console.WriteLine($"diagnostics: {model.Diagnostics.Count}");
