namespace DG.Some.Namespace.Test
{
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.Tools.XrmMockup;

    public class Test8Plugin2 : TestPlugin
    {
        public Test8Plugin2()
            : base(typeof(Test8Plugin2))
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync3NameUpdate)
                .AddImage(ImageType.PostImage, "name")
                .AddFilteredAttributes("emailaddress2")
                .SetExecutionMode(ExecutionMode.Synchronous);
        }

        protected void Sync3NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var account = GetPostImage<Account>(localContext, "PostImage");

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync3",
            };
            service.Update(accountUpd);
        }
    }
}
