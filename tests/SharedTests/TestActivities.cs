using System;
using System.Text;
using System.Collections.Generic;
using DG.Some.Namespace;
using System.Linq;
using Microsoft.Xrm.Sdk;
using System.Diagnostics;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest {

    [TestClass]
    public class TestActivities : UnitTestBase {
        [TestMethod]
        public void TestCreateEmail() {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email() {
                Subject = "subject",
                Description = "description",
                From = senderList,
                To = receivingList
            };
            Guid emailId = orgAdminService.Create(email);
            Assert.IsNotNull(emailId);
        }

        //Ignored until ActivityPointer-functionality has been implemented
        [TestMethod]
        public void TestActivityPointer() {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email() {
                Subject = "subject",
                Description = "description",
                From = senderList,
                To = receivingList
            };
            email.Id = orgAdminService.Create(email);

            var retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();

            using (var context = new Xrm(orgAdminService)) {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.IsNotNull(ap);
                Assert.AreEqual(retrieved.Subject, ap.Subject);
                Assert.AreEqual(retrieved.Description, ap.Description);
                Assert.AreEqual(retrieved.OwnerId, ap.OwnerId);
                Assert.AreEqual(retrieved.CreatedOn.Value.Date, ap.CreatedOn.Value.Date);
            }

            email.Subject = "updated subject";
            orgAdminService.Update(email);
            retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();
            using (var context = new Xrm(orgAdminService)) {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.IsNotNull(ap);
                Assert.AreEqual(retrieved.Subject, ap.Subject);
            }
        }
    }
}