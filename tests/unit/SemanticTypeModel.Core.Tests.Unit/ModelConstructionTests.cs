using System.Diagnostics.CodeAnalysis;
using SemanticTypeModel.Abstractions.Model;
using SemanticTypeModel.Core.Building;

namespace SemanticTypeModel.Core.Tests.Unit;

/// <summary>
/// Verifies runtime model construction and traversal behavior.
/// </summary>
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class ModelConstructionTests
{
    /// <summary>
    /// Verifies root registration and named lookup for a built model.
    /// </summary>
    [Test]
    public async Task Build_should_create_model_with_root_and_named_shape_lookup()
    {
        var builder = new TypeSchemaModelBuilder();
        var child = new ScalarShape { Kind = ScalarKind.String };
        var root = new ObjectShape
        {
            Properties =
            [
                new PropertyShape
                {
                    Name = "child",
                    IsRequired = true,
                    Type = ShapeRef.FromIdentifier("Child"),
                },
            ],
        };

        TypeSchemaModel model = builder
            .AddShape("Child", child)
            .AddShape("Root", root)
            .SetRoot("Root")
            .Build();

        _ = await Assert.That(model.RootIdentifier).IsEqualTo("Root");
        _ = await Assert.That(model.Root).IsNotNull();
        _ = await Assert.That(model.GetShape("Child")).IsEqualTo(child with { Identifier = "Child" });
        _ = await Assert.That(model.TryGetShape("Missing") is null).IsTrue();
    }

    /// <summary>
    /// Verifies traversal includes inline children while de-duplicating named shapes.
    /// </summary>
    [Test]
    public async Task TraverseAll_should_visit_named_and_inline_shapes_once()
    {
        var builder = new TypeSchemaModelBuilder();
        var inlineScalar = new ScalarShape { Kind = ScalarKind.Integer };
        var child = new ObjectShape
        {
            Properties =
            [
                new PropertyShape
                {
                    Name = "inline",
                    Type = ShapeRef.FromInline(inlineScalar),
                },
            ],
        };
        var root = new ObjectShape
        {
            Properties =
            [
                new PropertyShape
                {
                    Name = "child",
                    Type = ShapeRef.FromIdentifier("Child"),
                },
                new PropertyShape
                {
                    Name = "childAgain",
                    Type = ShapeRef.FromIdentifier("Child"),
                },
            ],
        };

        TypeSchemaModel model = builder
            .AddShape("Root", root)
            .AddShape("Child", child)
            .SetRoot("Root")
            .Build();

        var traversed = model.TraverseAll().ToList();

        _ = await Assert.That(traversed.Count).IsEqualTo(3);
        _ = await Assert.That(traversed.OfType<ObjectShape>().Count()).IsEqualTo(2);
        _ = await Assert.That(traversed.OfType<ScalarShape>().Single()).IsEqualTo(inlineScalar);
    }

    /// <summary>
    /// Verifies build fails when the declared root shape is absent.
    /// </summary>
    [Test]
    public async Task Build_should_throw_when_root_identifier_is_missing()
    {
        TypeSchemaModelBuilder builder = new TypeSchemaModelBuilder()
            .AddShape("Known", new ScalarShape { Kind = ScalarKind.String })
            .SetRoot("Missing");

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(builder.Build()));

        _ = await Assert.That(exception).IsNotNull();
        _ = await Assert.That(exception!.Message).IsEqualTo("Root identifier 'Missing' is not registered as a shape.");
    }

    /// <summary>
    /// Verifies build fails when a named shape reference cannot be resolved.
    /// </summary>
    [Test]
    public async Task Build_should_throw_when_named_reference_is_unresolved()
    {
        TypeSchemaModelBuilder builder = new TypeSchemaModelBuilder()
            .AddShape(
                "Root",
                new ObjectShape
                {
                    Properties =
                    [
                        new PropertyShape
                        {
                            Name = "child",
                            Type = ShapeRef.FromIdentifier("Missing"),
                        },
                    ],
                })
            .SetRoot("Root");

        InvalidOperationException? exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(builder.Build()));

        _ = await Assert.That(exception).IsNotNull();
        _ = await Assert.That(exception!.Message).IsEqualTo("Shape reference 'Missing' cannot be resolved in this model.");
    }

    /// <summary>
    /// Verifies shape references resolve against either named or inline shapes.
    /// </summary>
    [Test]
    public async Task ShapeRef_should_resolve_named_and_inline_shapes()
    {
        var inline = new ScalarShape { Kind = ScalarKind.Boolean };
        TypeSchemaModel model = new TypeSchemaModelBuilder()
            .AddShape("Root", new ScalarShape { Kind = ScalarKind.String })
            .SetRoot("Root")
            .Build();

        TypeShape namedResolved = ShapeRef.FromIdentifier("Root").Resolve(model);
        TypeShape inlineResolved = ShapeRef.FromInline(inline).Resolve(model);

        _ = await Assert.That(namedResolved).IsEqualTo(model.Root);
        _ = await Assert.That(inlineResolved).IsEqualTo(inline);
    }

    /// <summary>
    /// Verifies a built model remains unchanged after later builder mutations.
    /// </summary>
    [Test]
    public async Task Built_model_should_be_immune_to_builder_mutation()
    {
        TypeSchemaModelBuilder builder = new TypeSchemaModelBuilder()
            .AddShape("Root", new ScalarShape { Kind = ScalarKind.String })
            .SetRoot("Root");

        TypeSchemaModel model = builder.Build();
        _ = builder.AddShape("Root", new ScalarShape { Kind = ScalarKind.Integer });

        var root = model.Root as ScalarShape;

        _ = await Assert.That(root).IsNotNull();
        _ = await Assert.That(root!.Kind).IsEqualTo(ScalarKind.String);
    }
}
