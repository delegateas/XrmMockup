namespace XrmMockup.MetadataGenerator.Tool;

/// <summary>
/// CLI option definitions for consistency.
/// </summary>
internal static class CliOptions
{
    internal static class Output
    {
        public const string Primary = "--output";
        public const string Alias = "-o";
        public const string Description = "Output directory for generated metadata files";
    }

    internal static class Solutions
    {
        public const string Primary = "--solutions";
        public const string Alias = "-s";
        public const string Description = "Comma-separated list of solution unique names";
    }

    internal static class Entities
    {
        public const string Primary = "--entities";
        public const string Alias = "-e";
        public const string Description = "Comma-separated list of additional entity logical names to include";
    }

    internal static class Config
    {
        public const string Primary = "--config";
        public const string Alias = "-c";
        public const string Description = "Path to appsettings.json configuration file";
    }

    internal static class PrettyPrint
    {
        public const string Primary = "--pretty-print";
        public const string Alias = "-p";
        public const string Description = "Format XML output for readability (increases file size)";
    }
}
