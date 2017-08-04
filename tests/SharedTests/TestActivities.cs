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
        [TestMethod, Ignore]
        public void TestActivityPointer() {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email() {
                Subject = "subject",
                Description = "description",
                From = senderList,
                To = receivingList
            };
            Guid emailId = orgAdminService.Create(email);

            using (var context = new Xrm(orgAdminService)) {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == emailId);
                Assert.IsNotNull(ap);
                Assert.AreEqual(email.Subject, ap.Subject);
                Assert.AreEqual(email.Description, ap.Description);
                Assert.AreEqual(email.OwnerId, ap.OwnerId);
                Assert.AreEqual(email.CreatedOn, ap.CreatedOn);
            }
        }
    }
}