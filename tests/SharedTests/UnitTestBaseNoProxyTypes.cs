using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    [Collection("Xrm Collection No Proxy Types")]
    public class UnitTestBaseNoProxyTypes
    {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;

#if XRM_MOCKUP_TEST_2011
        static protected XrmMockup2011 crm;
#elif XRM_MOCKUP_TEST_2013
        static protected XrmMockup2013 crm;
#elif XRM_MOCKUP_TEST_2015
        static protected XrmMockup2015 crm;
#elif XRM_MOCKUP_TEST_2016
        static protected XrmMockup2016 crm;
#elif XRM_MOCKUP_TEST_365
        static protected XrmMockup365 crm;
#endif

        public UnitTestBaseNoProxyTypes(XrmMockupFixtureNoProxyTypes fixture)
        {
            crm = fixture.crm;
            crm.ResetEnvironment();
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
            if (fixture.crmRealData != null)
                orgRealDataService = fixture.crmRealData.GetAdminService();
        }

        public void Dispose()
        {
            crm.ResetEnvironment();
        }
    }
}