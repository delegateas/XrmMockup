using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestPrivilegesDelete : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        [TestInitialize]
        public void Init()
        {
            crm.DisableRegisteredPlugins(true);
            var allBU = orgAdminService.RetrieveMultiple(new QueryExpression("businessunit"));

            var bu1Id = Guid.Parse("bc6b78f4-adb2-4fb5-9af6-8623144d4c1a");
            var bu2Id = Guid.Parse("cc6b78f4-adb2-4fb5-9af6-8623144d4c1a");

            Entity buLevel1;
            Entity buLevel2;

            buLevel1 = allBU.Entities.SingleOrDefault(x => x.Id == bu1Id);
            if (buLevel1 == null)
                orgAdminService.Create(new BusinessUnit() { Id = bu1Id, ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });


            buLevel2 = allBU.Entities.SingleOrDefault(x => x.Id == bu2Id);
            if (buLevel2 == null)
                orgAdminService.Create(new BusinessUnit() { Id = bu2Id, ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, bu1Id), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, bu1Id) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, bu1Id) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, bu2Id) }) as SystemUser;

        }

        /// <summary>
        /// Test user delete account on basic level
        /// </summary>
        [TestMethod]
        public void TestDeleteUserLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test delete account without privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl11.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on local level
        /// </summary>
        [TestMethod]
        public void TestDeleteBULevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl12.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on deep level
        /// </summary>
        [TestMethod]
        public void TestDeleteBUChildLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBULvl2.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }

            // Add account to database
            AddAccountToDatabase(accId, UserBULvl2.ToEntityReference());

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }
        }

        /// <summary>
        /// Test user delete account on global level
        /// </summary>
        [TestMethod]
        public void TestDeleteGlobalLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            AddAccountToDatabase(accId, UserBURoot.ToEntityReference());

            // test delete account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try delete account with basic privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try delete account with local privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try delete account with deep privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
                Assert.Fail("User should not be able to delete");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to delete");
            }
            catch (Exception) { }

            // Add account delete privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.DeleteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try delete account with global privilege
            try
            {
                userOrg.Delete(Account.EntityLogicalName, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to delete");
            }
        }

        private void AddAccountToDatabase(Guid accId, EntityReference owner)
        {
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = owner,
            });
        }
    }
}
