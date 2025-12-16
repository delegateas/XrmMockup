namespace XrmMockup.MetadataGenerator.Core.Models;

/// <summary>
/// Options for metadata generation.
/// </summary>
public record GeneratorOptions
{
    /// <summary>
    /// Output directory for generated metadata files.
    /// </summary>
    public required string OutputDirectory { get; init; }

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
    /// Default is false to minimize file size.
    /// </summary>
    public bool PrettyPrint { get; init; }

    /// <summary>
    /// Default entities that are always included.
    /// </summary>
    public static readonly string[] DefaultEntities =
    [
        "businessunit",
        "systemuser",
        "transactioncurrency",
        "role",
        "systemuserroles",
        "team",
        "teamroles",
        "activitypointer",
        "roletemplate",
        "fileattachment",
        "workflow"
    ];
}
