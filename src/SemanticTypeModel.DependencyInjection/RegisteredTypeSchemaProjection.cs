using SemanticTypeModel.Abstractions.Hardening;

namespace SemanticTypeModel.DependencyInjection;

internal sealed record RegisteredTypeSchemaProjection<TProjection>(ISchemaProjection<TProjection> Projection, ProjectionTarget Target);
