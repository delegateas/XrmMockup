
namespace DG.Some.Namespace {
    using System;
    using DG.XrmFramework.BusinessDomain.ServiceContext;
    using XrmPluginCore;
    using XrmPluginCore.Enums;

    /// <summary>
    /// Test plugin that deletes the target account in a post-Update operation.
    /// Used to verify that XrmMockup handles record deletion inside a post-operation plugin gracefully.
    /// </summary>
    public class AccountDeleteInPostPlugin : Plugin {

        public AccountDeleteInPostPlugin() {
            RegisterPluginStep<Account>(
                EventOperation.Update,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext) {
            if (localContext == null) {
                throw new ArgumentNullException("localContext");
            }

            var service = localContext.OrganizationService;
            service.Delete(Account.EntityLogicalName, localContext.PluginExecutionContext.PrimaryEntityId);
        }
    }
}
