using System;
using DG.Tools;
using DG.Some.Namespace;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.Tools.XrmMockup;

namespace DG.XrmMockupTest {

    [TestClass]
    public class UnitTestBase {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
#if XRM_MOCKUP_TEST_2011
        protected static XrmMockup2011 crm;
#elif XRM_MOCKUP_TEST_2013
        protected static XrmMockup2013 crm;
#elif XRM_MOCKUP_TEST_2015
        protected static XrmMockup2015 crm;
#elif XRM_MOCKUP_TEST_2016
         protected static XrmMockup2016 crm;
#elif XRM_MOCKUP_TEST_365
         protected static XrmMockup365 crm;
#endif

        public UnitTestBase() {
            this.orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            this.orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            this.orgAdminService = crm.GetAdminService(); 
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
                BasePluginTypes = new Type[] { typeof(Plugin), typeof(PluginNonDaxif) },
                CodeActivityInstanceTypes = new Type[] { typeof(AccountWorkflowActivity) },
                EnableProxyTypes = true,
                IncludeAllWorkflows = false
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
        }
    }

}
