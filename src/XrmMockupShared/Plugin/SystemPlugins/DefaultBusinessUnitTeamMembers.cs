using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeamMembers : MockupPlugin
    {
        private IOrganizationService _orgService;

        internal DefaultBusinessUnitTeamMembers() : base(typeof(DefaultBusinessUnitTeams))
        {
            RegisterPluginStep(LogicalNames.SystemUser,
                PluginEventOperation.Create,
                PluginExecutionStage.PostOperation,
                AddMember);

            RegisterPluginStep(LogicalNames.SystemUser,
                PluginEventOperation.Delete,
                PluginExecutionStage.PreOperation,
                RemoveMember);

            RegisterPluginStep(LogicalNames.SystemUser,
                PluginEventOperation.Update,
                PluginExecutionStage.PostOperation,
                UpdatePreOperation)
                .AddImage(PluginImageType.PreImage);
        }

        private void UpdatePreOperation(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var preSystemUser = localContext.PluginExecutionContext.PreEntityImages.First().Value;
            var postSystemUser = _orgService.Retrieve("systemuser", localContext.PluginExecutionContext.PrimaryEntityId,
                new ColumnSet("businessunitid"));

            if (postSystemUser.Attributes["businessunitid"] != preSystemUser.Attributes["businessunitid"])
            {
                RemoveMember(preSystemUser);
                AddMember(localContext);
            }
        }

        private void RemoveMember(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedSystemUser = _orgService.Retrieve("systemuser", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));

            RemoveMember(retrievedSystemUser);
        }

        private void RemoveMember(Entity systemUser)
        {
            var team = GetBusinessUnitDefaultTeam(systemUser);

            var request = new RemoveMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] { systemUser.Id }
            };

            _orgService.Execute(request);
        }

        private void AddMember(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedSystemUser = _orgService.Retrieve("systemuser", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));

            var team = GetBusinessUnitDefaultTeam(retrievedSystemUser);

            var request = new AddMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] { retrievedSystemUser.Id }
            };

            _orgService.Execute(request);
        }


        private void HandleServices(LocalPluginContext localContext)
        {
            if (localContext == null)
            {
                throw new ArgumentNullException("localContext");
            }

            _orgService = localContext.OrganizationService;
        }

        private Entity GetBusinessUnitDefaultTeam(Entity retrievedSystemUser)
        {
            EntityReference businessUnitReference = (EntityReference)retrievedSystemUser.Attributes["businessunitid"];

            var teamQuery = new QueryExpression("team");
            teamQuery.ColumnSet = new ColumnSet("isdefault", "businessunitid");
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitReference.Id);

            var retrievedTeams = _orgService.RetrieveMultiple(teamQuery);

            if (retrievedTeams.Entities.Count > 1)
            {
                throw new FaultException("There cannot be more than one default business unit team!");
            }

            return _orgService.RetrieveMultiple(teamQuery).Entities[0];
        }
    }
}
