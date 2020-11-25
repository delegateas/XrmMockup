using System;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class UnitTestBase
    {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;


#if XRM_MOCKUP_TEST_2011
        static protected XrmMockup2011 crm;
        static protected XrmMockup2011 crmRealData;
#elif XRM_MOCKUP_TEST_2013
        static protected XrmMockup2013 crm;
        static protected XrmMockup2013 crmRealData;
#elif XRM_MOCKUP_TEST_2015
        static protected XrmMockup2015 crm;
        static protected XrmMockup2015 crmRealData;
#elif XRM_MOCKUP_TEST_2016
        static protected XrmMockup2016 crm;
        static XrmMockup2016 crmRealData;
#elif XRM_MOCKUP_TEST_365
        static protected XrmMockup365 crm;
        static protected XrmMockup365 crmRealData;
#endif

        public UnitTestBase()
        {
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
            if (crmRealData != null)
                orgRealDataService = crmRealData.GetAdminService();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            crm.ResetEnvironment();
        }


        [AssemblyInitialize]
        public static void InitializeServices(TestContext context)
        {
            InitializeMockup(context);
        }

        public static void InitializeMockup(TestContext context)
        {
            var additionalPluginMetadata = new MetaPlugin();
            additionalPluginMetadata.AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPreOp";
            additionalPluginMetadata.MessageName = "Create";
            additionalPluginMetadata.PrimaryEntity = "contact";
            additionalPluginMetadata.Rank = 10;
            additionalPluginMetadata.Stage = 20; //pre op as it only updates a field name

            var additionalPluginMetadata2 = new MetaPlugin();
            additionalPluginMetadata2.AssemblyName = "DG.Some.Namespace.ContactIPluginDirectPostOp";
            additionalPluginMetadata2.MessageName = "Create";
            additionalPluginMetadata2.PrimaryEntity = "contact";
            additionalPluginMetadata2.Rank = 10;
            additionalPluginMetadata2.Stage = 40; //pre op as it only updates a field name

            var settings = new XrmMockupSettings
            {
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                ExceptionFreeRequests = new string[] { "TestWrongRequest" },
                MetadataDirectoryPath = "../../../Metadata",
                IPluginMetadata = new MetaPlugin[] { additionalPluginMetadata , additionalPluginMetadata2 }
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
    }
}