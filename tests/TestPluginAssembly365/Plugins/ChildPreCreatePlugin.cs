using Microsoft.Xrm.Sdk;
using XrmPluginCore;
using XrmPluginCore.Enums;

namespace DG.Some.Namespace
{
    public class ChildPreCreatePlugin : Plugin
    {
        public ChildPreCreatePlugin() 
        {
#pragma warning disable CS0618 // Type or member is obsolete - disabled for testing purposes
            RegisterPluginStep<ChildEntity>(
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute).SetExecutionMode(ExecutionMode.Synchronous);
#pragma warning restore CS0618 // Type or member is obsolete
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
