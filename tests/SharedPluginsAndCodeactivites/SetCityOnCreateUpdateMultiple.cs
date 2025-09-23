using System;
using System.Collections.Generic;
using System.Linq;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace DG.Some.Namespace
{
    public class SetCityOnCreateUpdateMultiple : Plugin
    {
        public SetCityOnCreateUpdateMultiple() : base(typeof(SetCityOnCreateUpdateMultiple))
        {
            RegisterPluginStep<Contact>(
                EventOperation.CreateMultiple,
                ExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep<Contact>(
                EventOperation.UpdateMultiple,
                ExecutionStage.PreOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var targets = localContext.PluginExecutionContext.InputParameters["Targets"] as EntityCollection;

            if (targets == null)
            {
                throw new InvalidPluginExecutionException("Invalid request type.");
            }

            foreach (var entity in targets.Entities)
            {
                entity["address2_city"] = "Copenhagen";
            }
        }
    }
}
