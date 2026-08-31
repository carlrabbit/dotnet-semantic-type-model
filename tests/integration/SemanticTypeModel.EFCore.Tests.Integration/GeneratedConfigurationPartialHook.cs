using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InventoryItem = SemanticTypeModel.TestModels.ModelA.InventoryItem;
namespace SemanticTypeModel.TestModels.ModelA;

internal sealed partial class SemanticTypeModel_TestModels_ModelA_InventoryItemConfiguration
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
