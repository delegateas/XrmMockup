using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;

namespace DG.XrmMockupTest
{
    public class UnitTestBaseNoProxyTypes : IClassFixture<XrmMockupFixtureNoProxyTypes>
    {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;

        protected XrmMockup365 crm;

        public UnitTestBaseNoProxyTypes(XrmMockupFixtureNoProxyTypes fixture)
        {
            // Each test gets its own completely fresh instance
            crm = XrmMockup365.GetInstance(fixture.Settings);
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
            // Skip real data service - it causes online connection issues and isn't needed for most tests
            orgRealDataService = null;
        }

        public void Dispose()
        {
            // No need to reset environment since each test has its own instance
            // The instance will be garbage collected automatically
        }
    }
}