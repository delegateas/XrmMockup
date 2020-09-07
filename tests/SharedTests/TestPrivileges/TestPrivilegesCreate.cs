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
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestPrivilegesCreate : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        [TestInitialize]
        public void Init()
        {
            crm.DisableRegisteredPlugins(true);
            var buLevel1 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = crm.RootBusinessUnit, Name = "Business Level 1" });
            var buLevel2 = orgAdminService.Create(new BusinessUnit() { ParentBusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1), Name = "Business Level 2" });

            UserBURoot = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userRoot@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, crm.RootBusinessUnit.Id) }) as SystemUser;
            UserBULvl11 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl11@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl12 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl12@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel1) }) as SystemUser;
            UserBULvl2 = crm.CreateUser(orgAdminService, new SystemUser() { DomainName = "userBULvl2@privileges", BusinessUnitId = new EntityReference(BusinessUnit.EntityLogicalName, buLevel2) }) as SystemUser;

            // Add account append privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            AddGlobalCreateAccessOnAccount(UserBURoot.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl12.ToEntityReference());
            AddGlobalCreateAccessOnAccount(UserBULvl2.ToEntityReference());
        }

        /// <summary>
        /// Test create account with basic level privilege
        /// </summary>
        [TestMethod]
        public void TestCreateUserLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Basic },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl11.Id, account.OwnerId.Id);
                Assert.AreEqual("Account basic", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with local level privilege
        /// </summary>
        [TestMethod]
        public void TestCreateBULevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Local },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl11.Id, account.OwnerId.Id);
                Assert.AreEqual("Account basic", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl12.Id, account.OwnerId.Id);
                Assert.AreEqual("Account local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with deep level privilege
        /// </summary>
        [TestMethod]
        public void TestCreateDeepLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Deep },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl11.Id, account.OwnerId.Id);
                Assert.AreEqual("Account basic", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl12.Id, account.OwnerId.Id);
                Assert.AreEqual("Account local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl2.Id, account.OwnerId.Id);
                Assert.AreEqual("Account deep", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Test create account with global level privilege
        /// </summary>
        [TestMethod]
        public void TestCreateGlobalLevelPrivilege()
        {
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test create account without privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account non",
                });
                Assert.Fail("User should not be able to create");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to create");
            }
            catch (Exception) { }

            // Add account create privilege with global level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                        }
                    },
                });

            // try create account with basic privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account basic",
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl11.Id, account.OwnerId.Id);
                Assert.AreEqual("Account basic", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with local privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account local",
                    OwnerId = UserBULvl12.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl12.Id, account.OwnerId.Id);
                Assert.AreEqual("Account local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with deep privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account deep",
                    OwnerId = UserBULvl2.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBULvl2.Id, account.OwnerId.Id);
                Assert.AreEqual("Account deep", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }

            // try create account with global privilege
            try
            {
                var accId = userOrg.Create(new Account()
                {
                    Name = "Account global",
                    OwnerId = UserBURoot.ToEntityReference(),
                });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual(UserBURoot.Id, account.OwnerId.Id);
                Assert.AreEqual("Account global", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to create");
            }
        }
        
        private void AddGlobalCreateAccessOnAccount(EntityReference user)
        {
            crm.AddPrivileges(
                user,
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.CreateAccess, PrivilegeDepth.Global },
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });
        }
    }
}
