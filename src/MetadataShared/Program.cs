using DG.Tools;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DG.Tools.XrmMockup.Metadata
{
    class Program {
        static List<string> listOfAssemblies = new List<string>();
        static Program() {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveXrmAssemblies;
        }

        private static Assembly ResolveXrmAssemblies(object sender, ResolveEventArgs args) {
            if (listOfAssemblies.Contains(args.Name)) {
                return null;
            }
            try {
                listOfAssemblies.Add(args.Name);
                return AssemblyGetter.GetAssemblyFromName(args.Name);
            } finally {
                listOfAssemblies.Remove(args.Name);
            }
        }

    static void Main(string[] args) {
            var generator = new DataHelper();
            var skeleton = generator.GetMetadata(AssemblyGetter.GetProjectPath());
            var serializer = new DataContractSerializer(typeof(MetadataSkeleton));
            var workflowSerializer = new DataContractSerializer(typeof(Entity));
            var securitySerializer = new DataContractSerializer(typeof(SecurityRole));
            var location = Directory.GetCurrentDirectory();
            var workflowsLocation = Path.Combine(location, "Workflows");
            var securityLocation = Path.Combine(location, "SecurityRoles");

            Console.WriteLine("Deleting old files");

            Directory.CreateDirectory(workflowsLocation);
            foreach (var file in Directory.EnumerateFiles(workflowsLocation, "*.xml")) {
                File.Delete(Path.Combine(workflowsLocation, file));
            }

            Directory.CreateDirectory(securityLocation);
            foreach (var file in Directory.EnumerateFiles(securityLocation, "*.xml")) {
                File.Delete(Path.Combine(securityLocation, file));
            }

            Console.WriteLine("Writing files");

            Directory.CreateDirectory(location);
            using (var stream = new FileStream(location + "/Metadata.xml", FileMode.Create)) {
                serializer.WriteObject(stream, skeleton);
            }

            foreach (var workflow in generator.GetWorkflows()) {
                var safeName = ToSafeName(workflow.Attributes["name"] as string);
                using (var stream = new FileStream(workflowsLocation + "/" + safeName + ".xml", FileMode.Create)) {
                    workflowSerializer.WriteObject(stream, workflow);
                }
            }
            var securityRoles = generator.GetSecurityRoles(skeleton.RootBusinessUnit.Id);
            foreach (var securityRole in securityRoles) {
                var safeName = ToSafeName(securityRole.Value.Name);
                using (var stream = new FileStream(securityLocation + "/" + safeName + ".xml", FileMode.Create)) {
                    securitySerializer.WriteObject(stream, securityRole.Value);
                }
            }

            // Write to TypeDeclarations file
            var typedefFile = Path.Combine(location, "TypeDeclarations.cs");

            using (var file = new StreamWriter(typedefFile, false)) {
                file.WriteLine("using System;");
                file.WriteLine("");
                file.WriteLine("namespace DG.Tools.XrmMockup {");
                file.WriteLine("\tpublic struct SecurityRoles {");
                foreach (var securityRole in securityRoles.OrderBy(x => x.Value.Name)) {
                    file.WriteLine($"\t\tpublic static Guid {ToSafeName(securityRole.Value.Name)} = new Guid(\"{securityRole.Key.ToString()}\");");
                }
                file.WriteLine("\t}");
                file.WriteLine("}");
            }
        }

        private static bool StartsWithNumber(string str) {
            return str.Length > 0 && str[0] >= '0' && str[0] <= '9';
        }

        private static string ToSafeName(string str) {
            var compressed = Regex.Replace(str, @"[^\w]", "");
            if (StartsWithNumber(compressed)) {
                return $"_{compressed}";
            }
            if (compressed == "") {
                return "_EmptyString";
            }
            return compressed;
        }




    }
}
