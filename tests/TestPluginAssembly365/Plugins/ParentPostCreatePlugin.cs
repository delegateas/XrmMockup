using Microsoft.Xrm.Sdk;using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using XrmPluginCore;
using XrmPluginCore.Enums;

namespace DG.Some.Namespace 
{
    public class MockParent : Entity
    {
        public MockParent() : base("mock_parent")
        {
            
        }
    }

    public class ParentPostCreatePlugin : Plugin
    {
        public ParentPostCreatePlugin() 
        {
            RegisterPluginStep<MockParent>(
                EventOperation.Create,
                ExecutionStage.PostOperation,
                Execute)
                  .SetExecutionMode(ExecutionMode.Synchronous);
        }

        protected void Execute(LocalPluginContext localContext)
        {
            //assign the parent to the record owning team.

            var target = (Entity)localContext.PluginExecutionContext.InputParameters["Target"];
            localContext.TracingService.Trace($"target logical name {target.LogicalName}");

            //find the "record owner team" team
            var q = new QueryExpression("team");
            q.Criteria.AddCondition("name", ConditionOperator.Equal, "* RECORD OWNER TEAM *");
            var team = localContext.OrganizationService.RetrieveMultiple(q).Entities.Single();
            localContext.TracingService.Trace($"found team");

            //assign the target record to the team
            var req = new AssignRequest();
            req.Assignee = team.ToEntityReference();
            req.Target = target.ToEntityReference();

            localContext.TracingService.Trace($"assigning");
            var resp = (AssignResponse)localContext.OrganizationService.Execute(req);
        }
    }
}
