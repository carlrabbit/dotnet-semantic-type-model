using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.PowerBI.Tests.Unit;

#pragma warning disable CS1591
#pragma warning disable IDE0305
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0031PowerBiDerivationTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task DerivePowerBiModel_should_return_domain_model_diagnostics_and_trace()
    {
        TypeSchemaModel model = SalesModel();

        SemanticDerivationResult<PowerBiSemanticModel> result = model.DerivePowerBiModel(options => options.UseDefaultTransformations());

        _ = await Assert.That(result.Model.Tables.Count).IsEqualTo(2);
        _ = await Assert.That(result.Trace.Entries.Any(static entry => entry.TransformationId == "powerBi.derive-tables")).IsTrue();
        _ = await Assert.That(result.Diagnostics).IsNotNull();
    }

    [Test]
    public async Task Derivation_options_should_add_explicit_measure_and_calculated_table()
    {
        TypeSchemaModel model = SalesModel();

        SemanticDerivationResult<PowerBiSemanticModel> result = model.DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            options.Measures.Add("FactSales", "Total Sales", "SUM(FactSales[amount])", measure =>
            {
                measure.FormatString = "$#,0.00";
                measure.DisplayFolder = "Sales";
            });
            options.CalculatedTables.Add("Active Customers", "FILTER(DimCustomer, DimCustomer[IsActive] = TRUE())", table => table.DisplayFolder = "Customer");
        });

        PowerBiMeasureDefinition measure = result.Model.Tables.Single(static table => table.Name == "FactSales").Measures.Single(static measure => measure.Name == "Total Sales");
        _ = await Assert.That(measure.FormatString).IsEqualTo("$#,0.00");
        _ = await Assert.That(result.Model.CalculatedTables.Single().Name).IsEqualTo("Active Customers");
    }

    [Test]
    public async Task Derivation_pipeline_should_allow_custom_transformation_replacement_and_extension()
    {
        TypeSchemaModel model = SalesModel();

        SemanticDerivationResult<PowerBiSemanticModel> result = model.DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            _ = options.Transformations.Replace<DerivePowerBiTablesTransformation>(new CustomPowerBiTransformation());
            _ = options.ConfigureModel(powerBi =>
            {
                PowerBiTableDefinition fact = powerBi.Tables.Single(static table => table.Name == "FactSales");
                powerBi.Tables = [fact with { Measures = [.. fact.Measures, new PowerBiMeasureDefinition { Name = "Configured", Expression = "1" }] }, .. powerBi.Tables.Where(static table => table.Name != "FactSales")];
            });
        });

        _ = await Assert.That(result.Trace.Entries.Any(static entry => entry.TransformationId == "test.powerbi-custom")).IsTrue();
        _ = await Assert.That(result.Model.Tables.Single(static table => table.Name == "FactSales").Measures.Any(static measure => measure.Name == "Configured")).IsTrue();
    }

    [Test]
    public async Task Local_metadata_export_should_be_deterministic_and_include_sort_metadata()
    {
        TypeSchemaModel model = SalesModel();

        PowerBiSemanticModel first = model.DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            options.CalculatedTables.Add("Calendar", "CALENDAR(DATE(2024,1,1), DATE(2024,12,31))");
        }).Model;
        PowerBiSemanticModel second = model.DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            options.CalculatedTables.Add("Calendar", "CALENDAR(DATE(2024,1,1), DATE(2024,12,31))");
        }).Model;

        var firstJson = PowerBiLocalMetadataExporter.ExportJson(first);
        var secondJson = PowerBiLocalMetadataExporter.ExportJson(second);
        var inspection = PowerBiLocalMetadataExporter.Inspect(first);

        _ = await Assert.That(firstJson).IsEqualTo(secondJson);
        _ = await Assert.That(firstJson.Contains("SortByColumn", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(inspection.Contains("PowerBiSemanticModel: PowerBiModel", StringComparison.Ordinal)).IsTrue();
    }


    [Test]
    public async Task Unsupported_explicit_artifact_expression_languages_should_emit_diagnostics()
    {
        SemanticDerivationResult<PowerBiSemanticModel> result = SalesModel().DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            options.Measures.Add("FactSales", "Sql Measure", "sum(amount)", measure => measure.ExpressionLanguage = "SQL");
            options.CalculatedTables.Add("Sql Table", "select 1", table => table.ExpressionLanguage = "SQL");
        });

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_UNSUPPORTED_MEASURE_EXPRESSION_LANGUAGE")).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_UNSUPPORTED_CALCULATED_TABLE_EXPRESSION_LANGUAGE")).IsTrue();
    }

    [Test]
    public async Task Explicit_measure_for_missing_table_should_emit_diagnostic()
    {
        SemanticDerivationResult<PowerBiSemanticModel> result = SalesModel().DerivePowerBiModel(options =>
        {
            _ = options.UseDefaultTransformations();
            options.Measures.Add("Missing", "Broken", "1");
        });

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "POWERBI_EXPLICIT_MEASURE_TABLE_NOT_FOUND")).IsTrue();
    }

    private sealed class CustomPowerBiTransformation : ISemanticModelTransformation
    {
        public string Id => "test.powerbi-custom";

        public string DisplayName => nameof(CustomPowerBiTransformation);

        public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
        {
            return new SemanticModelTransformationStepResult { Model = model, ChangeSummary = ["custom Power BI transformation ran"] };
        }
    }

    private static TypeSchemaModel SalesModel()
    {
        ScalarTypeDefinition intType = Scalar("Int64", ScalarKind.Integer);
        ScalarTypeDefinition decimalType = Scalar("Decimal", ScalarKind.Decimal);
        ScalarTypeDefinition boolType = Scalar("Boolean", ScalarKind.Boolean);
        ObjectTypeDefinition dimension = Entity(
            "DimCustomer",
            EntityRole.Dimension,
            [Property("customerKey", "DimCustomerKey", intType.Id, true, false), Property("isActive", "IsActive", boolType.Id, true, false)],
            [Key("PK_DimCustomer", "DimCustomerKey")]);
        ObjectTypeDefinition fact = Entity(
            "FactSales",
            EntityRole.Fact,
            [
                Property("salesKey", "FactSalesKey", intType.Id, true, false),
                Property("customerKey", "FactCustomerKey", intType.Id, true, false),
                Property("monthNumber", "MonthNumber", intType.Id, true, false),
                Property("monthName", "MonthName", intType.Id, true, false, Annotation((PowerBiAnnotationNames.SortByColumn, "monthNumber"))),
                Property("amount", "Amount", decimalType.Id, true, false, Annotation((PowerBiAnnotationNames.FormatString, "$#,0.00"))),
            ],
            [Key("PK_FactSales", "FactSalesKey")],
            relationships:
            [
                new RelationshipDefinition
                {
                    Id = new RelationshipId("FactSales_DimCustomer"),
                    PrincipalType = new TypeRef(dimension.Id),
                    DependentType = new TypeRef(new TypeId("FactSales")),
                    PrincipalProperties = [new PropertyRef(new PropertyId("DimCustomerKey"))],
                    DependentProperties = [new PropertyRef(new PropertyId("FactCustomerKey"))],
                    Cardinality = RelationshipCardinality.ManyToOne,
                    Annotations = EmptyAnnotations,
                },
            ]);

        return BuildModel(fact, dimension, intType, decimalType, boolType);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new TypeSchemaModel { Id = new SchemaModelId("PowerBiModel"), Types = types, TypesById = types.ToDictionary(static type => type.Id, static type => type), Annotations = EmptyAnnotations };
    }

    private static ScalarTypeDefinition Scalar(string name, ScalarKind kind)
    {
        return new ScalarTypeDefinition { Id = new TypeId(name), Name = name, Kind = TypeKind.Scalar, Nullability = Nullability.NonNullable, Annotations = EmptyAnnotations, ScalarKind = kind };
    }

    private static ObjectTypeDefinition Entity(string name, EntityRole role, IReadOnlyList<PropertyDefinition> properties, IReadOnlyList<KeyDefinition> keys, IReadOnlyList<RelationshipDefinition>? relationships = null)
    {
        return new ObjectTypeDefinition { Id = new TypeId(name), Name = name, Kind = TypeKind.Object, Nullability = Nullability.NonNullable, Annotations = Annotation((PowerBiAnnotationNames.TableRole, role.ToString())), Semantics = new EntitySemantics { Role = role }, Properties = properties, Keys = keys, Relationships = relationships ?? [] };
    }

    private static KeyDefinition Key(string name, params string[] properties)
    {
        return new KeyDefinition { Name = name, Kind = KeyKind.Primary, Properties = properties.Select(static property => new PropertyRef(new PropertyId(property))).ToArray(), Annotations = EmptyAnnotations };
    }

    private static PropertyDefinition Property(string name, string propertyId, TypeId typeId, bool isRequired, bool allowsNull, AnnotationBag? annotations = null)
    {
        return new PropertyDefinition { Id = new PropertyId(propertyId), Name = name, Type = new TypeRef(typeId), Cardinality = new Cardinality { IsRequired = isRequired, AllowsNull = allowsNull }, Mutability = Mutability.Mutable, Constraints = new ConstraintSet(), Annotations = annotations ?? EmptyAnnotations };
    }

    private static AnnotationBag Annotation(params (string key, object? value)[] items)
    {
        return new AnnotationBag { Items = items.Select(static item => new Annotation { Key = new AnnotationKey(item.key), Value = item.value, Scope = AnnotationScope.Projection, Source = AnnotationSource.Declared }).ToArray() };
    }
}
#pragma warning restore IDE0305
#pragma warning restore CS1591
