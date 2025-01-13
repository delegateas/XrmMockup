using System;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup
{
    internal class UpdateLastNamePlugin : MockupPlugin
    {
        internal UpdateLastNamePlugin() : base(typeof(UpdateLastNamePlugin))
        {
            RegisterPluginStep("CreateMultipleRequest",
                PluginEventOperation.Execute,
                PluginExecutionStage.PreOperation,
                Execute);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var request = localContext.PluginExecutionContext.InputParameters["Target"] as CreateMultipleRequest;

            if (request == null)
            {
                throw new InvalidPluginExecutionException("Invalid request type.");
            }

            foreach (var createRequest in request.Requests)
            {
                var entity = (Entity)createRequest.Parameters["Target"];
                entity["lastname"] = "Saget";
            }
        }
    }
}
