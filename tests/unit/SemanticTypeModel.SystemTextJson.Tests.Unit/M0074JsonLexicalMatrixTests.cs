using System.Text.Json;
using System.Text.Json.Nodes;

namespace SemanticTypeModel.SystemTextJson.Tests.Unit;

#pragma warning disable JSON002
public sealed class M0074JsonLexicalMatrixTests
{
    [Test]
    public async Task Native_system_text_json_lexical_matrix_remains_the_baseline()
    {
        var options = new JsonSerializerOptions();
        _ = await Assert.That(JsonSerializer.Serialize(new DateOnly(2026, 9, 1), options)).IsEqualTo("\"2026-09-01\"");
        _ = await Assert.That(JsonSerializer.Serialize(new TimeOnly(12, 34, 56), options)).IsEqualTo("\"12:34:56\"");
        _ = await Assert.That(JsonSerializer.Serialize(new DateTime(2026, 9, 1, 12, 34, 56, DateTimeKind.Unspecified), options)).IsEqualTo("\"2026-09-01T12:34:56\"");
        _ = await Assert.That(JsonSerializer.Serialize(new DateTimeOffset(2026, 9, 1, 12, 34, 56, TimeSpan.FromHours(2)), options)).IsEqualTo("\"2026-09-01T12:34:56+02:00\"");
        _ = await Assert.That(JsonSerializer.Serialize(TimeSpan.FromMinutes(42), options)).IsEqualTo("\"00:42:00\"");
        _ = await Assert.That(JsonSerializer.Serialize(Guid.Parse("11111111-1111-1111-1111-111111111111"), options)).IsEqualTo("\"11111111-1111-1111-1111-111111111111\"");
        _ = await Assert.That(JsonSerializer.Serialize(new byte[] { 1, 2, 3, 4 }, options)).IsEqualTo("\"AQIDBA==\"");
        _ = await Assert.That(JsonSerializer.Serialize(new ReadOnlyMemory<byte>([1, 2, 3, 4]), options)).IsEqualTo("\"AQIDBA==\"");
        _ = await Assert.That(JsonSerializer.Serialize(new Uri("relative", UriKind.Relative), options)).IsEqualTo("\"relative\"");
        _ = await Assert.That(JsonSerializer.Serialize(JsonDocument.Parse("{\"a\":1}"), options)).IsEqualTo("{\"a\":1}");
        _ = await Assert.That(JsonSerializer.Serialize(JsonNode.Parse("[1, true, null]"), options)).IsEqualTo("[1,true,null]");
        _ = await Assert.That(JsonSerializer.Serialize(JsonDocument.Parse("42").RootElement, options)).IsEqualTo("42");
        _ = await Assert.That(JsonSerializer.Serialize(JsonDocument.Parse("null").RootElement, options)).IsEqualTo("null");
    }
}
#pragma warning restore JSON002
