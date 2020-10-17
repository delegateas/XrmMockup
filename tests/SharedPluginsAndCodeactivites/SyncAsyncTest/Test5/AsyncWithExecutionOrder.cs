namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup.Config;

    public class ASyncWithExecutionOrder : TestPlugin
    {
        public ASyncWithExecutionOrder()
            : base(typeof(ASyncWithExecutionOrder))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteASyncWithExecutionOrder)
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .SetExecutionOrder(1)
                .AddFilteredAttributes("emailaddress1");
        }

        protected void ExecuteASyncWithExecutionOrder(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + ", Async",
            };

            service.Update(personelUpd);
        }
    }
}
