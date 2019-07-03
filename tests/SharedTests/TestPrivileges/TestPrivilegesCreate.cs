using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using DG.Tools.XrmMockup;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestPrivilegesCreate : UnitTestBase
    {
        SystemUser User;

        [TestInitialize]
        public void Init()
        {
            crm.DisableRegisteredPlugins(true);
            User = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "test@privileges", BusinessUnitId = crm.RootBusinessUnit }) as SystemUser;
        }

        [TestMethod]
        public void TestCreateUserLevel()
        {
            var userOrg = crm.CreateOrganizationService(User.Id);
            {
                try
                {
                    var accountId = userOrg.Create(new Account());
                    Assert.Fail("User should not be able to create");
                }
                catch (Exception)
                {
                }
            }

            var createPrivilage = new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                { Account.EntityLogicalName,
                    new Dictionary<AccessRights, PrivilegeDepth>() {
                        { AccessRights.CreateAccess, PrivilegeDepth.Basic },
                        { AccessRights.ReadAccess, PrivilegeDepth.Basic }
                    }
                }
            };
            crm.AddPrivileges(User.ToEntityReference(), createPrivilage);

            {
                try
                {
                    var accountId = userOrg.Create(new Account());

                    var retrievedAccount = Account.Retrieve(orgAdminService, accountId);
                    Assert.IsNotNull(retrievedAccount);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"User should be able to create/read - failed with: {ex.Message}");
                }
            }
        }
    }
}
