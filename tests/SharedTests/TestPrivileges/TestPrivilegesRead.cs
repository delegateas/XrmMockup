using System;
using Microsoft.Crm.Sdk.Messages;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestPrivilegesRead : UnitTestBase
    {
        SystemUser UserBURoot;
        SystemUser UserBULvl11;
        SystemUser UserBULvl12;
        SystemUser UserBULvl2;

        [TestInitialize]
        public void Init()
        {

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
        /// Test user read account on basic level
        /// </summary>
        [TestMethod]
        public void TestReadUserLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl11.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on local level
        /// </summary>
        [TestMethod]
        public void TestReadBULevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl12.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on deep level
        /// </summary>
        [TestMethod]
        public void TestReadBUChildLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBULvl2.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(), 
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }
        }

        /// <summary>
        /// Test user read account on global level
        /// </summary>
        [TestMethod]
        public void TestReadGlobalLevel()
        {
            // add account to database
            var accId = Guid.NewGuid();
            crm.PopulateWith(new Account(accId)
            {
                Name = "Account",
                OwnerId = UserBURoot.ToEntityReference(),
            });

            // test read account without privilege
            var userOrg = crm.CreateOrganizationService(UserBULvl11.Id);
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with basic level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { "account",
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Basic },
                        }
                    }
                });

            // try read account with basic privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with local level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Local },
                        }
                    }
                });

            // try read account with local privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with deep level
            crm.AddPrivileges(
                UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Deep },
                        }
                    }
                });

            // try read account with deep privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
                Assert.Fail("User should not be able to read");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("User should not be able to read");
            }
            catch (Exception) { }

            // Add account read privilege with global level
            crm.AddPrivileges(UserBULvl11.ToEntityReference(),
                new Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>() {
                    { Account.EntityLogicalName,
                        new Dictionary<AccessRights, PrivilegeDepth>() {
                            { AccessRights.ReadAccess, PrivilegeDepth.Global },
                        }
                    }
                });

            // try read account with global privilege
            try
            {
                var account = Account.Retrieve(userOrg, accId);
            }
            catch (Exception)
            {
                Assert.Fail("User should be able to read");
            }
        }
    }
}
