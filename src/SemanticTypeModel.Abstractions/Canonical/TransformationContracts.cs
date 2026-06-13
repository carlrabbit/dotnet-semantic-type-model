namespace SemanticTypeModel.Abstractions.Canonical;

#pragma warning disable CS1591
public interface ISchemaTransformation
{
    ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context, CancellationToken cancellationToken = default);
}

public interface ISchemaProjection<T>
{
    T Project(TypeSchemaModel model, SchemaProjectionContext context);
}
#pragma warning restore CS1591
