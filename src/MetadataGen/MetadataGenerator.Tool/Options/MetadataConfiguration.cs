namespace XrmMockup.MetadataGenerator.Tool.Options;

/// <summary>
/// Configuration for metadata generation from appsettings.json.
/// </summary>
public record MetadataConfiguration
{
    /// <summary>
    /// The configuration section path.
    /// </summary>
    public const string SectionPath = "XrmMockup:Metadata";

    /// <summary>
    /// Output directory for generated metadata files.
    /// </summary>
    public string OutputDirectory { get; init; } = "./Metadata";

    /// <summary>
    /// Solution names to extract metadata from.
    /// </summary>
    public string[] Solutions { get; init; } = [];

    /// <summary>
    /// Additional entity logical names to include.
    /// </summary>
    public string[] Entities { get; init; } = [];

    /// <summary>
    /// Whether to format XML output for readability.
    /// </summary>
    public bool PrettyPrint { get; init; }
}
