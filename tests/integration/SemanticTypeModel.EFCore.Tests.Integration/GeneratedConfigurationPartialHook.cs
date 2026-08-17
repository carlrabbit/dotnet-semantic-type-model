using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace SemanticTypeModel.EFCore.CompatibilityModel;

internal sealed partial class InventoryItemConfiguration
{
    static partial void ConfigureBeforeGenerated(EntityTypeBuilder<InventoryItem> builder)
    {
        _ = builder.HasAnnotation("Compatibility.BeforeGenerated", true);
    }

    static partial void ConfigureAfterGenerated(EntityTypeBuilder<InventoryItem> builder)
    {
        _ = builder.HasIndex(entity => entity.DisplayName).IsUnique();
    }
}
