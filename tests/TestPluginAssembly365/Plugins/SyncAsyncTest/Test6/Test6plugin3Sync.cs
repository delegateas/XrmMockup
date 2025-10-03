namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore.Enums;
    using XrmPluginCore;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;

    public class Test6Plugin3Sync : TestPlugin
    {
        public Test6Plugin3Sync()
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync3NameUpdate)
                .AddFilteredAttributes(x => x.EMailAddress2);
        }

        protected void Sync3NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + "Sync3",
            };

            service.Update(personelUpd);

        }
    }
}
