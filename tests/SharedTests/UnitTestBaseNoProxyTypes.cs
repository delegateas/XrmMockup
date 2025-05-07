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

        static protected XrmMockup365 crm;

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