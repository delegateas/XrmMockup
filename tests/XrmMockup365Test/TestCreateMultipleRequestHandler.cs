using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestCreateMultipleRequestHandler : UnitTestBase
    {
        public TestCreateMultipleRequestHandler(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestCreateMultipleEntities()
        {
            var contact1 = new Contact { FirstName = "John", LastName = "Doe" };
            var contact2 = new Contact { FirstName = "Jane", LastName = "Doe" };

            var createRequest1 = new CreateRequest { Target = contact1 };
            var createRequest2 = new CreateRequest { Target = contact2 };

            var createMultipleRequest = new CreateMultipleRequest
            {
                Requests = new OrganizationRequestCollection { createRequest1, createRequest2 }
            };

            var response = (CreateMultipleResponse)orgAdminService.Execute(createMultipleRequest);

            Assert.Equal(2, response.Responses.Count);

            var createdContact1 = Contact.Retrieve(orgAdminService, ((CreateResponse)response.Responses[0].Response).id);
            var createdContact2 = Contact.Retrieve(orgAdminService, ((CreateResponse)response.Responses[1].Response).id);

            Assert.Equal(contact1.FirstName, createdContact1.FirstName);
            Assert.Equal(contact2.FirstName, createdContact2.FirstName);
        }
    }
}
