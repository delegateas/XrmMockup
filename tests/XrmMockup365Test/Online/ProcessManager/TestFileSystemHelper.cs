using System;
using System.Collections.Generic;
using System.IO;
using DG.Tools.XrmMockup.Online;

namespace DG.XrmMockupTest.Online.ProcessManager
{
    /// <summary>
    /// Mock file system helper for unit testing ProxyDllFinder.
    /// Allows control over which files/directories "exist" and environment variables.
    /// </summary>
    public class TestFileSystemHelper : IFileSystemHelper
    {
        private readonly HashSet<string> _existingFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _existingDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string ExecutingAssemblyLocation { get; set; } = @"C:\test\XrmMockup365.dll";
        public string AssemblyInformationalVersion { get; set; } = "1.0.0";
        public string UserProfilePath { get; set; } = @"C:\Users\testuser";

        public void AddFile(string path)
        {
            _existingFiles.Add(path);
            // Also ensure parent directory exists
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                AddDirectory(dir);
            }
        }

        public void AddDirectory(string path)
        {
            _existingDirectories.Add(path);
            // Also add parent directories
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !_existingDirectories.Contains(parent))
            {
                AddDirectory(parent);
            }
        }

        public void SetEnvironmentVariable(string name, string value)
        {
            _environmentVariables[name] = value;
        }

        public bool FileExists(string path) => _existingFiles.Contains(path);

        public bool DirectoryExists(string path) => _existingDirectories.Contains(path);

        public string GetEnvironmentVariable(string name)
        {
            string value;
            return _environmentVariables.TryGetValue(name, out value) ? value : null;
        }

        public string GetExecutingAssemblyLocation() => ExecutingAssemblyLocation;

        public string GetAssemblyInformationalVersion() => AssemblyInformationalVersion;

        public string GetUserProfilePath() => UserProfilePath;

        public string GetParentDirectory(string path)
        {
            var parent = Path.GetDirectoryName(path);
            return parent;
        }
    }
}
