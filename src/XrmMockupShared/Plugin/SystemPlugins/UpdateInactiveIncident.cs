using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class UpdateInactiveIncident : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

        // Register when/how to execute
        public UpdateInactiveIncident() : base(typeof(UpdateInactiveIncident))
        {
            RegisterPluginStep("incident",
                PluginEventOperation.Update,
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

            var retrievedTarget = orgService.Retrieve("incident", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("incidentid","statecode"));
            var stateCode = retrievedTarget.GetAttributeValue<OptionSetValue>("statecode").Value;

            var incident =
                (localContext.PluginExecutionContext.InputParameters["Target"] as Entity);

            string[] legalUpdates = new [] {"ownerid", "owneridyominame", "owneridtype", "owninguser",
                                         "statecode", "statuscode", "modifiedon", "modifiedby",
                                         "modifiedonbehalfby", "owningbusinessunit", "processid", "incidentid" };

            string errorMessage = "Only the following fields can be edited for inactive incident: " + string.Join(", ", legalUpdates.Select(x => "\"" + x + "\""));

            var illegalUpdates = incident.Attributes.Keys.Except(legalUpdates);

            if (illegalUpdates.Count() > 0 && (stateCode == 1 || stateCode == 2))
            {
                throw new System.ServiceModel.FaultException(errorMessage);
            } 
        }
    }
}
