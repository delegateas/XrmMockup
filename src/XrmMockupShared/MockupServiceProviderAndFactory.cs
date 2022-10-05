using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup
{

    /// <summary>
    /// A factory used to generate MockupServices based on a given Mockup instance and pluginContext
    /// </summary>
    internal class MockupServiceProviderAndFactory : IServiceProvider, IOrganizationServiceFactory {

        private Core core;
        private ITracingService tracingService;
        private PluginContext pluginContext;
        private Dictionary<Type, object> mockServices;

        /// <summary>
        /// Creates new MockupServiceProviderAndFactory object
        /// </summary>
        /// <param name="core"></param>
        public MockupServiceProviderAndFactory(Core core) : this(core, null, new TracingService()) { }

        internal MockupServiceProviderAndFactory(Core core, PluginContext pluginContext, ITracingService tracingService)
        {
            this.core = core;
            this.pluginContext = pluginContext;
            this.tracingService = tracingService;
            this.mockServices = core.ServiceFactory?.mockServices ?? new Dictionary<Type, object>();
        }

        /// <summary>
        /// Get service from servicetype. Returns null if unknown type, new types can be added with <see cref="AddCustomService"/> if needed.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType) {
            if (serviceType == typeof(IPluginExecutionContext)) return this.pluginContext;
            if (serviceType == typeof(ITracingService)) return this.tracingService;
            if (serviceType == typeof(IOrganizationServiceFactory)) return this;

            mockServices.TryGetValue(serviceType, out object customService);

            if(customService == null)
            {
                var errorMessage = $"No service with the type {serviceType} found.\n" +
                    $"Only {nameof(IPluginExecutionContext)}, {nameof(ITracingService)} and {nameof(IOrganizationServiceFactory)} are supported by default.\n" +
                    $"Other mock services need to be registered in the mock crm context by yourself.";
                throw new KeyNotFoundException(errorMessage);
            }

            return customService;
        }

        /// <summary>
        /// Add a custom mock service. 
        /// This will not override the default XrmMockup services.
        /// </summary>
        /// <typeparam name="TypeT"></typeparam>
        /// <param name="service"></param>
        public void AddCustomService<T>(T service)
        {
            mockServices.Add(typeof(T), service);
        }

        /// <summary>
        /// Remove a custom mock service.
        /// This will not affect the default XrmMockup services.
        /// </summary>
        /// <typeparam name="TypeT"></typeparam>
        /// <param name="service"></param>
        public void RemoveCustomService<T>()
        {
            mockServices.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all custom mock services.
        /// This will not affect the default XrmMockup services.
        /// </summary>
        public void ClearCustomServices()
        {
            mockServices.Clear();
        }

        /// <summary>
        /// Returns a new MockupService with the given userId, or standard user if null. 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid? userId) {
            return new MockupService(core, userId, this.pluginContext);
        }

        public IOrganizationService CreateOrganizationService(Guid? userId, MockupServiceSettings settings) {
            return new MockupService(core, userId, this.pluginContext, settings);
        }

        public IOrganizationService CreateAdminOrganizationService() {
            return new MockupService(core, null, this.pluginContext);
        }

        public IOrganizationService CreateAdminOrganizationService(MockupServiceSettings settings) {
            return new MockupService(core, null, this.pluginContext, settings);
        }
    }
}
