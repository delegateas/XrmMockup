namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class ASyncNameUpdateCopy : TestPlugin
    {
        public ASyncNameUpdateCopy()
            : base(typeof(ASyncNameUpdateCopy))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ExecutePostOperationPluginAsyncCopy)
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .AddFilteredAttributes(x => x.EMailAddress1);
        }

        protected void ExecutePostOperationPluginAsyncCopy(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + ", ASync",
            };

            service.Update(personelUpd);
        }
    }
}
