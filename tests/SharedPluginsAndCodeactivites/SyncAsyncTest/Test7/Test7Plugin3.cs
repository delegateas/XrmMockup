namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup.Config;

    public class Test7Plugin3 : TestPlugin
    {
        public Test7Plugin3()
            : base(typeof(Test7Plugin3))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Syn3NameUpdate)
                .AddImage(ImageType.PostImage,"name")
                .AddFilteredAttributes("emailaddress1")
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(2);
        }

        protected void Syn3NameUpdate(LocalPluginContext localContext)
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
                Name = account.Name + "Sync3"
            };
            service.Update(accountUpd);
        }
    }
}
