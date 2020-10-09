using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using DG.XrmFramework.BusinessDomain.ServiceContext;

namespace DG.XrmMockupTest
{
    public class TestActivities : UnitTestBase {

        public TestActivities(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
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
            Assert.NotEqual(Guid.Empty,emailId);
        }

        //Ignored until ActivityPointer-functionality has been implemented
        [Fact]
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
                Assert.NotNull(ap);
                Assert.Equal(retrieved.Subject, ap.Subject);
                Assert.Equal(retrieved.Description, ap.Description);
                Assert.Equal(retrieved.OwnerId, ap.OwnerId);
                Assert.Equal(retrieved.CreatedOn.Value.Date, ap.CreatedOn.Value.Date);
            }

            email.Subject = "updated subject";
            orgAdminService.Update(email);
            retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();
            using (var context = new Xrm(orgAdminService)) {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.NotNull(ap);
                Assert.Equal(retrieved.Subject, ap.Subject);
            }
        }
    }
}