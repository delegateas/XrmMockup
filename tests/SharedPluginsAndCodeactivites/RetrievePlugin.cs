﻿using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;


namespace DG.Some.Namespace
{
    public class RetrievePlugin : Plugin
    {
        public RetrievePlugin() : base(typeof(RetrievePlugin))
        {
            RegisterPluginStep<Contact>(
                EventOperation.Retrieve,
                ExecutionStage.PostOperation,
                ExecutePostRetrieve);
        }

        protected void ExecutePostRetrieve(LocalPluginContext localContext)
        {
            var entity = localContext.PluginExecutionContext.OutputParameters["BusinessEntity"] as Entity;

            if (entity.ToEntity<Contact>().StateCode == ContactState.Inactive)
            {
                throw new InvalidPluginExecutionException("Inactive contacts cannot be retrieved.");
            }
        }
    }
}
