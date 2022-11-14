using DG.Some.Namespace;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharedPluginsAndCodeactivites
{
    /// <summary>
    /// This plugin is used to test custom mock service registrations, the actual endpoint id doesn't matter here
    /// </summary>
    public class SendContactToServiceEndpointPlugin : Plugin
    {
        public SendContactToServiceEndpointPlugin() : base(typeof(SendContactToServiceEndpointPlugin))
        {
            RegisterPluginStep<Contact>(EventOperation.Create, ExecutionStage.PreOperation, Execute)
                .SetExecutionMode(ExecutionMode.Synchronous);
        }

        protected void Execute(LocalPluginContext localContext)
        {            
            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            var contact = target.ToEntity<Contact>();

            if (contact.Description == "Test_IServiceEndpointNotificationService")
            {
                var serviceEndpoint = (IServiceEndpointNotificationService)localContext.ServiceProvider.GetService(typeof(IServiceEndpointNotificationService));
                var endpointReference = new EntityReference("serviceendpoint", Guid.Empty);

                serviceEndpoint.Execute(endpointReference, localContext.PluginExecutionContext);
            }
        }
    }
}
