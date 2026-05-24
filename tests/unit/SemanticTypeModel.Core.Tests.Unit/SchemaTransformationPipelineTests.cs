using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Hardening;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class SchemaTransformationPipelineTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Pipeline_should_execute_transformations_in_deterministic_order()
    {
        TypeSchemaModel model = BuildModel(Scalar("Customer", "Customer"));
        AppendSuffixTransformation first = new("_A");
        AppendSuffixTransformation second = new("_B");

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(first)
            .Use(second)
            .RunAsync(model);

        _ = await Assert.That(result.Model.Types.OfType<ScalarTypeDefinition>().Single().Name).IsEqualTo("Customer_A_B");
    }

    [Test]
    public async Task Pipeline_should_accumulate_structured_diagnostics()
    {
        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new DiagnosticTransformation("STM2998", SchemaDiagnosticSeverity.Warning, "/types/Customer"))
            .Use(new DiagnosticTransformation("STM2999", SchemaDiagnosticSeverity.Error, "/types/Customer/properties/email"))
            .RunAsync(BuildModel(Scalar("Customer", "Customer")), new SchemaPipelineOptions { ContinueOnError = true });

        _ = await Assert.That(result.Diagnostics.Count).IsEqualTo(2);
        _ = await Assert.That(result.Diagnostics[0].Code).IsEqualTo("STM2998");
        _ = await Assert.That(result.Diagnostics[0].Stage).IsEqualTo(SchemaDiagnosticStage.Transformation);
        _ = await Assert.That(result.Diagnostics[1].Severity).IsEqualTo(SchemaDiagnosticSeverity.Error);
        _ = await Assert.That(result.Diagnostics[1].ModelPath).IsEqualTo("/types/Customer/properties/email");
    }

    [Test]
    public async Task Pipeline_should_stop_before_next_transformation_after_error_by_default()
    {
        SpyTransformation marker = new();

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new DiagnosticTransformation("STM2999", SchemaDiagnosticSeverity.Error, "/types/Customer"))
            .Use(marker)
            .RunAsync(BuildModel(Scalar("Customer", "Customer")));

        _ = await Assert.That(result.HasErrors).IsTrue();
        _ = await Assert.That(marker.WasRun).IsFalse();
    }

    [Test]
    public async Task Pipeline_should_continue_after_errors_when_configured()
    {
        SpyTransformation marker = new();

        _ = await SchemaTransformationPipeline.Create()
            .Use(new DiagnosticTransformation("STM2999", SchemaDiagnosticSeverity.Error, "/types/Customer"))
            .Use(marker)
            .RunAsync(BuildModel(Scalar("Customer", "Customer")), new SchemaPipelineOptions { ContinueOnError = true });

        _ = await Assert.That(marker.WasRun).IsTrue();
    }

    [Test]
    public async Task Validate_model_transformation_should_emit_validation_diagnostics()
    {
        ScalarTypeDefinition stringType = Scalar("String", "String");
        ObjectTypeDefinition invalid = new()
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            Properties =
            [
                Property("email", "Prop1", stringType.Id),
                Property("email", "Prop2", stringType.Id),
            ],
            Keys = [],
            Relationships = [],
        };

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new ValidateModelTransformation())
            .RunAsync(BuildModel(invalid, stringType), new SchemaPipelineOptions { ContinueOnError = true });

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM0003")).IsTrue();
    }

    [Test]
    public async Task Normalize_annotations_transformation_should_apply_last_wins_merge_for_duplicate_keys()
    {
        ObjectTypeDefinition annotated = new()
        {
            Id = new TypeId("Annotated"),
            Name = "Annotated",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = new AnnotationBag
            {
                Items =
                [
                    Annotation("UI.order", 1),
                    Annotation("ui.order", 2),
                ],
            },
            Properties = [],
            Keys = [],
            Relationships = [],
        };

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new NormalizeAnnotationsTransformation())
            .RunAsync(BuildModel(annotated));

        var transformed = (ObjectTypeDefinition)result.Model.GetType(annotated.Id);
        _ = await Assert.That(transformed.Annotations.Items.Count).IsEqualTo(1);
        _ = await Assert.That(transformed.Annotations.Items[0].Key.Value).IsEqualTo("ui.order");
        _ = await Assert.That(transformed.Annotations.Items[0].Value).IsEqualTo(2);
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM1002")).IsTrue();
    }

    [Test]
    public async Task Normalize_names_transformation_should_generate_deterministic_legal_unique_names()
    {
        ScalarTypeDefinition first = Scalar("Customer-A", "Customer-A");
        ScalarTypeDefinition second = Scalar("Customer B", "Customer B");

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new NormalizeNamesTransformation())
            .RunAsync(BuildModel(first, second), new SchemaPipelineOptions { ContinueOnError = true });

        List<string> names = [.. result.Model.Types.Select(static type => type.Name)];
        _ = await Assert.That(names[0]).IsEqualTo("Customer_A");
        _ = await Assert.That(names[1]).IsEqualTo("Customer_B");
        _ = await Assert.That(names.Distinct(StringComparer.OrdinalIgnoreCase).Count()).IsEqualTo(2);
    }

    [Test]
    public async Task Normalize_names_transformation_should_diagnose_collisions()
    {
        ScalarTypeDefinition first = Scalar("Customer-A", "Customer-A");
        ScalarTypeDefinition second = Scalar("Customer A", "Customer A");

        SchemaPipelineResult result = await SchemaTransformationPipeline.Create()
            .Use(new NormalizeNamesTransformation())
            .RunAsync(BuildModel(first, second), new SchemaPipelineOptions { ContinueOnError = true });

        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM2001")).IsTrue();
        _ = await Assert.That(result.Model.Types.Select(static type => type.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()).IsEqualTo(2);
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        Dictionary<TypeId, TypeDefinition> byId = [];
        foreach (TypeDefinition type in types)
        {
            if (!byId.ContainsKey(type.Id))
            {
                byId[type.Id] = type;
            }
        }

        return new TypeSchemaModel
        {
            Id = new SchemaModelId("PipelineModel"),
            Types = types,
            TypesById = byId,
            Annotations = EmptyAnnotations,
        };
    }

    private static ScalarTypeDefinition Scalar(string id, string name)
    {
        return new ScalarTypeDefinition
        {
            Id = new TypeId(id),
            Name = name,
            Kind = TypeKind.Scalar,
            Nullability = Nullability.NonNullable,
            Annotations = EmptyAnnotations,
            ScalarKind = ScalarKind.String,
        };
    }

    private static PropertyDefinition Property(string name, string propertyId, TypeId typeId)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(propertyId),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = EmptyAnnotations,
        };
    }

    private static Annotation Annotation(string key, object? value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Type,
            Source = AnnotationSource.Declared,
        };
    }

    private sealed class AppendSuffixTransformation(string suffix) : ISchemaTransformation
    {
        public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<TypeDefinition> transformedTypes =
            [
                .. model.Model.Types.Select(type => type switch
                {
                    ScalarTypeDefinition scalar => scalar with { Name = scalar.Name + suffix },
                    _ => type,
                }),
            ];

            model.Replace(new TypeSchemaModel
            {
                Id = model.Model.Id,
                Types = transformedTypes,
                TypesById = transformedTypes.ToDictionary(static type => type.Id, static type => type),
                Annotations = model.Model.Annotations,
            });

            return ValueTask.CompletedTask;
        }
    }

    private sealed class DiagnosticTransformation(string code, SchemaDiagnosticSeverity severity, string modelPath) : ISchemaTransformation
    {
        public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Diagnostics.Report(new SchemaDiagnostic
            {
                Severity = severity,
                Code = code,
                Message = code,
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.PipelineStage,
                ModelPath = modelPath,
            });

            return ValueTask.CompletedTask;
        }
    }

    private sealed class SpyTransformation : ISchemaTransformation
    {
        public bool WasRun { get; private set; }

        public ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WasRun = true;
            return ValueTask.CompletedTask;
        }
    }
}
#pragma warning restore CS1591
