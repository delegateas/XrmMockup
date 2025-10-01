using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using XrmPluginCore;
using Microsoft.Xrm.Sdk.Client;
using System;
using TestPluginAssembly365.Plugins.LegacyDaxif;
using TestPluginAssembly365.Plugins.ServiceBased;

public class XrmMockupFixture : IDisposable
{
    // Shared settings instances to ensure metadata cache hits
    private static XrmMockupSettings _sharedSettings;
    private static XrmMockupSettings _sharedRealDataSettings;
    private static readonly object _settingsLock = new object();
    
    public XrmMockupSettings Settings => _sharedSettings;
    public XrmMockupSettings RealDataSettings => _sharedRealDataSettings;

    public XrmMockupFixture()
    {
        lock (_settingsLock)
        {
            if (_sharedSettings == null)
            {
                _sharedSettings = new XrmMockupSettings
                {
                    BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif), typeof(LegacyPlugin), typeof(DIPlugin) },
                    BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(CustomAPI)), new Tuple<string, Type>("dg", typeof(LegacyCustomApi)) },
                    CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                    EnableProxyTypes = true,
                    IncludeAllWorkflows = false,
                    ExceptionFreeRequests = new string[] { "TestWrongRequest" },
                    MetadataDirectoryPath = GetMetadataPath(),
                    IPluginMetadata = metaPlugins
                };

                try
                {
                    _sharedRealDataSettings = new XrmMockupSettings
                    {
                        BasePluginTypes = _sharedSettings.BasePluginTypes,
                        CodeActivityInstanceTypes = _sharedSettings.CodeActivityInstanceTypes,
                        EnableProxyTypes = _sharedSettings.EnableProxyTypes,
                        IncludeAllWorkflows = _sharedSettings.IncludeAllWorkflows,
                        ExceptionFreeRequests = _sharedSettings.ExceptionFreeRequests,
                        MetadataDirectoryPath = GetMetadataPath(),
                        OnlineEnvironment = new Env
                        {
                            providerType = AuthenticationProviderType.OnlineFederation,
                            uri = "https://exampleURL/XRMServices/2011/Organization.svc",
                            username = "exampleUser",
                            password = "examplePass"
                        }
                    };
                }
                catch
                {
                    // ignore - set to null
                    _sharedRealDataSettings = null;
                }
            }
        }
    }

    public void Dispose()
    {
    }

    private static string GetMetadataPath()
    {
        // Find the test project directory by looking for the Metadata folder
        var currentDir = System.IO.Directory.GetCurrentDirectory();
        var metadataPath = System.IO.Path.Combine(currentDir, "Metadata");
        
        if (System.IO.Directory.Exists(metadataPath))
        {
            return metadataPath;
        }
        
        // If not found in current directory, try relative paths from bin output
        var testProjectPaths = new[]
        {
            System.IO.Path.Combine(currentDir, "..", "..", "..", "Metadata"),
            System.IO.Path.Combine(currentDir, "Metadata"),
            "Metadata"
        };
        
        foreach (var path in testProjectPaths)
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            if (System.IO.Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }
        
        throw new System.IO.DirectoryNotFoundException($"Could not find Metadata directory. Searched in: {currentDir}");
    }

    private static MetaPlugin[] metaPlugins = new MetaPlugin[]
    {
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPreOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 20 //pre op as it only updates a field name
        },
        new MetaPlugin()
        {
            PluginTypeAssemblyName = "TestPluginAssembly365",
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPostOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 40 //pre op as it only updates a field name
        }
    };
}