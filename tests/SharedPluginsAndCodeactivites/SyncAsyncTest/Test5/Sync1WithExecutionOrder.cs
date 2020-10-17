namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup;

    public class Sync1WithExecutionOrder : TestPlugin
    {
        public Sync1WithExecutionOrder()
            : base(typeof(Sync1WithExecutionOrder))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteSync1WithExecutionOrder)
                .SetExecutionOrder(1)
                .AddFilteredAttributes("emailaddress1");
        }

        protected void ExecuteSync1WithExecutionOrder(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + ", Sync1",
            };

            service.Update(personelUpd);
        }
    }
}
