namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Abstraction for file system operations to enable testing.
    /// </summary>
    internal interface IFileSystemHelper
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        string GetEnvironmentVariable(string name);
        string GetExecutingAssemblyLocation();
        string GetAssemblyInformationalVersion();
        string GetUserProfilePath();
        string GetParentDirectory(string path);
    }
}
