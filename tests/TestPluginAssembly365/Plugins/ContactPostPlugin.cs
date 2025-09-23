namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.XrmPluginCore;
    using DG.XrmPluginCore.Enums;

    public class ContactPostPlugin : Plugin 
    {
        public ContactPostPlugin()
        {
            RegisterPluginStep<Contact>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext) 
        {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var con = (localContext.PluginExecutionContext.InputParameters["Target"] as Entity).ToEntity<Contact>();

            if (con.FirstName == "CheckSystemAttributes")
            {
                con.LastName = con.CreatedOn?.ToString();
                con.FirstName = "updated";
                service.Update(con);
            }
        }
    }
}
