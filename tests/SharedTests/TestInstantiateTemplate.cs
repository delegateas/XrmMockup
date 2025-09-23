using Microsoft.Crm.Sdk.Messages;
using System;
using System.ServiceModel;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestInstantiateTemplate : UnitTestBase
    {
        public TestInstantiateTemplate(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestInstantiateTemplateRequestReturnsResponseWithEmail()
        {
            var templateRequest = new InstantiateTemplateRequest
            {
                TemplateId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid(),
                ObjectType = "account"
            };

            var response = orgAdminUIService.Execute(templateRequest) as InstantiateTemplateResponse;

            Assert.Single(response.EntityCollection.Entities);
            Assert.Equal("email", response.EntityCollection.Entities[0].LogicalName);
        }

        [Fact]
        public void TestInstantiateTemplateRequest_ThrowsFaultException_WhenTemplateIdIsNull()
        {
            var templateRequest = new InstantiateTemplateRequest
            {
                ObjectId = Guid.NewGuid(),
                ObjectType = "account"
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(templateRequest));
        }

        [Fact]
        public void TestInstantiateTemplateRequest_ThrowsFaultException_WhenObjectIdIsNull()
        {
            var templateRequest = new InstantiateTemplateRequest
            {
                TemplateId = Guid.NewGuid(),
                ObjectType = "account"
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(templateRequest));
        }

        [Fact]
        public void TestInstantiateTemplateRequest_ThrowsFaultException_WhenObjectTypeIsNull()
        {
            var templateRequest = new InstantiateTemplateRequest
            {
                TemplateId = Guid.NewGuid(),
                ObjectId = Guid.NewGuid()
            };

            Assert.Throws<FaultException>(() => orgAdminUIService.Execute(templateRequest));
        }
    }
}

