using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeams : MockupPlugin
    {
        private IOrganizationService _orgService;

        internal DefaultBusinessUnitTeams() : base(typeof(DefaultBusinessUnitTeams))
        {
            RegisterPluginStep("businessunit",
                PluginEventOperation.Update,
                PluginExecutionStage.PostOperation,
                Update);

            RegisterPluginStep("businessunit",
                PluginEventOperation.Delete,
                PluginExecutionStage.PreOperation,
                Delete);
        }

        private void Delete(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedBusinessUnit = _orgService.Retrieve("businessunit", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));

            var team = GetBusinessUnitDefaultTeam(retrievedBusinessUnit.Id);

            _orgService.Delete("team", team.Id);
        }

        private void Update(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedBusinessUnit = _orgService.Retrieve("businessunit", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("name"));
            
            var team = GetBusinessUnitDefaultTeam(retrievedBusinessUnit.Id);

            var newTeam = new Entity("team");
            newTeam["name"] = retrievedBusinessUnit.Attributes["name"];
            newTeam["teamid"] = team.Id;
            newTeam.Id = team.Id;

            _orgService.Update(newTeam);
        }

        private void HandleServices(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            _orgService = localContext.OrganizationService;
        }

        private Entity GetBusinessUnitDefaultTeam(Guid businessUnitGuid)
        {
            var teamQuery = new QueryExpression("team");
            teamQuery.ColumnSet = new ColumnSet();
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitGuid);

            var retrievedTeams = _orgService.RetrieveMultiple(teamQuery);

            if (retrievedTeams.Entities.Count > 1)
            {
                throw new FaultException("There cannot be more than one default business unit team!");
            }

            return _orgService.RetrieveMultiple(teamQuery).Entities[0];
        }
    }
}
