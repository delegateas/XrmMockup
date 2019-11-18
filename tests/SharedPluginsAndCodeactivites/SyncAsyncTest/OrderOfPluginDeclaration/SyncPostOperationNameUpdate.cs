namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class SyncPostOperationNameUpdate : TestPlugin
    {
        public SyncPostOperationNameUpdate()
            : base(typeof(SyncPostOperationNameUpdate))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecutePostOperationPluginSync)
                .AddFilteredAttributes(x => x.EMailAddress1);
        }

        protected void ExecutePostOperationPluginSync(LocalPluginContext localContext)
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
