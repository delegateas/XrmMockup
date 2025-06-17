using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;
using Microsoft.Xrm.Sdk;

namespace DG.XrmMockupTest
{
    public class TestActivities : UnitTestBase
    {
        public TestActivities(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateEmail()
        {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email()
            {
                Subject = "subject",
                Description = "description",
                From = senderList,
                To = receivingList
            };
            Guid emailId = orgAdminService.Create(email);
            Assert.NotEqual(Guid.Empty, emailId);
        }

        [Fact]
        public void TestActivityPointer()
        {
            var senderList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var receivingList = new List<ActivityParty>() { new ActivityParty() { PartyId = crm.AdminUser } };
            var email = new Email()
            {
                Subject = "subject",
                Description = "description",
                From = senderList,
                To = receivingList
            };
            email.Id = orgAdminService.Create(email);

            var retrieved = orgAdminService.Retrieve(email.LogicalName, email.Id, new ColumnSet(true)).ToEntity<Email>();

            using (var context = new Xrm(orgAdminService))
            {
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
            using (var context = new Xrm(orgAdminService))
            {
                var ap = context.ActivityPointerSet.FirstOrDefault(a => a.Id == email.Id);
                Assert.NotNull(ap);
                Assert.Equal(retrieved.Subject, ap.Subject);
            }
        }

        [Theory]
        [InlineData("appointment", 4201)]
        [InlineData("email", 4202)]
        [InlineData("fax", 4204)]
        [InlineData("incidentresolution", 4206)]
        [InlineData("letter", 4207)]
        [InlineData("opportunityclose", 4208)]
        [InlineData("phonecall", 4210)]
        [InlineData("task", 4212)]
        [InlineData("serviceappointment", 4214)]
        [InlineData("campaignresponse", 4401)]
        [InlineData("campaignactivity", 4402)]
        public void TestActivityPointer_SystemActivities(string entityName, int activityTypeCode)
        {
            var entity = new Entity(entityName)
            {
                ["subject"] = "Test Activity"
            };
            entity.Id = orgAdminService.Create(entity);

            // Retrieve the entity to ensure it was created correctly
            var retrieved = orgAdminService.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, retrieved.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Test Activity", retrieved.GetAttributeValue<string>("subject"));

            var ap = orgAdminService.Retrieve("activitypointer", entity.Id, new ColumnSet(true));
            Assert.Equal(activityTypeCode, ap.GetAttributeValue<OptionSetValue>("activitytypecode").Value);
            Assert.Equal("Test Activity", ap.GetAttributeValue<string>("subject"));
        }
    }
}