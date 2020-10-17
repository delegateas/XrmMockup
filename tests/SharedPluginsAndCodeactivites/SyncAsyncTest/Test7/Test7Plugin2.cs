namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup.Config;

    public class Test7Plugin2 : TestPlugin
    {
        public Test7Plugin2()
            : base(typeof(Test7Plugin2))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Async2NameUpdate)
                .AddImage(ImageType.PostImage,"name")
                .AddFilteredAttributes("emailaddress2")
                .SetExecutionMode(ExecutionMode.Asynchronous)
                .SetExecutionOrder(1);
        }

        protected void Async2NameUpdate(LocalPluginContext localContext)
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
                Name = account.Name + "ASync2"
            };
            service.Update(accountUpd);
        }
    }
}
