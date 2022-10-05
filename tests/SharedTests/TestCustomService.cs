using Xunit;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using System.Collections.Generic;

namespace DG.XrmMockupTest
{
    class MockServiceEndpointNotificationService : IServiceEndpointNotificationService
    {
        public string Execute(EntityReference serviceEndpoint, IExecutionContext context)
        {
            return null;
        }
    }

    public class TestCustomService : UnitTestBase
    {
        private const string contactDescription = "Test_IServiceEndpointNotificationService";
        
        public TestCustomService(XrmMockupFixture fixture) : base(fixture)
        {
            crm.ResetServices();
        }

        [Fact]
        public void CustomServiceShouldBeAvailableInPlugin()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);

            orgAdminUIService.Create(new Contact() { Description = contactDescription });
        }

        [Fact]
        public void ShouldThrowExceptionOnDuplicateRegistration()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);

            Assert.Throws<System.ArgumentException>(() =>
            {
                var customService2 = new MockServiceEndpointNotificationService();
                crm.AddService<IServiceEndpointNotificationService>(customService2);
            });
        }

        [Fact]
        public void ShouldThrowKeyNotFoundExceptionIfServiceIsNotFound()
        {
            Assert.Throws<KeyNotFoundException>(() =>
            {
                orgAdminUIService.Create(new Contact() { Description = contactDescription });
            });

            crm.ResetServices();
        }

        [Fact]
        public void ShouldThrowKeyNotFoundExceptionIfServiceIsRemoved()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);
            crm.RemoveService<IServiceEndpointNotificationService>();

            Assert.Throws<KeyNotFoundException>(() =>
            {
                orgAdminUIService.Create(new Contact() { Description = contactDescription });
            });
        }

        [Fact]
        public void ShouldThrowKeyNotFoundExceptionIfServicesAreReset()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);
            crm.ResetServices();

            Assert.Throws<KeyNotFoundException>(() =>
            {
                orgAdminUIService.Create(new Contact() { Description = contactDescription });
            });
        }

        [Fact]
        public void CustomServiceShouldStayAvailableAfterEnvironmentReset()
        {
            var customService = new MockServiceEndpointNotificationService();
            crm.AddService<IServiceEndpointNotificationService>(customService);
            crm.ResetEnvironment();

            orgAdminUIService.Create(new Contact() { Description = "Test_IServiceEndpointNotificationService" });
        }
    }
}
