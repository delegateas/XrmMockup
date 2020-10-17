namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup;

    public class Test6Plugin2Async : TestPlugin

    {
        public Test6Plugin2Async()
            : base(typeof(Test6Plugin2Async))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Async2NameUpdate)
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .AddFilteredAttributes("emailaddress1");
        }

        protected void Async2NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var personel = Contact.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.FirstName);

            var personelUpd = new Contact(personel.Id)
            {
                FirstName = personel.FirstName + "ASync2",
            };

            service.Update(personelUpd);
        }
    }
}
