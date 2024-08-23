using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;


namespace DG.Some.Namespace
{
    public class RetrievePlugin : Plugin
    {
        public RetrievePlugin() : base(typeof(RetrievePlugin))
        {
            RegisterPluginStep<Account>(
                EventOperation.Retrieve,
                ExecutionStage.PostOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            var entity = localContext.PluginExecutionContext.OutputParameters["BusinessEntity"] as Entity;
            if (entity.ToEntity<Account>().StateCode == AccountState.Inactive)
            {
                throw new InvalidPluginExecutionException("Inactive accounts cannot be retrieved.");
            }
        }
    }
}
