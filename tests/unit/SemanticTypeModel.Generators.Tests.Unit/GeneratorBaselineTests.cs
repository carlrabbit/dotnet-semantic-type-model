using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.DotNet;
using SemanticTypeModel.Generators;
using SemanticTypeModel.JsonSchema;
using SemanticTypeModel.JsonSchema.Export;
using Hardening = SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.Generators.Tests.Unit;

[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class GeneratorBaselineTests
{
    [Test]
    public async Task Fixture_1_simple_annotated_object_should_preserve_requiredness_nullability_and_key_metadata()
    {
        const string source = """
            using System;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            [SemanticName("Customer")]
            [SemanticDescription("A customer account.")]
            public sealed class Customer
            {
                [SemanticKey]
                public Guid Id { get; init; }

                public required string Name { get; init; }

                public string? Nickname { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");

        PropertyShape name = customer.Properties.Single(static property => property.Name == "Name");
        PropertyShape nickname = customer.Properties.Single(static property => property.Name == "Nickname");
        PropertyShape id = customer.Properties.Single(static property => property.Name == "Id");

        _ = await Assert.That(name.IsRequired).IsTrue();
        _ = await Assert.That(name.IsNullable).IsFalse();
        _ = await Assert.That(nickname.IsRequired).IsFalse();
        _ = await Assert.That(nickname.IsNullable).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "schema.key" && annotation.Value == "true")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.title" && annotation.Value == "Customer")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.description" && annotation.Value == "A customer account.")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_2_scalars_should_map_to_expected_scalar_shapes_and_annotations()
    {
        const string source = """
            using System;
            using System.Text.Json;
            using System.Text.Json.Nodes;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class ScalarBag
            {
                public bool Flag { get; init; }
                public string Text { get; init; } = string.Empty;
                public int Count { get; init; }
                public double Ratio { get; init; }
                public decimal Amount { get; init; }
                public DateOnly Date { get; init; }
                public TimeOnly Time { get; init; }
                public DateTime Timestamp { get; init; }
                public DateTimeOffset OffsetTimestamp { get; init; }
                public TimeSpan Duration { get; init; }
                public Guid Identifier { get; init; }
                public byte[] Payload { get; init; } = [];
                public JsonDocument Document { get; init; } = JsonDocument.Parse("{}");
                public JsonElement Element { get; init; }
                public JsonNode? Node { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape root = (ObjectShape)model.GetShape("global::ScalarBag");
        Dictionary<string, PropertyShape> properties = root.Properties.ToDictionary(static property => property.Name, static property => property, StringComparer.Ordinal);

        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Flag"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Boolean);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Text"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.String);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Count"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Integer);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Ratio"].Type!.Identifier!)).Kind).IsEqualTo(ScalarKind.Number);
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Amount"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "dotnet.scalarKind" && annotation.Value == "Decimal")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Date"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "date")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Time"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "time")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Timestamp"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "date-time")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Duration"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "duration")).IsTrue();
        _ = await Assert.That(((ScalarShape)model.GetShape(properties["Identifier"].Type!.Identifier!)).Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "uuid")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_3_collections_and_dictionaries_should_map_and_diagnose_unsupported_keys()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class CollectionBag
            {
                public string[] Names { get; init; } = [];
                public List<int> Ints { get; init; } = [];
                public IReadOnlyList<Guid> Ids { get; init; } = [];
                public HashSet<double> Ratios { get; init; } = [];
                public Dictionary<string, int> Lookup { get; init; } = [];
                public Dictionary<DateTime, int> Unsupported { get; init; } = [];
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape root = (ObjectShape)model.GetShape("global::CollectionBag");
        Dictionary<string, PropertyShape> properties = root.Properties.ToDictionary(static property => property.Name, static property => property, StringComparer.Ordinal);

        _ = await Assert.That(model.GetShape(properties["Names"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ints"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ids"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Ratios"].Type!.Identifier!)).IsTypeOf<ArrayShape>();
        _ = await Assert.That(model.GetShape(properties["Lookup"].Type!.Identifier!)).IsTypeOf<DictionaryShape>();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5007")).IsTrue();
    }

    [Test]
    public async Task Fixture_4_enum_should_include_values_and_numeric_metadata()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            public enum OrderStatus
            {
                New = 1,
                Packed = 2,
                Shipped = 5,
            }

            [SemanticType]
            public sealed class Order
            {
                public OrderStatus Status { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        EnumShape orderStatus = (EnumShape)model.GetShape("global::OrderStatus");

        _ = await Assert.That(orderStatus.Values.SequenceEqual(["New", "Packed", "Shipped"])).IsTrue();
        _ = await Assert.That(orderStatus.Annotations.Any(static annotation => annotation.Key == "dotnet.enumNumericValues" && annotation.Value == "[1,2,5]")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_5_nested_object_graph_should_include_reachable_types()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public Address Address { get; init; } = new();
            }

            public sealed class Address
            {
                public string Street { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        _ = await Assert.That(model.TryGetShape("global::Address")).IsNotNull();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_6_generics_should_generate_distinct_closed_identities_and_diagnose_open_generics()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
            public sealed class Customer;
            public sealed class Order;

            [SemanticType]
            public sealed class Root
            {
                public PagedResult<Customer> Customers { get; init; } = new([], 0);
                public PagedResult<Order> Orders { get; init; } = new([], 0);
            }

            [SemanticType]
            public sealed class OpenGeneric<T>
            {
                public T Value { get; init; } = default!;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);

        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Customer>")).IsNotNull();
        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Order>")).IsNotNull();
        _ = await Assert.That(model.TryGetShape("global::PagedResult<global::Customer>")).IsNotEqualTo(model.TryGetShape("global::PagedResult<global::Order>"));
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5004")).IsTrue();
    }

    [Test]
    public async Task Fixture_7_inheritance_and_interface_metadata_should_be_preserved_as_annotations()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            public interface IMarker { }
            public class BaseEntity { public int Id { get; init; } }

            [SemanticType]
            public sealed class Customer : BaseEntity, IMarker
            {
                public string Name { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");

        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "dotnet.baseType" && annotation.Value == "global::BaseEntity")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "dotnet.interfaces")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_8_generated_model_should_export_to_json_schema()
    {
        const string source = """
            using System;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                [SemanticKey]
                public Guid Id { get; init; }

                public required string Name { get; init; }

                [SemanticIgnore]
                public string InternalCode { get; init; } = string.Empty;
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        JsonSchemaExportResult export = JsonSchemaExporter.Export(model);
        string json = export.Document.RootElement.GetRawText();

        _ = await Assert.That(json.Contains("\"properties\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"Name\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"required\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("\"Id\"", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(json.Contains("InternalCode", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_9_namespace_discovery_should_respect_includes_excludes_and_ignore()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            namespace MyApp.Contracts
            {
                public sealed class IncludedType
                {
                    public string Name { get; init; } = string.Empty;
                }
            }

            namespace MyApp.Domain.Internal
            {
                public sealed class ExcludedType
                {
                    public string Value { get; init; } = string.Empty;
                }
            }

            namespace MyApp.Domain
            {
                [SemanticIgnore]
                public sealed class IgnoredType
                {
                    public string Secret { get; init; } = string.Empty;
                }
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelDiscoveryMode"] = "Namespace",
            ["SemanticTypeModelIncludedNamespaces"] = "MyApp.Contracts;MyApp.Domain",
            ["SemanticTypeModelExcludedNamespaces"] = "MyApp.Domain.Internal",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, options);

        _ = await Assert.That(model.TryGetShape("global::MyApp.Contracts.IncludedType")).IsNotNull();
        _ = await Assert.That(model.TryGetShape("global::MyApp.Domain.Internal.ExcludedType")).IsNull();
        _ = await Assert.That(model.TryGetShape("global::MyApp.Domain.IgnoredType")).IsNull();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5010")).IsTrue();
    }

    [Test]
    public async Task Fixture_10_naming_policy_should_apply_and_diagnose_collisions()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class NamingRoot
            {
                public string CustomerName { get; init; } = string.Empty;
                public string customerName { get; init; } = string.Empty;
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelNamingPolicy"] = "CamelCase",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, options);
        ObjectShape root = (ObjectShape)model.GetShape("global::NamingRoot");

        _ = await Assert.That(root.Properties.Any(static property => property.Name == "customerName")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5006")).IsTrue();
    }

    [Test]
    public async Task Fixture_11_key_inference_and_composite_keys_should_be_deterministic()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public int Id { get; init; }
                public int CustomerId { get; init; }
                [SemanticKey(Name = "tenantCustomer", Order = 0)]
                public string TenantId { get; init; } = string.Empty;
                [SemanticKey(Name = "tenantCustomer", Order = 1)]
                public string ExternalId { get; init; } = string.Empty;
            }

            [SemanticType]
            public sealed class AmbiguousEntity
            {
                public int Id { get; init; }
                public int AmbiguousEntityId { get; init; }
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelInferKeys"] = "true",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, options);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");
        PropertyShape tenantId = customer.Properties.Single(static property => property.Name == "TenantId");
        PropertyShape externalId = customer.Properties.Single(static property => property.Name == "ExternalId");

        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5013")).IsTrue();
        _ = await Assert.That(tenantId.Annotations.Any(static annotation => annotation.Key == "schema.key.name" && annotation.Value == "tenantCustomer")).IsTrue();
        _ = await Assert.That(externalId.Annotations.Any(static annotation => annotation.Key == "schema.key.order" && annotation.Value == "1")).IsTrue();
    }

    [Test]
    public async Task Fixture_12_relationship_inference_should_be_conservative_and_diagnose_missing_targets()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Order
            {
                public Customer Customer { get; init; } = new();
                public List<OrderItem> Items { get; init; } = [];
                [SemanticRelationship("global::MissingTarget")]
                public string LegacyForeignKey { get; init; } = string.Empty;
            }

            public sealed class Customer
            {
                public int Id { get; init; }
            }

            public sealed class OrderItem
            {
                public string Sku { get; init; } = string.Empty;
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelInferRelationships"] = "true",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, options);
        ObjectShape order = (ObjectShape)model.GetShape("global::Order");
        PropertyShape customer = order.Properties.Single(static property => property.Name == "Customer");

        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.relationship" && annotation.Value == "inferred")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5015")).IsTrue();
    }

    [Test]
    public async Task Fixture_13_generator_configuration_should_apply_provider_options_and_validate_invalid_values()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class ConfiguredRoot
            {
                public string Name { get; init; } = string.Empty;
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelGeneratedNamespace"] = "MyApp.Generated",
            ["SemanticTypeModelGeneratedProviderName"] = "MySemanticModel",
            ["SemanticTypeModelDiscoveryMode"] = "NotARealMode",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(
            source,
            options,
            "MyApp.Generated.MySemanticModel");

        _ = await Assert.That(model.TryGetShape("global::ConfiguredRoot")).IsNotNull();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5008")).IsTrue();
    }

    [Test]
    public async Task Fixture_14_xml_documentation_should_map_description_when_enabled_and_not_override_explicit_description()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            /// <summary>Customer summary from XML.</summary>
            [SemanticType]
            [SemanticDescription("Explicit customer description.")]
            public sealed class Customer
            {
                /// <summary>Identifier from XML.</summary>
                public int Id { get; init; }
            }
            """;

        var options = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelIncludeXmlDocumentation"] = "true",
        };

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, options);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");
        PropertyShape id = customer.Properties.Single(static property => property.Name == "Id");

        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.description" && annotation.Value == "Explicit customer description.")).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "schema.description" && annotation.Value == "Identifier from XML.")).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_15_generated_model_should_compose_with_transformation_pipeline_and_json_schema_export()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Product
            {
                public int Id { get; init; }
                public required string Name { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(
            source,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["SemanticTypeModelInferKeys"] = "true",
            });

        JsonSchemaExportResult export = JsonSchemaExporter.Export(model);
        string json = export.Document.RootElement.GetRawText();

        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
        _ = await Assert.That(export.Diagnostics.Any(static diagnostic => diagnostic.Severity == Hardening.SchemaDiagnosticSeverity.Error)).IsFalse();
        _ = await Assert.That(json.Contains("\"properties\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Fixture_16_usability_attributes_should_map_to_annotations_and_exported_schema_keywords()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            public enum CustomerStatus
            {
                [SemanticEnumValue(DisplayName = "Active customer", Description = "Can place orders.")]
                Active = 1,
                Suspended = 2,
            }

            [SemanticType(SemanticTypeRole.Entity)]
            [SemanticDisplayName("Customer account")]
            [SemanticCategory("CRM")]
            [SemanticAnnotation("ui.placeholder", "Create a customer")]
            public sealed class Customer
            {
                [SemanticDisplayName("Email address")]
                [SemanticCategory("Contact")]
                [SemanticOrder(10)]
                [SemanticFormat(SemanticScalarFormat.Email)]
                [SemanticStringConstraints(MinLength = 5, MaxLength = 200, Pattern = ".+@.+")]
                [SemanticAnnotation("ui.placeholder", "name@example.com")]
                public required string Email { get; init; }

                [SemanticOrder(20)]
                [SemanticNumericConstraints(Minimum = 0, Maximum = 100, MultipleOf = 0.5)]
                public decimal Percent { get; init; }

                [SemanticCollectionConstraints(MinItems = 1, MaxItems = 3, UniqueItems = true)]
                public List<string> Tags { get; init; } = [];

                public CustomerStatus Status { get; init; }
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);
        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");
        PropertyShape email = customer.Properties.Single(static property => property.Name == "Email");
        PropertyShape percent = customer.Properties.Single(static property => property.Name == "Percent");
        PropertyShape tags = customer.Properties.Single(static property => property.Name == "Tags");
        EnumShape status = (EnumShape)model.GetShape("global::CustomerStatus");

        JsonSchemaExportResult export = JsonSchemaExporter.Export(model);
        JsonElement properties = export.Document.RootElement.GetProperty("properties");
        JsonElement emailJson = properties.GetProperty("Email");
        JsonElement percentJson = properties.GetProperty("Percent");
        JsonElement tagsJson = properties.GetProperty("Tags");

        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "schema.role" && annotation.Value == "Entity")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "ui.title" && annotation.Value == "Customer account")).IsTrue();
        _ = await Assert.That(customer.Annotations.Any(static annotation => annotation.Key == "ui.category" && annotation.Value == "CRM")).IsTrue();
        _ = await Assert.That(email.Annotations.Any(static annotation => annotation.Key == "ui.order" && annotation.Value == "10")).IsTrue();
        _ = await Assert.That(email.Annotations.Any(static annotation => annotation.Key == "schema.format" && annotation.Value == "email")).IsTrue();
        _ = await Assert.That(email.Annotations.Any(static annotation => annotation.Key == "schema.minLength" && annotation.Value == "5")).IsTrue();
        _ = await Assert.That(percent.Annotations.Any(static annotation => annotation.Key == "schema.multipleOf" && annotation.Value == "0.5")).IsTrue();
        _ = await Assert.That(tags.Annotations.Any(static annotation => annotation.Key == "schema.uniqueItems" && annotation.Value == "true")).IsTrue();
        _ = await Assert.That(status.Annotations.Any(static annotation => annotation.Key == "dotnet.enumDisplayNames")).IsTrue();
        _ = await Assert.That(emailJson.GetProperty("format").GetString()).IsEqualTo("email");
        _ = await Assert.That(emailJson.GetProperty("minLength").GetInt32()).IsEqualTo(5);
        _ = await Assert.That(percentJson.GetProperty("multipleOf").GetDouble()).IsEqualTo(0.5d);
        _ = await Assert.That(tagsJson.GetProperty("minItems").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(tagsJson.GetProperty("uniqueItems").GetBoolean()).IsTrue();
        _ = await Assert.That(diagnostics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Fixture_17_invalid_usability_attributes_should_report_stable_diagnostics()
    {
        const string source = """
            using System.Collections.Generic;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class InvalidAttributes
            {
                [SemanticAnnotation("invalid key", "value")]
                [SemanticStringConstraints(MinLength = 10, MaxLength = 2)]
                [SemanticFormat("email")]
                public string Code { get; init; } = string.Empty;

                [SemanticNumericConstraints(Minimum = 10, Maximum = 2)]
                public int Quantity { get; init; }

                [SemanticCollectionConstraints(MinItems = 3, MaxItems = 1)]
                public List<string> Tags { get; init; } = [];

                [SemanticFormat("uuid")]
                public List<int> Values { get; init; } = [];
            }
            """;

        (_, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source);

        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5020")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5022")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5023")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STM5024")).IsTrue();
        _ = await Assert.That(diagnostics.Count(static diagnostic => diagnostic.Id == "STM5025")).IsEqualTo(1);
    }


    [Test]
    public async Task Fixture_18_system_text_json_attributes_should_import_as_annotations()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Text.Json;
            using System.Text.Json.Serialization;
            using SemanticTypeModel.DotNet;

            public sealed class CustomerIdConverter : JsonConverter<int>
            {
                public override int Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => reader.GetInt32();
                public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
            }

            [SemanticType]
            public sealed class Customer
            {
                [SemanticName("Customer ID")]
                [JsonPropertyName("customer_id")]
                [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
                [JsonConverter(typeof(CustomerIdConverter))]
                public int Id { get; init; }

                [JsonIgnore]
                public required string Secret { get; init; }

                [JsonExtensionData]
                public Dictionary<string, JsonElement> Extra { get; init; } = [];
            }
            """;

        (TypeSchemaModel model, IReadOnlyList<Diagnostic> diagnostics) = GenerateModel(source, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelImportSystemTextJsonAttributes"] = "true",
        });

        ObjectShape customer = (ObjectShape)model.GetShape("global::Customer");
        PropertyShape id = customer.Properties.Single(static property => property.Annotations.Any(static annotation => annotation.Key == "systemTextJson.propertyName" && annotation.Value == "customer_id"));
        PropertyShape secret = customer.Properties.Single(static property => property.Name == "Secret");
        PropertyShape extra = customer.Properties.Single(static property => property.Name == "Extra");

        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "schema.title" && annotation.Value == "Customer ID")).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "systemTextJson.propertyName" && annotation.Value == "customer_id")).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "systemTextJson.numberHandling")).IsTrue();
        _ = await Assert.That(id.Annotations.Any(static annotation => annotation.Key == "systemTextJson.converter")).IsTrue();
        _ = await Assert.That(secret.Annotations.Any(static annotation => annotation.Key == "systemTextJson.ignore" && annotation.Value == "true")).IsTrue();
        _ = await Assert.That(extra.Annotations.Any(static annotation => annotation.Key == "systemTextJson.extensionData" && annotation.Value == "true")).IsTrue();
        _ = await Assert.That(diagnostics.Any(static diagnostic => diagnostic.Id == "STJ003")).IsTrue();
    }

    [Test]
    public async Task Fixture_19_system_text_json_name_policy_should_be_opt_in()
    {
        const string source = """
            using System.Text.Json.Serialization;
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                [SemanticName("Customer ID")]
                [JsonPropertyName("customer_id")]
                public int Id { get; init; }
            }
            """;

        (TypeSchemaModel defaultModel, IReadOnlyList<Diagnostic> defaultDiagnostics) = GenerateModel(source, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelImportSystemTextJsonAttributes"] = "true",
        });

        (TypeSchemaModel promotedModel, IReadOnlyList<Diagnostic> promotedDiagnostics) = GenerateModel(source, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelImportSystemTextJsonAttributes"] = "true",
            ["SemanticTypeModelUseJsonPropertyNameAsSemanticName"] = "true",
        });

        ObjectShape defaultCustomer = (ObjectShape)defaultModel.GetShape("global::Customer");
        ObjectShape promotedCustomer = (ObjectShape)promotedModel.GetShape("global::Customer");

        _ = await Assert.That(defaultCustomer.Properties.Any(static property => property.Name == "Customer ID")).IsTrue();
        _ = await Assert.That(promotedCustomer.Properties.Any(static property => property.Name == "customer_id")).IsTrue();
        _ = await Assert.That(defaultDiagnostics.Any(static diagnostic => diagnostic.Id.StartsWith("STJ", StringComparison.Ordinal))).IsFalse();
        _ = await Assert.That(promotedCustomer.Properties.Single(static property => property.Name == "customer_id").Annotations.Any(static annotation => annotation.Key == "systemTextJson.propertyName" && annotation.Value == "customer_id")).IsTrue();
    }


    [Test]
    public async Task Fixture_20_generator_should_reject_removed_system_text_json_context_options()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public required string Name { get; init; }
            }
            """;

        Diagnostic[] diagnostics = GenerateDiagnostics(source, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SemanticTypeModelGenerateSystemTextJsonContext"] = "true",
            ["SemanticTypeModelSystemTextJsonContextName"] = "RemovedContext",
        });

        _ = await Assert.That(diagnostics.Count(static diagnostic => diagnostic.Id == "STJ004")).IsEqualTo(2);
    }

    [Test]
    public async Task Fixture_21_generator_should_not_emit_system_text_json_context_source()
    {
        const string source = """
            using SemanticTypeModel.DotNet;

            [SemanticType]
            public sealed class Customer
            {
                public required string Name { get; init; }
            }
            """;

        string[] generatedHints = GenerateSourceHints(source);

        _ = await Assert.That(generatedHints).Contains("SemanticTypeModel.Generated.g.cs");
        _ = await Assert.That(generatedHints.Any(static hint => hint.Contains("SystemTextJsonContext", StringComparison.Ordinal))).IsFalse();
    }


    private static Diagnostic[] GenerateDiagnostics(string source, IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        AnalyzerConfigOptionsProvider optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);
        driver = driver.RunGenerators(compilation);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return runResult.Results.SelectMany(static result => result.Diagnostics).ToArray();
    }

    private static string[] GenerateSourceHints(string source, IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        AnalyzerConfigOptionsProvider optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);
        driver = driver.RunGenerators(compilation);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        return runResult.Results.SelectMany(static result => result.GeneratedSources).Select(static source => source.HintName).ToArray();
    }

    private static (TypeSchemaModel Model, IReadOnlyList<Diagnostic> Diagnostics) GenerateModel(
        string source,
        IReadOnlyDictionary<string, string>? globalOptions = null,
        string generatedProviderTypeName = "SemanticTypeModel.Generated.AppSemanticTypeModel")
    {
        CSharpCompilation compilation = CreateCompilation(source);
        IIncrementalGenerator generator = new SemanticTypeModelSourceGenerator();
        CSharpParseOptions parseOptions = (CSharpParseOptions)compilation.SyntaxTrees.First().Options;
        AnalyzerConfigOptionsProvider optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: optionsProvider);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> _);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        IReadOnlyList<Diagnostic> diagnostics = runResult.Results.SelectMany(static result => result.Diagnostics).ToArray();

        using var stream = new MemoryStream();
        EmitResult emitResult = outputCompilation.Emit(stream);
        if (!emitResult.Success)
        {
            string messages = string.Join(Environment.NewLine, emitResult.Diagnostics.Select(static diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException($"Compilation failed:{Environment.NewLine}{messages}");
        }

        stream.Position = 0;
        System.Reflection.Assembly generatedAssembly = System.Reflection.Assembly.Load(stream.ToArray());
        Type? providerType = generatedAssembly.GetType(generatedProviderTypeName, throwOnError: false, ignoreCase: false);
        if (providerType is null)
        {
            throw new InvalidOperationException("Generated provider type was not found.");
        }

        MethodInfo? createMethod = providerType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        if (createMethod is null)
        {
            throw new InvalidOperationException("Generated provider Create method was not found.");
        }

        var model = (TypeSchemaModel?)createMethod.Invoke(null, null);
        if (model is null)
        {
            throw new InvalidOperationException("Generated provider returned null.");
        }

        return (model, diagnostics);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        MetadataReference[] references = GetMetadataReferences();

        return CSharpCompilation.Create(
            assemblyName: $"SemanticTypeModel.GeneratorTest_{Guid.NewGuid():N}",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    private static MetadataReference[] GetMetadataReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        var references = new Dictionary<string, PortableExecutableReference>(StringComparer.Ordinal);
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            references[path] = MetadataReference.CreateFromFile(path);
        }

        AddReference(references, typeof(object).Assembly);
        AddReference(references, typeof(Enumerable).Assembly);
        AddReference(references, typeof(SemanticTypeAttribute).Assembly);
        AddReference(references, typeof(SemanticTypeModelSourceGenerator).Assembly);
        AddReference(references, typeof(TypeSchemaModel).Assembly);
        AddReference(references, typeof(JsonSchemaExporter).Assembly);
        AddReference(references, typeof(System.Text.Json.JsonDocument).Assembly);

        return [.. references.Values];
    }

    private static void AddReference(Dictionary<string, PortableExecutableReference> references, Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly.Location))
        {
            return;
        }

        references[assembly.Location] = MetadataReference.CreateFromFile(assembly.Location);
    }

    private sealed class TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string>? values) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _global = new TestAnalyzerConfigOptions(values ?? new Dictionary<string, string>(StringComparer.Ordinal));

        public override AnalyzerConfigOptions GlobalOptions => _global;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return _global;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return _global;
        }
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (values.TryGetValue(key, out string? configured))
            {
                value = configured;
                return true;
            }

            const string buildPropertyPrefix = "build_property.";
            if (key.StartsWith(buildPropertyPrefix, StringComparison.Ordinal)
                && values.TryGetValue(key[buildPropertyPrefix.Length..], out configured))
            {
                value = configured;
                return true;
            }

            value = null;
            return false;
        }
    }
}
