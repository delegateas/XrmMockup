using System;
using System.IO;
using System.Reflection;

namespace DG.Tools.XrmMockup.Online
{
    /// <summary>
    /// Default implementation of IFileSystemHelper using real file system operations.
    /// </summary>
    internal class DefaultFileSystemHelper : IFileSystemHelper
    {
        public bool FileExists(string path) => File.Exists(path);

        public bool DirectoryExists(string path) => Directory.Exists(path);

        public string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

        public string GetExecutingAssemblyLocation() => Assembly.GetExecutingAssembly().Location;

        public string GetAssemblyInformationalVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        public string GetUserProfilePath() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string GetParentDirectory(string path)
        {
            var parent = Directory.GetParent(path);
            return parent?.FullName;
        }
    }
}
