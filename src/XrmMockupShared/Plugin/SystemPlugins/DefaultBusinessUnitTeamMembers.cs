using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;

namespace DG.Tools.XrmMockup.SystemPlugins
{
    internal class DefaultBusinessUnitTeamMembers : MockupPlugin
    {
        IOrganizationService orgAdminService;
        IOrganizationService orgService;

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
                PluginExecutionStage.PreOperation,
                UpdatePreOperation);

            RegisterPluginStep(LogicalNames.SystemUser,
                PluginEventOperation.Update,
                PluginExecutionStage.PostOperation,
                UpdatePostOperation);
        }

        private void UpdatePostOperation(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            if (IsBusinessUnitIdChanged(localContext))
            {
                AddMember(localContext);
            }
        }

        private void UpdatePreOperation(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            if (IsBusinessUnitIdChanged(localContext))
            {
                RemoveMember(localContext);
            }
        }

        private bool IsBusinessUnitIdChanged(LocalPluginContext localContext)
        {
            var targetEntity = (localContext.PluginExecutionContext.InputParameters["Target"] as Entity);

            return targetEntity.Attributes.Contains("businessunitid");
        }


        private void RemoveMember(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedSystemUser = orgService.Retrieve("systemuser", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));
            
            var team = GetBusinessUnitDefaultTeam(retrievedSystemUser);

            var request = new RemoveMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] {retrievedSystemUser.Id}
            };

            orgService.Execute(request);
        }

        private void AddMember(LocalPluginContext localContext)
        {
            HandleServices(localContext);

            var retrievedSystemUser = orgService.Retrieve("systemuser", localContext.PluginExecutionContext.PrimaryEntityId, new ColumnSet("businessunitid"));
            
            var team = GetBusinessUnitDefaultTeam(retrievedSystemUser);

            var request = new AddMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = new[] {retrievedSystemUser.Id}
            };

            orgService.Execute(request);
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

        private Entity GetBusinessUnitDefaultTeam(Entity retrievedSystemUser)
        {
            EntityReference businessUnitReference = (EntityReference)retrievedSystemUser.Attributes["businessunitid"];

            var teamQuery = new QueryExpression("team");
            teamQuery.ColumnSet = new ColumnSet("isdefault", "businessunitid");
            teamQuery.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            teamQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, businessUnitReference.Id);

            var retrievedTeams = orgService.RetrieveMultiple(teamQuery);

            if (retrievedTeams.Entities.Count > 1)
            {
                throw new FaultException("There cannot be more than one default business unit team!");
            }

            return orgService.RetrieveMultiple(teamQuery).Entities[0];
        }
    }
}
