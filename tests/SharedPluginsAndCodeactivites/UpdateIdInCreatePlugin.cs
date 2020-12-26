namespace DG.Some.Namespace
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using static SharedPluginsAndCodeactivites.Utility.Enums;

    public class UpdateIdInCreatePlugin : Plugin
    {
        public UpdateIdInCreatePlugin()
            : base(typeof(UpdateIdInCreatePlugin))
        {
            RegisterPluginStep<dg_child>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;
            var con = localContext.PluginExecutionContext.InputParameters["Target"] as dg_child;

            service.Update(new dg_child()
            {
                Id = con.Id,
                dg_name = "Micky Mouse",
            });
        }
    }
}
