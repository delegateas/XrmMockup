using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmPluginCore;
using DG.XrmPluginCore.Enums;

namespace DG.Some.Namespace 
{
    public class ChildPreCreatePlugin : Plugin 
    {
        public ChildPreCreatePlugin() 
        {
            RegisterPluginStep("mock_child",
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute).SetExecutionMode(ExecutionMode.Synchronous);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            //create the parent
            var parent = new Entity("mock_parent");
            parent.Id = localContext.OrganizationService.Create(parent);

            var child = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            child["mock_parentid"] = parent.ToEntityReference();
        }
    }
}
