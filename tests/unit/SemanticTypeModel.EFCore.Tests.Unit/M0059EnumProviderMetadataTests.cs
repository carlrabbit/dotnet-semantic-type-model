#pragma warning disable CA1707, CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SemanticTypeModel.RealWorldFixtures;
using Regression = SemanticTypeModel.RealWorldFixtures.M0059ClrEnumRegression;

namespace SemanticTypeModel.EFCore.Tests.Unit;

public sealed class M0059EnumProviderMetadataTests
{
    [Test]
    public async Task M0059_enum_columns_have_string_provider_metadata_after_finalization()
    {
        var builder = new ModelBuilder();
        _ = builder.Entity<Regression.CsvSource>().HasNoKey();
        _ = builder.ApplySemanticTypeModel(FixtureModels.CreateM0059EnumRegression());

        IModel model = builder.FinalizeModel();
        IEntityType entity = model.FindEntityType(typeof(Regression.ImportJob))!;
        _ = await Assert.That(entity.FindProperty(nameof(Regression.ImportJob.SourceKind))!.GetProviderClrType()).IsEqualTo(typeof(string));
        _ = await Assert.That(entity.FindProperty(nameof(Regression.ImportJob.OptionalSourceKind))!.GetProviderClrType()).IsEqualTo(typeof(string));
        _ = await Assert.That(model.FindEntityType(typeof(Regression.CsvSource))).IsNull();
        _ = await Assert.That(entity.FindProperty(nameof(Regression.ImportJob.CsvSource))!.GetValueConverter()).IsNotNull();
    }
}
