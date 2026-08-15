using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace SemanticTypeModel.M0060.ModelA;

internal sealed partial class InventoryItemConfiguration
{
    static partial void ConfigureBeforeGenerated(EntityTypeBuilder<InventoryItem> builder)
    {
        _ = builder.HasAnnotation("M0060.BeforeGenerated", true);
    }

    static partial void ConfigureAfterGenerated(EntityTypeBuilder<InventoryItem> builder)
    {
        _ = builder.HasIndex(entity => entity.DisplayName).IsUnique();
    }
}
