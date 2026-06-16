using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Inspection;
using SemanticTypeModel.Core.Transformation;

namespace SemanticTypeModel.Core.Tests.Unit;

#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class M0028TransformationPipelineTests
{
    private static readonly AnnotationBag EmptyAnnotations = new();

    [Test]
    public async Task Pipeline_configuration_should_support_add_remove_replace_and_ordered_insertion()
    {
        SchemaTransformationPipeline pipeline = SchemaTransformationPipeline.Create()
            .Add(new NamedTransformation("a", "A"))
            .Add(new NamedTransformation("c", "C"))
            .AddBefore("c", new NamedTransformation("b", "B"))
            .Replace("b", new NamedTransformation("b2", "B2"))
            .AddAfter("b2", new NamedTransformation("b3", "B3"))
            .Remove("a");

        _ = await Assert.That(pipeline.GetTransformationOrder()).IsEquivalentTo(["b2", "b3", "c"]);
    }

    [Test]
    public async Task Pipeline_configuration_should_reject_duplicate_identifiers_and_missing_targets()
    {
        SchemaTransformationPipeline pipeline = SchemaTransformationPipeline.Create().Add(new NamedTransformation("a", "A"));

        InvalidOperationException? duplicate = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(pipeline.Add(new NamedTransformation("a", "Duplicate"))));
        InvalidOperationException? missing = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(pipeline.AddBefore("missing", new NamedTransformation("b", "B"))));

        _ = await Assert.That(duplicate?.Message).Contains("already configured");
        _ = await Assert.That(missing?.Message).Contains("was not found");
    }

    [Test]
    public async Task Pipeline_execution_should_expose_deterministic_result_trace_and_stop_modes()
    {
        TypeSchemaModel model = BuildModel(Scalar("String", "String"));
        var marker = new NamedTransformation("after", "After");

        SemanticModelTransformationResult stopped = SchemaTransformationPipeline.Create()
            .Add(new DiagnosticSemanticTransformation("error", SchemaDiagnosticSeverity.Error))
            .Add(marker)
            .Run(model);

        SemanticModelTransformationResult continued = SchemaTransformationPipeline.Create()
            .Add(new DiagnosticSemanticTransformation("error", SchemaDiagnosticSeverity.Error))
            .Add(new NamedTransformation("after", "After"))
            .Run(model, new SchemaPipelineOptions { ContinueOnError = true });

        _ = await Assert.That(stopped.Trace.Entries.Count).IsEqualTo(1);
        _ = await Assert.That(continued.Trace.Entries.Count).IsEqualTo(2);
        _ = await Assert.That(continued.ToTransformationText()).Contains("Transformation Pipeline: Configured\n\n[1] error\n    Id: error\n    Diagnostics: 1 (STM2999)");
    }

    [Test]
    public async Task Core_defaults_should_normalize_aliases_derive_keys_display_metadata_and_diagnostics()
    {
        ScalarTypeDefinition stringType = Scalar("String", "String");
        ObjectTypeDefinition customer = new()
        {
            Id = new TypeId("Customer"),
            Name = "Customer",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotations(Annotation("schema.role", "Entity"), Annotation("schema.title", "Customer Display")),
            Properties =
            [
                Property("Id", "Id", stringType.Id, Annotations(Annotation("schema.key", "true"), Annotation("schema.key.generated", "true"))),
            ],
            Keys = [],
            Relationships = [],
        };

        ObjectTypeDefinition valueObject = new()
        {
            Id = new TypeId("Address"),
            Name = "Address",
            Kind = TypeKind.Object,
            Nullability = Nullability.NonNullable,
            Annotations = Annotations(Annotation("schema.role", "ValueObject")),
            Properties = [Property("Code", "Code", stringType.Id, Annotations(Annotation("schema.key", "true")))],
            Keys = [],
            Relationships = [],
        };

        SemanticModelTransformationResult result = BuildModel(customer, valueObject, stringType).Transform(pipeline => pipeline.UseCoreDefaults(), new SchemaPipelineOptions { ContinueOnError = true });

        var transformedCustomer = (ObjectTypeDefinition)result.Model.GetType(customer.Id);
        var transformedAddress = (ObjectTypeDefinition)result.Model.GetType(valueObject.Id);

        _ = await Assert.That(transformedCustomer.Semantics.Role).IsEqualTo(EntityRole.Entity);
        _ = await Assert.That(transformedCustomer.DisplayName).IsEqualTo("Customer Display");
        _ = await Assert.That(transformedCustomer.Keys.Single().Name).IsEqualTo("PK_Id");
        _ = await Assert.That(transformedCustomer.Keys.Single().IsGenerated).IsTrue();
        _ = await Assert.That(transformedAddress.Semantics.IsValueObject).IsTrue();
        _ = await Assert.That(result.Diagnostics.Any(static diagnostic => diagnostic.Code == "STM1006" && diagnostic.PipelineStage == "core.derive-semantic-keys")).IsTrue();
    }

    [Test]
    public async Task Domain_derivation_result_should_carry_domain_model_diagnostics_and_trace()
    {
        TypeSchemaModel model = BuildModel(Scalar("String", "String"));
        SemanticModelTransformationResult transformed = model.Transform(pipeline => pipeline.Add(new NamedTransformation("domain.naming", "Domain Naming")));

        SemanticDerivationResult<FakeDomainModel> result = new()
        {
            Model = new FakeDomainModel(transformed.Model.Id.Value),
            Diagnostics = transformed.Diagnostics,
            Trace = transformed.Trace,
        };

        _ = await Assert.That(result.Model.Name).IsEqualTo("TestModel");
        _ = await Assert.That(result.Diagnostics).IsEmpty();
        _ = await Assert.That(result.Trace.Entries.Single().TransformationId).IsEqualTo("domain.naming");
    }

    private static TypeSchemaModel BuildModel(params TypeDefinition[] types)
    {
        return new TypeSchemaModel
        {
            Id = new SchemaModelId("TestModel"),
            Types = types,
            TypesById = types.ToDictionary(static type => type.Id, static type => type),
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

    private static PropertyDefinition Property(string name, string propertyId, TypeId typeId, AnnotationBag? annotations = null)
    {
        return new PropertyDefinition
        {
            Id = new PropertyId(propertyId),
            Name = name,
            Type = new TypeRef(typeId),
            Cardinality = new Cardinality { IsRequired = true },
            Mutability = Mutability.Mutable,
            Constraints = new ConstraintSet(),
            Annotations = annotations ?? EmptyAnnotations,
        };
    }

    private static AnnotationBag Annotations(params Annotation[] annotations)
    {
        return new AnnotationBag { Items = annotations };
    }

    private static Annotation Annotation(string key, string value)
    {
        return new Annotation
        {
            Key = new AnnotationKey(key),
            Value = value,
            Scope = AnnotationScope.Type,
            Source = AnnotationSource.Declared,
        };
    }

    private sealed record FakeDomainModel(string Name);

    private sealed class NamedTransformation(string id, string displayName) : ISemanticModelTransformation
    {
        public string Id => id;

        public string DisplayName => displayName;

        public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
        {
            return new SemanticModelTransformationStepResult
            {
                Model = model,
                ChangeSummary = [$"/{id}"],
            };
        }
    }

    private sealed class DiagnosticSemanticTransformation(string id, SchemaDiagnosticSeverity severity) : ISemanticModelTransformation
    {
        public string Id => id;

        public string DisplayName => id;

        public SemanticModelTransformationStepResult Transform(TypeSchemaModel model, SemanticModelTransformationContext context)
        {
            context.Diagnostics.Report(new SchemaDiagnostic
            {
                Severity = severity,
                Code = "STM2999",
                Message = "diagnostic",
                Stage = SchemaDiagnosticStage.Transformation,
                PipelineStage = context.TransformationId,
                ModelPath = "/types/String",
            });

            return SemanticModelTransformationStepResult.Unchanged(model);
        }
    }
}
#pragma warning restore CS1591
