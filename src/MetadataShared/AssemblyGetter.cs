using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup.Metadata
{
    internal static class AssemblyGetter {
        private static XNamespace msbuild = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

        internal static Assembly GetAssemblyFromName(string Name) {
            var projectFilePath = Directory.GetFiles(GetProjectPath(), "*.csproj")[0];
            var projectFile = XDocument.Load(projectFilePath);
            var references = 
                projectFile.Root
                .Descendants()
                .Where(e => e.Name.LocalName == "ItemGroup")
                .Elements()
                .Where(e => e.Name.LocalName == "Reference" && e.Attribute("Include") != null && e.HasElements);

            var referenceLookup = new Dictionary<string, string>();
            foreach (var reference in references) {
                if (reference.Attribute("Include") != null && reference.Element("HintPath") != null) {
                    var key = reference.Attribute("Include").Value;
                    if (key.Contains(",")) {
                        key = key.Split(',')[0];
                    }
                    referenceLookup.Add(key, reference.Element("HintPath").Value);
                }
            }
            if (Name.Contains(",")) {
                Name = Name.Split(',')[0];
            }
            
            if (referenceLookup.ContainsKey(Name)) {
                return Assembly.LoadFile(Path.Combine(projectFilePath, "..", referenceLookup[Name]));
            }

            return null;
        }

        internal static AssemblyName[] GetAssembliesInBuildPath() {
            var projectPath = GetProjectPath();
            var projectFilePath = Directory.GetFiles(projectPath, "*.csproj")[0];
            var projectFile = XDocument.Load(projectFilePath);

            return projectFile
                .Element(msbuild + "Project")
                .Elements(msbuild + "PropertyGroup")
                .Where(e => e.Element(msbuild + "OutputPath") != null)
                .Select(e => e.Element(msbuild + "OutputPath").Value)
                .Where(path => Directory.Exists(Path.Combine(projectPath, path)))
                .Select(path => Directory.GetFiles(Path.Combine(projectPath, path), "*.dll"))
                .Select(dirs => dirs.Select(file => AssemblyName.GetAssemblyName(file)))
                .Aggregate((current, next) => current.Concat(next))
                .GroupBy(file => file.Name,
                    (name, files) => files.Aggregate((highest, next) => next.Version > highest.Version ? next : highest))
                .ToArray();
        }


        internal static string GetProjectPath() {
            var startDirectory = Program.ParsedArgs[Arguments.UnitTestProjectPath] ?? Directory.GetCurrentDirectory();
            var owncsproj = "MetadataGenerator365.csproj";
            var current = startDirectory;
            try {
                while (Directory.GetFiles(current, "*.csproj").Length == 0 || Directory.GetFiles(current, "*.csproj")[0].EndsWith(owncsproj)) {
                    current = Directory.GetParent(current).FullName;
                }
               return current;

            } catch (Exception e) {
                throw new InvalidOperationException(
                    $"Unable to find path to the unit test project in any parent folder of '{startDirectory}'. " +
                    $"Provide a path to the folder which contains the .csproj file with the '{Arguments.UnitTestProjectPath.Name}'-argument.", e);
            }
        }
    }
}
