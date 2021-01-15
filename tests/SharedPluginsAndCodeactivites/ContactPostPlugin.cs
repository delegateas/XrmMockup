namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class ContactPostPlugin : Plugin 
    {
        public ContactPostPlugin()
            : base(typeof(ContactPostPlugin)) 
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

            var con = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;

            if (con.GetAttributeValue<string>("firstname") == "CheckSystemAttributes")
            {
                con["lastname"] = con.GetAttributeValue<DateTime>("createdon").ToString();
                con["firstname"] = "updated";
                service.Update(con);
            }
        }
    }
}
