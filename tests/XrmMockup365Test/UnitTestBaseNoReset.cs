using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace DG.XrmMockupTest
{
    public class UnitTestBaseNoReset : IClassFixture<XrmMockupFixture>
    {
        private static DateTime _startTime { get; set; }

        protected IOrganizationService orgAdminUIService;
        protected IOrganizationService orgAdminService;
        protected IOrganizationService orgGodService;
        protected IOrganizationService orgRealDataService;

        protected XrmMockup365 crm;

        public UnitTestBaseNoReset(XrmMockupFixture fixture)
        {
            // Each test gets its own completely fresh instance
            crm = XrmMockup365.GetInstance(fixture.Settings);
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
            // Skip real data service - it causes online connection issues and isn't needed for most tests
            orgRealDataService = null;

            //create an admin user to run our impersonating user plugins as

            var userQuery = new QueryExpression("systemuser");
            userQuery.Criteria.AddCondition("systemuserid", ConditionOperator.Equal, Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c"));
            var users = orgAdminService.RetrieveMultiple(userQuery);
            if (users.Entities.SingleOrDefault() == null)
            {
                var adminUser = new Entity("systemuser") { Id = Guid.Parse("3b961284-cd7a-4fa3-af7e-89802e88dd5c") };
                adminUser["businessunitid"] = crm.RootBusinessUnit;
                adminUser = crm.CreateUser(orgAdminService, adminUser, SecurityRoles.SystemAdministrator);
            }
        }
    }
}