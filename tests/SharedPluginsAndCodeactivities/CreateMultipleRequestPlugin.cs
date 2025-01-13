using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace DG.Tools.XrmMockup
{
    internal class CreateMultipleRequestPlugin : MockupPlugin
    {
        internal CreateMultipleRequestPlugin() : base(typeof(CreateMultipleRequestPlugin))
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
                entity["name"] = "Bob";
            }
        }
    }
}
