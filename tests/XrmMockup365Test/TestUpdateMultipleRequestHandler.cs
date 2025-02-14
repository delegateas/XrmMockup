using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestUpdateMultipleRequestHandler : UnitTestBase
    {
        public TestUpdateMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestUpdateMultipleEntities()
        {
            var contact1 = orgGodService.Create(new Contact { FirstName = "John", LastName = "Doe" });
            var contact2 = orgGodService.Create(new Contact { FirstName = "Jane", LastName = "Doe" });

            var updateContact1 = new Contact(contact1) { FirstName = "Johny" };
            var updateContact2 = new Contact(contact2) { FirstName = "Janey" };

            var updateMultipleRequest = new UpdateMultipleRequest
            {
                Targets = new EntityCollection
                {
                    EntityName = Contact.EntityLogicalName,
                    Entities = { updateContact1, updateContact2 }
                }
            };

            var response = (UpdateMultipleResponse)orgAdminService.Execute(updateMultipleRequest);

            //Assert.Equal(2, response.Ids.Length);

            var updatedContact1 = Contact.Retrieve(orgAdminService, contact1);
            var updatedContact2 = Contact.Retrieve(orgAdminService, contact2);

            Assert.Equal(updateContact1.FirstName, updatedContact1.FirstName);
            Assert.Equal(updateContact2.FirstName, updatedContact2.FirstName);
        }
    }
}
