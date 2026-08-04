using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Validation;
using SemanticTypeModel.RealWorldFixtures;
using Intake = SemanticTypeModel.RealWorldFixtures.OrderIntakeSpecificationModel;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names use underscores for readability.")]
public sealed class M0058ConditionalConstraintValidationTests
{
    [Test]
    public async Task M0058_shared_import_fixture_is_valid_and_preserves_typed_enum_constraints()
    {
        TypeSchemaModel model = FixtureModels.CreateIntake();
        ObjectTypeDefinition import = Import(model);
        ConditionalConstraint[] constraints = [.. import.Properties.SelectMany(static property => property.Constraints.Conditional)];

        _ = await Assert.That(TypeSchemaModelValidator.Validate(model).Where(static diagnostic => diagnostic.Severity == SchemaDiagnosticSeverity.Error)).IsEmpty();
        _ = await Assert.That(constraints.Length).IsEqualTo(4);
        _ = await Assert.That(constraints.All(static constraint => constraint.Literal.Kind == SemanticLiteralKind.EnumMember && constraint.Literal.EnumTypeId == constraint.SourceTypeId)).IsTrue();
    }

    [Test]
    public async Task ConditionalConstraint_invalid_target_and_source_are_diagnostic()
    {
        TypeSchemaModel source = FixtureModels.CreateIntake();
        TypeSchemaModel invalidTarget = ChangeCsvConstraint(source, constraint => constraint with { TargetPropertyId = new("ImportSpecification.Unknown") });
        TypeSchemaModel invalidSource = ChangeCsvConstraint(source, constraint => constraint with { SourcePropertyName = "WrongName", SourcePropertyId = new("Unknown") });

        _ = await Assert.That(TypeSchemaModelValidator.Validate(invalidTarget).Any(static diagnostic => diagnostic.Code == "STM1023")).IsTrue();
        _ = await Assert.That(TypeSchemaModelValidator.Validate(invalidSource).Any(static diagnostic => diagnostic.Code == "STM1020")).IsTrue();
    }

    [Test]
    public async Task ConditionalConstraint_type_enum_and_operator_mismatches_are_diagnostic()
    {
        TypeSchemaModel source = FixtureModels.CreateIntake();
        TypeSchemaModel wrongKind = ChangeCsvConstraint(source, constraint => constraint with { Literal = constraint.Literal with { Kind = SemanticLiteralKind.Boolean } });
        TypeSchemaModel wrongEnum = ChangeCsvConstraint(source, constraint => constraint with { Literal = constraint.Literal with { EnumTypeId = new("WrongEnum") } });
        TypeSchemaModel wrongOperator = ChangeCsvConstraint(source, constraint => constraint with { Operator = ConditionalConstraintOperator.IsNull });

        _ = await Assert.That(TypeSchemaModelValidator.Validate(wrongKind).Any(static diagnostic => diagnostic.Code == "STM1022")).IsTrue();
        _ = await Assert.That(TypeSchemaModelValidator.Validate(wrongEnum).Any(static diagnostic => diagnostic.Code == "STM1022")).IsTrue();
        _ = await Assert.That(TypeSchemaModelValidator.Validate(wrongOperator).Any(static diagnostic => diagnostic.Code == "STM1022")).IsTrue();
    }

    private static ObjectTypeDefinition Import(TypeSchemaModel model)
    {
        return model.Types.OfType<ObjectTypeDefinition>().Single(type => type.Id.Value == typeof(Intake.ImportSpecification).FullName);
    }

    private static TypeSchemaModel ChangeCsvConstraint(TypeSchemaModel source, Func<ConditionalConstraint, ConditionalConstraint> change)
    {
        ObjectTypeDefinition import = Import(source);
        PropertyDefinition csv = import.Properties.Single(static property => property.Name == nameof(Intake.ImportSpecification.CsvSource));
        PropertyDefinition changedCsv = csv with { Constraints = csv.Constraints with { Conditional = [change(csv.Constraints.Conditional.Single())] } };
        ObjectTypeDefinition changedImport = import with { Properties = [.. import.Properties.Select(property => property.Id == csv.Id ? changedCsv : property)] };
        TypeDefinition[] types = [.. source.Types.Select(type => type.Id == import.Id ? changedImport : type)];
        return new() { Id = source.Id, Types = types, TypesById = types.ToDictionary(static type => type.Id), Annotations = source.Annotations };
    }
}
#pragma warning restore CS1591
