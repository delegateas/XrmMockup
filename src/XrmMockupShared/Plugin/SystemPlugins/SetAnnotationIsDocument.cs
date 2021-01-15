using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class SetAnnotationIsDocument : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

        // Register when/how to execute
        public SetAnnotationIsDocument() : base(typeof(SetAnnotationIsDocument))
        {
            RegisterPluginStep("annotation",
                PluginEventOperation.Create,
                PluginExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep("annotation",
                PluginEventOperation.Update,
                PluginExecutionStage.PreOperation,
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

            var targetAnnotation = localContext.PluginExecutionContext.InputParameters["Target"] as Entity;
            if (targetAnnotation.Contains("documentbody") && !string.IsNullOrEmpty(targetAnnotation.GetAttributeValue<string>("documentbody")))
            {
                targetAnnotation["isdocument"] = true;
            }
            else
            {
                targetAnnotation["isdocument"] = false; 
            }
        }
    }
}
