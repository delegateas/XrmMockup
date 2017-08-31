using DG.Tools.XrmMockup.Plugin;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// A factory used to generate MockupServices based on a given Mockup instance and pluginContext
    /// </summary>
    internal class MockupServiceProviderAndFactory : IServiceProvider, IOrganizationServiceFactory {

        private Core core;
        private ITracingService tracingService;
        private MockupPluginContext pluginContext;

        /// <summary>
        /// Creates new MockupServiceProviderAndFactory object
        /// </summary>
        /// <param name="core"></param>
        public MockupServiceProviderAndFactory(Core core) : this(core, null, new TracingService()) { }

        internal MockupServiceProviderAndFactory(Core core, MockupPluginContext pluginContext, ITracingService tracingService) {
            this.core = core;
            this.pluginContext = pluginContext;
            this.tracingService = tracingService;
        }

        /// <summary>
        /// Get service from servicetype. Returns null if unknown type
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType) {
            if (serviceType == typeof(IPluginExecutionContext)) return this.pluginContext;
            if (serviceType == typeof(ITracingService)) return this.tracingService;
            if (serviceType == typeof(IOrganizationServiceFactory)) return this;
            return null;
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
