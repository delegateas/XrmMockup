using System;
using System.Collections.Generic;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;

namespace DG.Some.Namespace {

    public class ParentPreCreatePlugin : Plugin {
        public ParentPreCreatePlugin() : base(typeof(ParentPreCreatePlugin)) {
            RegisterPluginStep("mock_parent",
                EventOperation.Create,
                ExecutionStage.PreOperation,
                Execute)
                  .SetExecutionMode(ExecutionMode.Synchronous)
                  ;
        }

        protected void Execute(LocalPluginContext localContext)
        {
            //assign the parent to the record owning team.

            var localPluginContext = new LocalPluginContext(localContext.ServiceProvider);

            ITracingService tracer = (ITracingService)localContext.ServiceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)localContext.ServiceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)localContext.ServiceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);


            var target = (Entity)context.InputParameters["Target"];
            tracer.Trace($"target logical name {target.LogicalName}");

            //find the "record owner team" team
            var q = new QueryExpression("team");
            q.Criteria.AddCondition("name", ConditionOperator.Equal, "* RECORD OWNER TEAM *");
            var team = service.RetrieveMultiple(q).Entities.Single();
            tracer.Trace($"found team");

            //assign the target record to the team
            var req = new AssignRequest();
            req.Assignee = team.ToEntityReference();
            req.Target = target.ToEntityReference();

            tracer.Trace($"assigning");
            var resp = (AssignResponse)service.Execute(req);



        }
    }
}
