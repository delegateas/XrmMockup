using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using DG.Tools.XrmMockup;

namespace DG.Some.Namespace {
    public class OpportunityWinLose : Plugin {

        // Register when/how to execute
        public OpportunityWinLose() : base(typeof(OpportunityWinLose)) {

            RegisterPluginStep<Opportunity>(
                EventOperation.Lose,
                ExecutionStage.PostOperation,
                Execute);
            RegisterPluginStep<Opportunity>(
                EventOperation.Win,
                ExecutionStage.PostOperation,
                Execute);

        }

        // Execute plugin logic
        protected void Execute(LocalPluginContext context) {
            
            var ctx = context.PluginExecutionContext;
            var opportunityId = ctx.PrimaryEntityId;
            var message = ctx.MessageName;
            if (message == "Lose") {
                var loseRequest = new LoseOpportunityRequest { Parameters = ctx.InputParameters };
                opportunityId = loseRequest.OpportunityClose.ToEntity<OpportunityClose>().OpportunityId.Id;
            } else if (message == "Win") {
                var winRequest = new WinOpportunityRequest { Parameters = ctx.InputParameters };
                opportunityId = winRequest.OpportunityClose.ToEntity<OpportunityClose>().OpportunityId.Id;
            }
            var upd = new Opportunity(opportunityId);
            upd.Description = "SetFromWinLose";
            context.OrganizationAdminService.Update(upd);
        }
    }
}
