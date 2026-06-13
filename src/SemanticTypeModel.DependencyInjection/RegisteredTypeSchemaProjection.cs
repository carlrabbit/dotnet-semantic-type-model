using SemanticTypeModel.Abstractions.Canonical;

namespace SemanticTypeModel.DependencyInjection;

internal sealed record RegisteredTypeSchemaProjection<TProjection>(ISchemaProjection<TProjection> Projection, ProjectionTarget Target);
