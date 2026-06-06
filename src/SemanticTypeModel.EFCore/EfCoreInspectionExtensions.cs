using System.Text;

namespace SemanticTypeModel.EFCore;

/// <summary>
/// Provides deterministic inspection text for EF Core domain semantic models.
/// </summary>
public static class EfCoreInspectionExtensions
{
    /// <summary>
    /// Formats an EF Core domain semantic model as deterministic text.
    /// </summary>
    public static string ToEfCoreSemanticText(this EfCoreSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var builder = new StringBuilder();
        _ = builder.Append("EF Core Semantic Model: ").AppendLine(model.Name);
        foreach (EfEntityTypeDefinition entity in model.EntityTypes.OrderBy(static entity => entity.Name, StringComparer.Ordinal))
        {
            _ = builder.Append("  Entity: ").Append(entity.Name);
            if (entity.IsOwned)
            {
                _ = builder.Append(" (owned)");
            }

            _ = builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(entity.TableName))
            {
                _ = builder.Append("    Table: ").Append(entity.TableName);
                if (!string.IsNullOrWhiteSpace(entity.SchemaName))
                {
                    _ = builder.Append(" Schema: ").Append(entity.SchemaName);
                }

                _ = builder.AppendLine();
            }

            if (entity.Inheritance is EfInheritanceDefinition inheritance)
            {
                _ = builder.Append("    Inheritance: ").Append(inheritance.Strategy);
                if (!string.IsNullOrWhiteSpace(inheritance.BaseEntity))
                {
                    _ = builder.Append(" Base: ").Append(inheritance.BaseEntity);
                }

                _ = builder.AppendLine();
            }

            foreach (EfPropertyDefinition property in entity.Properties.OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                _ = builder.Append("    Property: ").Append(property.Name).Append(" Clr: ").AppendLine(property.ClrType.Name);
            }

            foreach (EfKeyDefinition key in entity.Keys.OrderBy(static key => key.Name, StringComparer.Ordinal))
            {
                _ = builder.Append("    Key: ").Append(key.Name).Append(' ').Append(key.Kind).Append(" [").Append(string.Join(",", key.PropertyNames)).AppendLine("]");
            }

            foreach (EfIndexDefinition index in entity.Indexes.OrderBy(static index => index.Name, StringComparer.Ordinal))
            {
                _ = builder.Append("    Index: ").Append(index.Name).Append(index.IsUnique ? " Unique" : string.Empty).Append(" [").Append(string.Join(",", index.PropertyNames)).AppendLine("]");
            }

            foreach (EfRelationshipDefinition relationship in entity.Relationships.OrderBy(static relationship => relationship.Name, StringComparer.Ordinal))
            {
                _ = builder.Append("    Relationship: ").Append(relationship.Name).Append(' ').Append(relationship.Cardinality).Append(" -> ").AppendLine(relationship.PrincipalEntity);
            }
        }

        return builder.ToString();
    }
}
