using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

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

            // var teamQuery = new QueryExpression
            // {
            //     EntityName = "team",
            //     Criteria = new FilterExpression
            //     {
            //         Conditions = {
            //             new ConditionExpression
            //             {
            //                 AttributeName = "isdefualt",
            //                 Operator = ConditionOperator.Equal,
            //                 Values = {true}
            //             }
            //         }
            //     }
            // };
            
            var teamQuery = new QueryExpression
            {
                EntityName = "team"
            };

            var teams = orgService.RetrieveMultiple(teamQuery).Entities;
            
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
