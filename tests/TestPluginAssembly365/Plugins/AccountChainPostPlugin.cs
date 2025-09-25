
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using DG.XrmPluginCore;
    using DG.XrmPluginCore.Enums;

    public class AccountChainPostPlugin : Plugin {

        public AccountChainPostPlugin() {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute)
                .AddFilteredAttributes(x => x.Fax);

        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }


            var service = localContext.OrganizationService;

            var rand = new Random();

            var newAcc = new Account(localContext.PluginExecutionContext.PrimaryEntityId) {
                Fax = rand.Next().ToString()
            };
            service.Update(newAcc);
        }
    }
}
