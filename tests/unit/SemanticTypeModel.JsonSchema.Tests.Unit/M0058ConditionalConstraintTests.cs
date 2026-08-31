using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Transformation;
using SemanticTypeModel.JsonSchema.Derivation;
using SemanticTypeModel.JsonSchema.Domain;
using SemanticTypeModel.JsonSchema.Export;
using Intake = SemanticTypeModel.TestModels.ModelA.Intake;
using ModelAGenerated = SemanticTypeModel.TestModels.ModelA.Generated;

namespace SemanticTypeModel.JsonSchema.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names use underscores for readability.")]
public sealed class M0058ConditionalConstraintTests
{
    [Test]
    public async Task M0058_import_conditions_emit_deterministic_enum_constants()
    {
        TypeSchemaModel model = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition import = model.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(Intake.ImportSpecification));

        ConditionalConstraint[] constraints = [.. import.Properties.SelectMany(property => property.Constraints.Conditional)];
        _ = await Assert.That(constraints.All(constraint => constraint.Literal.Kind == SemanticLiteralKind.EnumMember)).IsTrue();
        _ = await Assert.That(constraints.Select(constraint => constraint.Literal.EnumMemberName!)).IsEquivalentTo(["CsvFile", "XmlFile", "WebService1", "WebService2"]);
        _ = await Assert.That(model.GetType(constraints[0].SourceTypeId)).IsTypeOf<EnumTypeDefinition>();

        JsonSchemaExportResult export = JsonSchemaExporter.Export(model.DeriveJsonSchemaModel().Model);
        JsonElement allOf = export.Document.RootElement.GetProperty("$defs").GetProperty(import.Id.Value).GetProperty("allOf");
        _ = await Assert.That(allOf.GetArrayLength()).IsEqualTo(4);
        _ = await Assert.That(allOf.EnumerateArray().Select(item => item.GetProperty("if").GetProperty("properties").GetProperty("ImportType").GetProperty("const").GetString()!)).IsEquivalentTo(["CsvFile", "XmlFile", "WebService1", "WebService2"]);
    }

    [Test]
    public async Task ConditionalConstraint_unsupported_operator_emits_diagnostic_instead_of_being_dropped()
    {
        TypeSchemaModel source = ModelAGenerated.ModelASemanticTypeModel.Create();
        ObjectTypeDefinition original = source.Types.OfType<ObjectTypeDefinition>().Single(type => type.Name == nameof(Intake.ImportSpecification));
        PropertyDefinition csv = original.Properties.Single(property => property.Name == nameof(Intake.ImportSpecification.CsvSource));
        PropertyDefinition changedCsv = csv with { Constraints = csv.Constraints with { Conditional = [csv.Constraints.Conditional.Single() with { Operator = ConditionalConstraintOperator.NotEquals }] } };
        ObjectTypeDefinition changed = original with { Properties = [.. original.Properties.Select(property => property.Id == csv.Id ? changedCsv : property)] };
        TypeDefinition[] types = [.. source.Types.Select(type => type.Id == original.Id ? changed : type)];
        TypeSchemaModel model = new() { Id = source.Id, Types = types, TypesById = types.ToDictionary(type => type.Id), Annotations = source.Annotations };

        SemanticDerivationResult<JsonSchemaSemanticModel> result = model.DeriveJsonSchemaModel();

        _ = await Assert.That(result.Diagnostics.Any(diagnostic => diagnostic.Code == "JSONSCHEMA_CONDITIONAL_OPERATOR_UNSUPPORTED")).IsTrue();
    }
}
#pragma warning restore CS1591
