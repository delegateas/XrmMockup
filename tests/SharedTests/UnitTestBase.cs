using System;
using DG.Tools;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Client;

namespace DG.XrmMockupTest {

    [TestClass]
    public class UnitTestBase {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;
#if XRM_MOCKUP_TEST_2011
        protected static XrmMockup2011 crm;
        protected static XrmMockup2011 crmRealData;
#elif XRM_MOCKUP_TEST_2013
        protected static XrmMockup2013 crm;
        protected static XrmMockup2013 crmRealData;
#elif XRM_MOCKUP_TEST_2015
        protected static XrmMockup2015 crm;
        protected static XrmMockup2015 crmRealData;
#elif XRM_MOCKUP_TEST_2016
        protected static XrmMockup2016 crm;
        protected static XrmMockup2016 crmRealData;
#elif XRM_MOCKUP_TEST_365
        protected static XrmMockup365 crm;
        protected static XrmMockup365 crmRealData;
#endif

        public UnitTestBase() {
            this.orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            this.orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            this.orgAdminService = crm.GetAdminService();
            if(crmRealData != null)
                this.orgRealDataService = crmRealData.GetAdminService();
        }

        [TestCleanup]
        public void TestCleanup() {
            crm.ResetEnvironment();
        }


        [AssemblyInitialize]
        public static void InitializeServices(TestContext context) {
            InitializeMockup(context);
        }

        public static void InitializeMockup(TestContext context) {
            var settings = new XrmMockupSettings {
                BasePluginTypes = new Type[] { typeof(DG.Some.Namespace.Plugin), typeof(PluginNonDaxif) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false,
                ExceptionFreeRequests = new string[] { "TestWrongRequest" },
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
