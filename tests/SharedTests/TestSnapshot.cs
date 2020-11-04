using System;
using System.Collections.Generic;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DG.XrmMockupTest
{
    [TestClass]
    public class TestSnapshot : UnitTestBase
    {
        [TestMethod]
        public void TestTakeSnapshot()
        {
            if (crm.UsingSQL)
            {
                return;
            }

            var contact = new Contact()
            {
                FirstName = "John"
            };
            contact.Id = orgAdminService.Create(contact);

            crm.TakeSnapshot("test1");

            var dbContact = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.AreEqual(contact.FirstName, dbContact.FirstName);

            crm.ResetEnvironment();

            try
            {
                Contact.Retrieve(orgAdminService, contact.Id);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.Message, $"The record of type '{contact.LogicalName}' with id '{contact.Id.ToString()}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
            }

            crm.RestoreToSnapshot("test1");

            var dbContact2 = Contact.Retrieve(orgAdminService, contact.Id);
            Assert.AreEqual(contact.FirstName, dbContact2.FirstName);
        }

        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void TestDeleteSnapshot()
        {
            if (crm.UsingSQL)
            {
                throw new KeyNotFoundException();
            }
            crm.TakeSnapshot("test1");
            crm.RestoreToSnapshot("test1");
            crm.DeleteSnapshot("test1");
            crm.RestoreToSnapshot("test1");
        }

        [TestMethod]
        public void TestMultipleSnapshot()
        {
            if (crm.UsingSQL)
            {
                return;
            }
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            crm.TakeSnapshot("test1");
            crm.ResetEnvironment();

            var contactP = new Contact()
            {
                FirstName = "Peter"
            };
            contactP.Id = orgAdminService.Create(contactP);

            crm.TakeSnapshot("test2");
            crm.ResetEnvironment();
            crm.RestoreToSnapshot("test1");

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.AreEqual(contactJ.FirstName, dbContactJ.FirstName);

            try
            {
                Contact.Retrieve(orgAdminService, contactP.Id);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.Message, $"The record of type '{contactP.LogicalName}' with id '{contactP.Id.ToString()}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
            }

            crm.RestoreToSnapshot("test2");

            var dbContactP = Contact.Retrieve(orgAdminService, contactP.Id);
            Assert.AreEqual(contactP.FirstName, dbContactP.FirstName);

            try
            {
                Contact.Retrieve(orgAdminService, contactJ.Id);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(ex.Message, $"The record of type '{contactJ.LogicalName}' with id '{contactJ.Id.ToString()}' does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
            }
        }

        [TestMethod]
        public void TestMultipleSnapshotIncremental()
        {
            if (crm.UsingSQL)
            {
                return;
            }
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            crm.TakeSnapshot("test1");

            var contactUpd = new Contact(contactJ.Id)
            {
                LastName = "Smith"
            };
            orgAdminService.Update(contactUpd);

            crm.TakeSnapshot("test2");

            crm.ResetEnvironment();
            crm.RestoreToSnapshot("test1");

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.AreEqual(contactJ.FirstName, dbContactJ.FirstName);
            Assert.IsNull(contactJ.LastName);

            crm.ResetEnvironment();
            crm.RestoreToSnapshot("test2");

            var dbContactJ2 = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.AreEqual(contactJ.FirstName, dbContactJ2.FirstName);
            Assert.AreEqual(contactUpd.LastName, dbContactJ2.LastName);
        }

        [TestMethod]
        public void TestTakeActionAfterRestore()
        {
            if (crm.UsingSQL)
            {
                return;
            }
            var contactJ = new Contact()
            {
                FirstName = "John"
            };
            contactJ.Id = orgAdminService.Create(contactJ);

            var testUser = new SystemUser()
            {
                LastName = "test",
                BusinessUnitId = crm.RootBusinessUnit
            };
            testUser.Id = crm.CreateUser(orgAdminService, testUser, SecurityRoles.SystemAdministrator).Id;

            crm.TakeSnapshot("test1");
            crm.ResetEnvironment();
            crm.RestoreToSnapshot("test1");

            // Create Contact

            var contactP = new Contact()
            {
                FirstName = "Peter"
            };
            contactP.Id = orgAdminService.Create(contactP);

            var dbContactJ = Contact.Retrieve(orgAdminService, contactJ.Id);
            Assert.AreEqual(contactJ.FirstName, dbContactJ.FirstName);

            var dbContactP = Contact.Retrieve(orgAdminService, contactP.Id);
            Assert.AreEqual(contactP.FirstName, dbContactP.FirstName);

            // Create Account

            var acc = new Account()
            {
                Name = "test"
            };
            acc.Id = orgAdminUIService.Create(acc);

            var accFetch = Account.Retrieve(orgAdminService, acc.Id);
            Assert.AreEqual(acc.Name, accFetch.Name);

            // Set Owner
            var ownerUpd = new Contact(contactP.Id)
            {
                OwnerId = testUser.ToEntityReference(),
            };
            orgAdminService.Update(ownerUpd);
        }
    }
}
