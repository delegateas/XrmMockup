using System;
using Microsoft.Xrm.Sdk;
using DG.Tools.XrmMockup;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace DG.XrmMockupTest
{
    [Collection("Xrm Collection")]
    public class UnitTestBaseNoReset
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

        public UnitTestBaseNoReset(XrmMockupFixture fixture)
        {
            crm = fixture.crm;
            orgAdminUIService = crm.GetAdminService(new MockupServiceSettings(true, false, MockupServiceSettings.Role.UI));
            orgGodService = crm.GetAdminService(new MockupServiceSettings(false, true, MockupServiceSettings.Role.SDK));
            orgAdminService = crm.GetAdminService();
            if (fixture.crmRealData != null)
                orgRealDataService = fixture.crmRealData.GetAdminService();

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