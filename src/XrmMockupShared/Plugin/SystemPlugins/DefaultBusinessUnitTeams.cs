using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Web.WebSockets;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeams : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

        internal DefaultBusinessUnitTeams() : base(typeof(DefaultBusinessUnitTeams))
        {
            RegisterPluginStep("businessunit",
                PluginEventOperation.Update,
                PluginExecutionStage.PostOperation,
                Update);
        }

        private void Update(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedBusinessUnit = orgService.Retrieve("businessunit", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));
            var oldBusinessUnit = localContext.PluginExecutionContext.PreEntityImages;

            var teamQuery = new QueryExpression("team");
            teamQuery.ColumnSet = new ColumnSet();
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, retrievedBusinessUnit.Id);


            var teams = orgService.RetrieveMultiple(teamQuery).Entities;

            var newTeam = new Entity("team");
            newTeam["name"] = retrievedBusinessUnit.Attributes["name"];
            newTeam["teamid"] = teams[0].Id;
            newTeam.Id = teams[0].Id;

            
            orgService.Update(newTeam);
            
            //var retrievedTeam = orgService.

        }

        private void HandleServices(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            orgAdminService = localContext.OrganizationAdminService;
            orgService = localContext.OrganizationService;
        }
    }
}
