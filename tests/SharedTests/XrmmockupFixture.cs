using DG.Some.Namespace;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Client;
using System;

public class XrmMockupFixture : IDisposable
{
#if XRM_MOCKUP_TEST_2011
    public XrmMockup2011 crm;
    public XrmMockup2011 crmRealData;
#elif XRM_MOCKUP_TEST_2013
    public XrmMockup2013 crm;
    public XrmMockup2013 crmRealData;
#elif XRM_MOCKUP_TEST_2015
    public XrmMockup2015 crm;
    public XrmMockup2015 crmRealData;
#elif XRM_MOCKUP_TEST_2016
    public XrmMockup2016 crm;
    public XrmMockup2016 crmRealData;
#elif XRM_MOCKUP_TEST_365
    public XrmMockup365 crm;
    public XrmMockup365 crmRealData;
#endif

    public XrmMockupFixture()
    {
        var settings = new XrmMockupSettings
        {
            BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif) },
            CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
            EnableProxyTypes = true,
            IncludeAllWorkflows = false,
            ExceptionFreeRequests = new string[] { "TestWrongRequest" },
            MetadataDirectoryPath = "../../../Metadata",
            IPluginMetadata = metaPlugins
        };

#if XRM_MOCKUP_TEST_2011
        crm = XrmMockup2011.GetInstance(settings);
#elif XRM_MOCKUP_TEST_2013
        crm = XrmMockup2013.GetInstance(settings);
#elif XRM_MOCKUP_TEST_2015
        crm = XrmMockup2015.GetInstance(settings);
#elif XRM_MOCKUP_TEST_2016
        crm = XrmMockup2016.GetInstance(settings);
#elif XRM_MOCKUP_TEST_365
        crm = XrmMockup365.GetInstance(settings);
#endif
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
#if XRM_MOCKUP_TEST_2011
            crmRealData = XrmMockup2011.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2013
            crmRealData = XrmMockup2013.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2015
            crmRealData = XrmMockup2015.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_2016
            crmRealData = XrmMockup2016.GetInstance(realDataSettings);
#elif XRM_MOCKUP_TEST_365
            crmRealData = XrmMockup365.GetInstance(realDataSettings);
#endif
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
#if XRM_MOCKUP_TEST_2011
            PluginTypeAssemblyName = "TestPluginAssembly11",
#elif XRM_MOCKUP_TEST_2013
            PluginTypeAssemblyName = "TestPluginAssembly13",
#elif XRM_MOCKUP_TEST_2015
            PluginTypeAssemblyName = "TestPluginAssembly15",
#elif XRM_MOCKUP_TEST_2016
            PluginTypeAssemblyName = "TestPluginAssembly16",
#elif XRM_MOCKUP_TEST_365
            PluginTypeAssemblyName = "TestPluginAssembly365",
#endif
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPreOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 20 //pre op as it only updates a field name
        },
        new MetaPlugin()
        {
#if XRM_MOCKUP_TEST_2011
            PluginTypeAssemblyName = "TestPluginAssembly11",
#elif XRM_MOCKUP_TEST_2013
            PluginTypeAssemblyName = "TestPluginAssembly13",
#elif XRM_MOCKUP_TEST_2015
            PluginTypeAssemblyName = "TestPluginAssembly15",
#elif XRM_MOCKUP_TEST_2016
            PluginTypeAssemblyName = "TestPluginAssembly16",
#elif XRM_MOCKUP_TEST_365
            PluginTypeAssemblyName = "TestPluginAssembly365",
#endif
            AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPostOp",
            MessageName = "Create",
            PrimaryEntity = "contact",
            Rank = 10,
            Stage = 40 //pre op as it only updates a field name
        }
    };
}