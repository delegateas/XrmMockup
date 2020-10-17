namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup;

    public class Test1Plugin1 : TestPlugin
    {
        public Test1Plugin1()
            : base(typeof(Test1Plugin1))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync1NameUpdate)
                .AddImage(ImageType.PostImage, "name")
                .AddFilteredAttributes("emailaddress1")
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(1);
        }

        protected void Sync1NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            //var account = Account.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, "name");
            var account = GetPostImage<Account>(localContext, "PostImage");

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync1"
            };
            service.Update(accountUpd);
        }
    }
}
