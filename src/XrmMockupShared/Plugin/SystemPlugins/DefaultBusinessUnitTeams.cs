using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeams : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

        internal DefaultBusinessUnitTeams() : base(typeof(DefaultBusinessUnitTeams))
        {
            RegisterPluginStep("businessunit",
                PluginEventOperation.Create,
                PluginExecutionStage.PostOperation,
                Execute);

        }

        private void Execute(LocalPluginContext locatContext)
        {
            if (locatContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            orgAdminService = locatContext.OrganizationAdminService;
            orgService = locatContext.OrganizationService;
            /* When creating a business unit, we have to create a corresponding team, with the same name
             */

            var retrievedTarget = orgService.Retrieve("businessunit", locatContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));

            var team = new Entity("team");
            team["name"] = retrievedTarget.GetAttributeValue<string>("name");
            team["businessunitid"] = retrievedTarget.ToEntityReference();

            var businessUnitTeam = orgAdminService.Create(team);

        }
    }
}
