using SemanticTypeModel.DotNet;

namespace SemanticTypeModel.SystemTextJson;

/// <summary>
/// Adds System.Text.Json integration to .NET extraction options.
/// </summary>
public static class SystemTextJsonExtractionExtensions
{
    /// <summary>
    /// Enables System.Text.Json attribute import and resolver customization settings for .NET extraction.
    /// </summary>
    public static DotNetExtractionOptions UseSystemTextJson(
        this DotNetExtractionOptions options,
        Action<SystemTextJsonProjectionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var projectionOptions = new SystemTextJsonProjectionOptions();
        configure?.Invoke(projectionOptions);

        return options with
        {
            SystemTextJson = new SystemTextJsonExtractionOptions
            {
                ImportAttributes = projectionOptions.ImportSystemTextJsonAttributes,
                UseJsonPropertyNameAsSerializationName = projectionOptions.UseJsonPropertyNameAsSerializationName,
                UseJsonPropertyNameAsSemanticName = projectionOptions.UseJsonPropertyNameAsSemanticName,
                PreserveUnsupportedConverterMetadata = projectionOptions.PreserveUnsupportedConverterMetadata,
            },
        };
    }
}
