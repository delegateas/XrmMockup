using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;

public class XrmMockupFixture : IDisposable
{
    public XrmMockup365 crm;
    public XrmMockup365 crmRealData;

    public XrmMockupFixture()
    {
        var settings = new XrmMockupSettings
        {
            BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif) },
            BaseCustomApiTypes = new[] { new Tuple<string, Type>("dg", typeof(CustomAPI)) },
            CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
            EnableProxyTypes = true,
            IncludeAllWorkflows = false,
            ExceptionFreeRequests = new string[] { "TestWrongRequest" },
            MetadataDirectoryPath = "../../../Metadata",
            IPluginMetadata = metaPlugins
        };

        crm = XrmMockup365.GetInstance(settings);

        try
        {
            var realDataSettings = new XrmMockupSettings
            {
                BasePluginTypes = settings.BasePluginTypes,
                CodeActivityInstanceTypes = settings.CodeActivityInstanceTypes,
                EnableProxyTypes = settings.EnableProxyTypes,
                IncludeAllWorkflows = settings.IncludeAllWorkflows,
                ExceptionFreeRequests = settings.ExceptionFreeRequests,
                OnlineEnvironment = new Env
                {
                    providerType = AuthenticationProviderType.OnlineFederation,
                    uri = "https://exampleURL/XRMServices/2011/Organization.svc",
                    username = "exampleUser",
                    password = "examplePass"
                }
            };
            crmRealData = XrmMockup365.GetInstance(realDataSettings);
        }
        catch
        {
            // ignore
        }
    }

    public void Dispose()
    {
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