﻿namespace DG.Some.Namespace.Test
{
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using Microsoft.Xrm.Sdk;
    using System;
    using TestPluginAssembly365.Plugins.SyncAsyncTest;
    using XrmPluginCore;
    using XrmPluginCore.Enums;
    using XrmPluginCore.Extensions;

    public class Test7Plugin2 : TestPlugin
    {
        public Test7Plugin2()
        {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Async2NameUpdate)
                .AddImage(ImageType.PostImage,(x => x.Name))
                .AddFilteredAttributes(x => x.EMailAddress2)
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
            var account = localContext.GetPostImage<Account>();

            var accountUpd = new Account(account.Id)
            {
                Name = account.Name + "ASync2"
            };
            service.Update(accountUpd);
        }
    }
}
