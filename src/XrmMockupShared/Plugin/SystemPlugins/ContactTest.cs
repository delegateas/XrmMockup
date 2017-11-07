using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

 namespace DG.Tools.XrmMockup.SystemPlugins
{
    public class ContactDefaultValues : Plugin
    {
        // Register when/how to execute
        public ContactDefaultValues() : base(typeof(ContactDefaultValues))
        {
            RegisterPluginStep("contact",
                PluginEventOperation.Create,
                PluginExecutionStage.PreOperation,
                Execute);

            RegisterPluginStep(
                "contact",
                PluginEventOperation.Create,
                PluginExecutionStage.PostOperation,
                Execute)
                .SetExecutionMode(PluginExecutionMode.Asynchronous);
        }

        // Execute plugin logic
        protected void Execute(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            var contact =
                (localContext.PluginExecutionContext.InputParameters["Target"] as Entity);

            if(localContext.PluginExecutionContext.Stage == (int)ExecutionStage.PreOperation)
                SetDefaultValues(contact);
            else if (localContext.PluginExecutionContext.Stage == (int)ExecutionStage.PostOperation)
                SetDefaultValuesPost(localContext.PluginExecutionContext.PrimaryEntityId);
        }

        private void SetDefaultValues(Entity contact)
        {
            if (contact != null)
            {
                contact["firstname"] = "Test Name";
                
            }
        }

        private void SetDefaultValuesPost(Guid contactid)
        {
            if (contactid != Guid.Empty)
            {
                
            }
        }
    }
}
