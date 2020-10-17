namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup;


    public class ContactPostPluginAdmin : Plugin 
    {
        public ContactPostPluginAdmin()
            : base(typeof(ContactPostPlugin)) 
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .SetUserContext(Guid.Parse("84a23551-017a-44fa-9cc1-08ee14bb97e8"));
        }

        protected void Execute(LocalPluginContext localContext) 
        {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var con = localContext.PluginExecutionContext.InputParameters["Target"] as Contact;
            Guid id;
            if (con != null && con.FirstName != null && Guid.TryParse(con.FirstName, out id))
            {
                //this should work as this plugin has been registered to run as system admin
                service.Delete("contact", id);
            }
        }
    }
}
