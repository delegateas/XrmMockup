using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.XrmPluginCore;
using DG.XrmPluginCore.Enums;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DG.Some.Namespace
{
    public class LeadRetrieveMultiplePlugin : Plugin
    {
        public LeadRetrieveMultiplePlugin()
        {
            RegisterPluginStep<Lead>(EventOperation.RetrieveMultiple, ExecutionStage.PostOperation, OnRetrieveMultiple);
        }

        private void OnRetrieveMultiple(LocalPluginContext ctx)
        {
            ctx.PluginExecutionContext.OutputParameters.TryGetValue("BusinessEntityCollection", out var ec);
            if (ec == null) return;

            var first = (ec as EntityCollection)?.Entities?.FirstOrDefault();
            if (first != null)
                first.Attributes["description"] = "*** TEST VALUE ***";
        } 
    }
}
