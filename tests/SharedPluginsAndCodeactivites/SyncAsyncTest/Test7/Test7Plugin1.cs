namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;

    public class Test7Plugin1 : TestPlugin
    {
        public Test7Plugin1()
            : base(typeof(Test7Plugin1))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync1NameAndEmailAddress2Update)
                .AddImage(ImageType.PostImage,(x => x.Name))
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionMode(ExecutionMode.Synchronous)
                .SetExecutionOrder(1);
        }

        protected void Sync1NameAndEmailAddress2Update(LocalPluginContext localContext)
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
                Name = account.Name + "Sync1",
                EMailAddress2 = account.EMailAddress2
            };
            service.Update(accountUpd);
        }
    }
}
