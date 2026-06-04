using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Query;
using LegacyModel = SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class QueryAndInspectionTests
{
    [Test]
    public async Task Query_api_should_support_typed_string_annotation_and_semantic_filters()
    {
        TypeSchemaModel model = CreateHardeningModel();

        TypeDefinition typed = model.RequireType<Customer>();
        TypeDefinition stringType = model.RequireType("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer");
        PropertyDefinition email = model.RequireProperty<Customer>(static customer => customer.Email);
        PropertyDefinition emailByName = model.RequireProperty("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer", "Email");
        string[] semanticTypes = [.. model.Types().WithSemanticType("Entity").Select(static type => type.Id.Value)];
        string[] annotatedTypes = [.. model.Types().WithAnnotation("semantic.type", "Entity").Select(static type => type.Id.Value)];
        string[] keyProperties = [.. model.PropertiesOf<Customer>().WithSemantic("Key").Select(static property => property.Name)];
        string[] annotatedProperties = [.. model.Properties().WithAnnotation("efCore.primaryKey").Select(static property => property.Name)];
        string[] constrainedProperties = [.. model.Properties().WithConstraint("string.minLength", 5).Select(static property => property.Name)];

        _ = await Assert.That(typed).IsEqualTo(stringType);
        _ = await Assert.That(email).IsEqualTo(emailByName);
        _ = await Assert.That(semanticTypes).IsEquivalentTo(["global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"]);
        _ = await Assert.That(annotatedTypes).IsEquivalentTo(["global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"]);
        _ = await Assert.That(keyProperties).IsEquivalentTo(["Id"]);
        _ = await Assert.That(annotatedProperties).IsEquivalentTo(["Id"]);
        _ = await Assert.That(constrainedProperties).IsEquivalentTo(["Email"]);
    }

    [Test]
    public async Task Query_api_should_provide_safe_and_assertive_string_fallback_without_clr_metadata()
    {
        TypeSchemaModel model = CreateSnapshotLikeModel();

        var found = model.TryGetType("Customer", out TypeDefinition? customer);
        PropertyDefinition email = model.RequireProperty("Customer", "Email");
        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(model.RequireType<Customer>()));

        _ = await Assert.That(found).IsTrue();
        _ = await Assert.That(customer).IsNotNull();
        _ = await Assert.That(email.Name).IsEqualTo("Email");
        _ = await Assert.That(exception).IsNotNull();
        _ = await Assert.That(exception!.Message.Contains("string fallback", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task Diagnostic_helpers_should_filter_assert_and_emit_deterministic_text()
    {
        SchemaDiagnostic[] diagnostics =
        [
            new()
            {
                Severity = SchemaDiagnosticSeverity.Warning,
                Code = "STM5008",
                Message = "Missing semantic display name.",
                Stage = SchemaDiagnosticStage.Validation,
                ModelPath = ModelPath.ForProperty(new TypeId("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"), "Email"),
                RelatedModelPaths = [ModelPath.ForType(new TypeId("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"))],
            },
            new()
            {
                Severity = SchemaDiagnosticSeverity.Error,
                Code = "STM5012",
                Message = "Unsupported dictionary key type.",
                Stage = SchemaDiagnosticStage.Import,
                ModelPath = ModelPath.ForType(new TypeId("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer")),
                PipelineStage = "extract",
                ProjectionTarget = ProjectionTarget.DotNet,
            },
        ];

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() => { diagnostics.ThrowIfErrors(new DiagnosticTextOptions { Detail = SemanticTextDetail.Detailed, IncludeRelatedPaths = true }); return Task.CompletedTask; });
        var text = diagnostics.ToDiagnosticText(new DiagnosticTextOptions { IncludeRelatedPaths = true });

        _ = await Assert.That(diagnostics.HasErrors()).IsTrue();
        _ = await Assert.That(diagnostics.Errors().Single().Code).IsEqualTo("STM5012");
        _ = await Assert.That(diagnostics.WithCode("STM5008").Single().Severity).IsEqualTo(SchemaDiagnosticSeverity.Warning);
        _ = await Assert.That(diagnostics.ForType<Customer>().Single().Code).IsEqualTo("STM5012");
        _ = await Assert.That(diagnostics.ForProperty<Customer>(static customer => customer.Email).Single().Code).IsEqualTo("STM5008");
        _ = await Assert.That(text).IsEqualTo("error STM5012 /types/global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer Unsupported dictionary key type.\nwarning STM5008 /types/global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer/properties/Email Missing semantic display name.\n  related /types/global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer\n");
        _ = await Assert.That(exception).IsNotNull();
        _ = await Assert.That(exception!.Message.Contains("error STM5012", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Model_text_should_support_summary_normal_detailed_and_stable_ordering()
    {
        TypeSchemaModel model = CreateHardeningModel();

        var summary = model.ToSemanticText(new SemanticTextOptions { Detail = SemanticTextDetail.Summary });
        var detailed = model.ToSemanticText(new SemanticTextOptions { Detail = SemanticTextDetail.Detailed, IncludeAnnotations = true, IncludeConstraints = true });

        _ = await Assert.That(summary).IsEqualTo("Model QueryModel\nTypes: 2\n");
        _ = await Assert.That(detailed.Contains("Property Email: String optional nullable", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(detailed.Contains("Annotation dotnet.memberName=Email", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(detailed.Contains("Key PK_Customer: Primary (Id)", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Legacy_generated_model_queries_and_inspection_should_remain_supported()
    {
        var model = new LegacyModel.TypeSchemaModel(
            new Dictionary<string, LegacyModel.TypeShape>(StringComparer.Ordinal)
            {
                ["global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"] = new LegacyModel.ObjectShape
                {
                    Identifier = "global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer",
                    Annotations = [new LegacyModel.SchemaAnnotation("semantic.type", "Entity")],
                    Properties =
                    [
                        new LegacyModel.PropertyShape
                        {
                            Name = "Email",
                            IsRequired = true,
                            Type = LegacyModel.ShapeRef.FromIdentifier("global::System.String"),
                            Annotations = [new LegacyModel.SchemaAnnotation("dotnet.memberName", "Email")],
                        },
                    ],
                },
                ["global::System.String"] = new LegacyModel.ScalarShape { Identifier = "global::System.String", Kind = LegacyModel.ScalarKind.String },
            },
            "global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer");

        LegacyModel.TypeShape type = model.RequireType<Customer>();
        LegacyModel.PropertyShape property = model.RequireProperty<Customer>(static customer => customer.Email);
        var text = model.ToSemanticText(new SemanticTextOptions { IncludeAnnotations = true });

        _ = await Assert.That(type.Identifier).IsEqualTo("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer");
        _ = await Assert.That(property.Name).IsEqualTo("Email");
        _ = await Assert.That(model.Types().WithSemanticType("Entity").Count()).IsEqualTo(1);
        _ = await Assert.That(text.Contains("Annotation dotnet.memberName=Email", StringComparison.Ordinal)).IsTrue();
    }

    private static TypeSchemaModel CreateHardeningModel()
    {
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
            Id = new TypeId("global::SemanticTypeModel.Core.Tests.Unit.QueryAndInspectionTests.Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotation("semantic.type", "Entity", AnnotationScope.Type),
            Semantics = new EntitySemantics { Role = EntityRole.Entity },
            Properties =
            [
                Property("Email", stringType.Id, false, true, Annotation("dotnet.memberName", "Email", AnnotationScope.Member), new ConstraintSet { String = new StringConstraints { MinLength = 5 } }),
                Property("Id", stringType.Id, true, false, Annotations(
                    AnnotationItem("dotnet.memberName", "Id", AnnotationScope.Member),
                    AnnotationItem("semantic.primitive", "Key", AnnotationScope.Member),
                    AnnotationItem("efCore.primaryKey", true, AnnotationScope.Member))),
            ],
            Keys =
            [
                new KeyDefinition
                {
                    Name = "PK_Customer",
                    Kind = KeyKind.Primary,
                    Properties = [new PropertyRef(new PropertyId("Id"))],
                    Annotations = new AnnotationBag(),
                },
            ],
            Relationships = [],
        };

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("QueryModel"),
            Types = [customer, stringType],
            TypesById = new Dictionary<TypeId, TypeDefinition>
            {
                [customer.Id] = customer,
                [stringType.Id] = stringType,
            },
            Annotations = new AnnotationBag(),
        };
    }

    private static TypeSchemaModel CreateSnapshotLikeModel()
    {
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
            Properties = [Property("Email", stringType.Id, true, false, new AnnotationBag())],
            Keys = [],
            Relationships = [],
        };

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("Snapshot"),
            Types = [customer, stringType],
            TypesById = new Dictionary<TypeId, TypeDefinition>
            {
                [customer.Id] = customer,
                [stringType.Id] = stringType,
            },
            Annotations = new AnnotationBag(),
        };
    }

    private static PropertyDefinition Property(string name, TypeId typeId, bool required, bool nullable, AnnotationBag annotations, ConstraintSet? constraints = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(name),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = required, AllowsNull = nullable },
            Mutability = Mutability.Mutable,
            Constraints = constraints ?? new ConstraintSet(),
            Annotations = annotations,
        };
    }

    private static AnnotationBag Annotation(string key, object? value, AnnotationScope scope)
    {
        return Annotations(AnnotationItem(key, value, scope));
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new AnnotationBag { Items = annotations };
    }

    private static Annotation AnnotationItem(string key, object? value, AnnotationScope scope)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = scope,
            Source = AnnotationSource.Declared,
        };
    }

    private sealed class Customer
    {
        public string Id { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;
    }
}
#pragma warning restore CS1591
