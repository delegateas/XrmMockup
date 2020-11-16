using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class SetIsDocumentOnAnnotation : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

        // Register when/how to execute
        public SetIsDocumentOnAnnotation() : base(typeof(SetIsDocumentOnAnnotation))
        {
            RegisterPluginStep("annotation",
                PluginEventOperation.Create,
                PluginExecutionStage.PreValidation,
                Execute);
        }

        // Execute plugin logic
        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            orgService = localContext.OrganizationService;
            orgAdminService = localContext.OrganizationAdminService;

            var target = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            if (!string.IsNullOrEmpty(target.GetAttributeValue<string>("documentbody")))
            {
                target["isdocument"] = true;
            }

            
        }
    }
}
