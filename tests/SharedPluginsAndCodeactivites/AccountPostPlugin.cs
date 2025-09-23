﻿
namespace DG.Some.Namespace {
    using System;
    using Microsoft.Xrm.Sdk;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    public class AccountPostPlugin : Plugin {

        public AccountPostPlugin()
            : base(typeof(AccountPostPlugin)) {

            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);

            RegisterPluginStep<Account>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute);

        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }


            var service = localContext.OrganizationService;

            var rand = new Random();
            service.Create(new Lead() {
                Subject = "Some new lead " + rand.Next(0, 1000),
                ParentAccountId = new Account(localContext.PluginExecutionContext.PrimaryEntityId).ToEntityReference()
            });

            service.Create(new Lead() {
                Subject = "Some other lead " + rand.Next(0, 1000),
                ParentAccountId = new Account(localContext.PluginExecutionContext.PrimaryEntityId).ToEntityReference()
            });
        }
    }
}
