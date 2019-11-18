namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class Sync2PostOperation : TestPlugin
    {
        public Sync2PostOperation()
            : base(typeof(Sync2PostOperation))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteAccountPostOperationPluginSync2)
                .AddFilteredAttributes(x => x.EMailAddress2);
        }

        protected void ExecuteAccountPostOperationPluginSync2(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + ", Sync",
            };

            service.Update(personelUpd);

        }
    }
}
