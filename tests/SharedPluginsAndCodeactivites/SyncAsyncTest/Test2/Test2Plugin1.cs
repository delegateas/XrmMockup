namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup.Config;

    public class Test2Plugin1 : TestPlugin
    {
        public Test2Plugin1()
            : base(typeof(Test2Plugin1))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                ASync1NameUpdate)
                .AddImage(ImageType.PostImage, "name")
                .AddFilteredAttributes("emailaddress1")
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .SetExecutionOrder(1);
        }

        protected void ASync1NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            //var account = Account.Retrieve(service, localContext.PluginExecutionContext.PrimaryEntityId, x => x.Name);
            var account = GetPostImage<Account>(localContext, "PostImage");

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "ASync1"
            };
            service.Update(accountUpd);
        }
    }
}
