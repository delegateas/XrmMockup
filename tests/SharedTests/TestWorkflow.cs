#if !(XRM_MOCKUP_TEST_2011 || XRM_MOCKUP_TEST_2013 || XRM_MOCKUP_TEST_2015)
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using DG.Tools.XrmMockup;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestWorkflow : UnitTestBase {

        [TestMethod]
        public void TestCreateWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "Activeworkflow.xml"));
                var acc = new Account() {
                    Name = "Wap"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = context.AccountSet.Where(x => x.AccountId == acc.Id).FirstOrDefault();
                Assert.AreEqual(acc.Name + "setFromCodeActivity", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestStringAdd() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "AddStringWorkflow.xml"));
                var acc = new Account() {
                    Name = "WapAdd"
                };
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(acc.Name + " - " + acc.Name + " ", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestOtherwiseBranchingWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "OtherwiseWorkflow.xml"));
                var otherwise = new Account() {
                    Name = "Otherwise"
                };
                otherwise.Id = orgAdminUIService.Create(otherwise);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, otherwise.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("SetFromOtherwise", retrieved.Name);


                var thenThen = new Account() {
                    Name = "ThenThen"
                };
                thenThen.Id = orgAdminUIService.Create(thenThen);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, thenThen.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("SetFromThenThen", retrieved.Name);

                var thenOtherwise = new Account();
                thenOtherwise.Name = "ThenOtherwise";
                thenOtherwise.Id = orgAdminUIService.Create(thenOtherwise);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, thenOtherwise.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("SetFromThenOtherwise", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestTimeWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "WaitingWorkflow.xml"));
                var acc = new Account();
                acc.Id = orgAdminUIService.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.IsNull(retrieved.Name);

                acc.Name = "Some name";
                orgAdminUIService.Update(acc);

                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(acc.Name + acc.Name, retrieved.Name);
                
                crm.AddDays(1);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreNotEqual("qwerty", retrieved.Description);

                crm.AddDays(2);
                retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("qwerty", retrieved.Description);
            }
        }


        [TestMethod]
        public void TestRelatedNullWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "RelatedWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, 
                    new SystemUser() { LastName = null, BusinessUnitId = businessunit.ToEntityReference() }, SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);
                var con = new Contact() {
                    LastName = "SomeLastname"
                };
                con.Id = service.Create(con);

                var acc = new Account();
                acc.Name = "Related";
                acc.PrimaryContactId = con.ToEntityReference();
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(user.LastName + " - " + con.LastName + " ", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestRelatedWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "RelatedWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService,
                    new SystemUser() { LastName = "UserLastName", BusinessUnitId = businessunit.ToEntityReference() }, SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);
                var con = new Contact();
                con.LastName = "SomeLastname";
                con.Id = service.Create(con);

                var acc = new Account();
                acc.Name = "Related";
                acc.PrimaryContactId = con.ToEntityReference();
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(user.LastName + " - " + con.LastName + " ", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestOwningBusinessUnitInvalidNameWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "OwningBUWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Name = "something";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account();
                acc.Name = "Some name";
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual(acc.Name, retrieved.Name);
            }
        }

        [TestMethod]
        public void TestOwningBusinessUnitValidNameWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "OwningBUWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Name = "delegatelab4";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account();
                acc.Name = "Some name";
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("SetOwningBU", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestMyConditionWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "OtherWorkflows", "MyConditionWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Name = "deledevelopmentasd";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account();
                acc.Name = "Some name";
                acc.Revenue = 3000;
                acc.DoNotFax = true;
                acc.DoNotBulkPostalMail = false;
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;
                Assert.AreEqual("MyCondition", retrieved.Name);
            }
        }

        [TestMethod]
        public void TestSendMailWorkflow() {
            using (var context = new Xrm(orgAdminUIService)) {
                crm.AddWorkflow(Path.Combine("../..", "Metadata", "Workflows", "SendMailWorkflow.xml"));
                var businessunit = new BusinessUnit();
                businessunit.Name = "delegatelab4";
                businessunit.Id = orgAdminUIService.Create(businessunit);

                crm.CreateUser(orgAdminService, 
                    new SystemUser() {
                        Id = new Guid("9732d6d5-8e46-44e1-b408-32d3801c5724"),
                        FirstName = "Kaspar",
                        LastName = "Bøgh Christensen",
                        BusinessUnitId = crm.RootBusinessUnit
                    }, SecurityRoles.SystemAdministrator);

                var user = crm.CreateUser(orgAdminUIService, businessunit.ToEntityReference(), SecurityRoles.SystemAdministrator) as SystemUser;
                var service = crm.CreateOrganizationService(user.Id);

                var acc = new Account();
                acc.Name = "Some name";
                acc.Id = service.Create(acc);

                var retrieved = orgAdminUIService.Retrieve(Account.EntityLogicalName, acc.Id, new ColumnSet(true)) as Account;

                var email = orgAdminService.RetrieveMultiple(new QueryExpression
                {
                    EntityName = Email.EntityLogicalName,
                    ColumnSet = new ColumnSet(true)
                }).Entities.FirstOrDefault() as Email;

                Assert.IsNotNull(email);
                Assert.AreEqual("&lt;span&gt;&lt;span&gt;Some kind of email&lt;/span&gt;&lt;/span&gt;", email.Description);
                Assert.AreEqual("Something", email.Subject);
                Assert.AreEqual(retrieved.ToEntityReference(), email.RegardingObjectId);
            }
        }
    }
}
#endif
