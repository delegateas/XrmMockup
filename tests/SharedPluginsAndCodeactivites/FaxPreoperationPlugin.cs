using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DG.Some.Namespace
{
    public class FaxPreOperationPlugin : Plugin
    {
        public FaxPreOperationPlugin() : base(typeof(FaxPreOperationPlugin))
        {
            RegisterPluginStep<Fax>(EventOperation.Create, ExecutionStage.PreOperation, OnCreate);
        }

        private void OnCreate(LocalPluginContext ctx)
        {
            object targetFax;
            ctx.PluginExecutionContext.InputParameters.TryGetValue("Target", out targetFax);
            if (targetFax == null) return;

            (targetFax as Entity)["category"] = "test category";
            (targetFax as Entity)["isbilled"] = true;



        }
    }
}
