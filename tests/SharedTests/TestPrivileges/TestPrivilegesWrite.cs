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
    public class TestPrivilegesWrite : UnitTestBase
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
        /// Test user write account on basic level
        /// </summary>
        [TestMethod]
        public void TestWriteUserLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl11.ToEntityReference(),
            });

            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);

            // test write account without privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try write account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated basic" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated basic", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try write account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated local" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try write account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated local" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try write account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated global" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated global", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }
        }

        /// <summary>
        /// Test user write account on local level
        /// </summary>
        [TestMethod]
        public void TestWriteBULevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl12.ToEntityReference(),
            });

            // test write account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try write account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try write account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated local" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated local", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try write account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated deep" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated deep", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try write account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated global" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated global", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }
        }

        /// <summary>
        /// Test user write account on deep level
        /// </summary>
        [TestMethod]
        public void TestWriteBUChildLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl2.ToEntityReference(),
            });

            // test write account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try write account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try write account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try write account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated deep" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated deep", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }

            // Add account write privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try write account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated global" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated global", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }
        }

        /// <summary>
        /// Test user write account on global level
        /// </summary>
        [TestMethod]
        public void TestWriteGlobalLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBURoot.ToEntityReference(),
            });

            // test write account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try write account with basic privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try write account with local privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try write account with deep privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated" });
                Assert.Fail("User should not be able to write");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to write");
            }
            catch (Exception) { }

            // Add account write privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.WriteAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try write account with global privilege
            try
            {
                userOrg.Update(new Account(accId) { Name = "Account Updated global" });
                var account = Account.Retrieve(orgAdminService, accId);
                Assert.AreEqual("Account Updated global", account.Name);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to write");
            }
        }
    }
}
