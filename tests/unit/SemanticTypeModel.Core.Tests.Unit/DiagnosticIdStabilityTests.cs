using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SemanticTypeModel.Core.Diagnostics;

namespace SemanticTypeModel.Core.Tests.Unit;

/// <summary>
/// Verifies that <see cref="StmDiagnosticIds"/> contains no duplicate IDs and
/// that each ID has the correct STM format.
/// </summary>
#pragma warning disable CS1591
[SuppressMessage("Naming", "CA1707:Remove the underscores from member name", Justification = "Test names may use underscores for readability.")]
public sealed class DiagnosticIdStabilityTests
{
    [Test]
    public async Task StmDiagnosticIds_should_have_no_duplicate_values()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(StmDiagnosticIds));
        var duplicates = ids
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToList();

        _ = await Assert.That(duplicates).IsEmpty()
            .Because($"Duplicate diagnostic IDs found in StmDiagnosticIds: {string.Join(", ", duplicates)}");
    }

    [Test]
    public async Task StmDiagnosticIds_should_all_use_stm_prefix_and_numeric_suffix()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(StmDiagnosticIds));

        foreach (string id in ids)
        {
            var isValid = id.StartsWith("STM", StringComparison.Ordinal)
                && id.Length > 3
                && id[3..].All(char.IsDigit);

            _ = await Assert.That(isValid).IsTrue()
                .Because($"Diagnostic ID '{id}' does not match the STMxxxx format.");
        }
    }

    [Test]
    public async Task StmDiagnosticIds_stm0xxx_range_should_contain_all_model_validation_codes()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(StmDiagnosticIds));
        var stm0xxx = ids.Where(static id => id.StartsWith("STM0", StringComparison.Ordinal)).ToList();

        // STM0001-STM0013 are the documented model validation codes.
        _ = await Assert.That(stm0xxx.Count).IsGreaterThanOrEqualTo(13);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.DuplicateTypeId);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.UnresolvedTypeRef);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.DuplicatePropertyName);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.DuplicateKeyName);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.KeyPropertyRefMissing);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.RelationshipTypeMissing);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.RelationshipPropertyRefMissing);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.InvalidCardinalityBounds);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.InvalidStringConstraintBounds);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.InvalidNumericConstraintBounds);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.MalformedAnnotationKey);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.DuplicateEnumValueName);
        _ = await Assert.That(stm0xxx).Contains(StmDiagnosticIds.DuplicateEnumValuePayload);
    }

    [Test]
    public async Task StmDiagnosticIds_stm3xxx_range_should_contain_all_json_schema_runtime_codes()
    {
        IReadOnlyList<string> ids = CollectStringConstants(typeof(StmDiagnosticIds));
        var stm3xxx = ids.Where(static id => id.StartsWith("STM3", StringComparison.Ordinal)).ToList();

        // STM3201-STM3207 are the documented JSON Schema runtime projection codes.
        _ = await Assert.That(stm3xxx.Count).IsGreaterThanOrEqualTo(7);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeRootFallback);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeSemanticMembersSkipped);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeDictionaryKeyMetadataLost);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeUnionApproximated);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeIntersectionApproximated);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeUnsupportedTypeKind);
        _ = await Assert.That(stm3xxx).Contains(StmDiagnosticIds.JsonSchemaRuntimeNonStringEnumValue);
    }

    private static IReadOnlyList<string> CollectStringConstants(Type type)
    {
        return
        [
            .. type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(static field => (string)field.GetRawConstantValue()!)
                .Where(static value => value is not null),
        ];
    }
}
#pragma warning restore CS1591
