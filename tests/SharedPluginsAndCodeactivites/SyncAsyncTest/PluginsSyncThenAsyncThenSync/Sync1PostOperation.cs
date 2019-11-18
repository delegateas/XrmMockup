namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class Sync1PostOperation : TestPlugin
    {
        public Sync1PostOperation()
            : base(typeof(Sync1PostOperation))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecuteAccountPostOperationPluginSync1)
                .AddFilteredAttributes(x => x.EMailAddress1);
        }

        protected void ExecuteAccountPostOperationPluginSync1(LocalPluginContext localContext)
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
                EMailAddress2 = "trigger@valid.com"
            };

            service.Update(personelUpd);
        }
    }
}
