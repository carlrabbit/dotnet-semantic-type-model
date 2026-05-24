namespace SemanticTypeModel.Abstractions.Hardening;

#pragma warning disable CS1591
public interface ISchemaTransformation
{
    ValueTask TransformAsync(TypeSchemaModelBuilder model, SchemaTransformContext context);
}

public interface ISchemaProjection<T>
{
    T Project(TypeSchemaModel model, SchemaProjectionContext context);
}
#pragma warning restore CS1591
