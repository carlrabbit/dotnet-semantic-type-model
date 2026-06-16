using SemanticTypeModel.Abstractions.Model;

namespace SemanticTypeModel.DependencyInjection;

internal sealed record RegisteredTypeSchemaProjection<TProjection>(ISchemaProjection<TProjection> Projection, ProjectionTarget Target);
