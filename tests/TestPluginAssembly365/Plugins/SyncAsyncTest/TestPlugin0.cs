using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmPluginCore.Enums;
using DG.XrmPluginCore;
using System;
using TestPluginAssembly365.Plugins.SyncAsyncTest;

namespace DG.Some.Namespace.Test
{
    public class TestPlugin0 : TestPlugin
    {
        public TestPlugin0()
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Sync0NameUpdate)
                .AddImage(ImageType.PostImage, x => x.Name)
                .AddFilteredAttributes(x => x.EMailAddress1)
                .SetExecutionOrder(0);
        }

        protected void Sync0NameUpdate(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;

            var account = GetPostImage<Account>(localContext, "PostImage");

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "Sync0"
            };
            service.Update(accountUpd);
        }
    }
}
