using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Activities;

namespace WorkflowExecuter
{
    [DataContract]
    internal class CallCodeActivity : IWorkflowNode
    {
        [DataMember]
        public string CodeActivityName;
        [DataMember]
        public Dictionary<string, string> inArguments;
        [DataMember]
        public Dictionary<string, string> outArguments;


        public CallCodeActivity(string CodeActivityName, Dictionary<string, string> inArguments, Dictionary<string, string> outArguments)
        {
            this.CodeActivityName = CodeActivityName;
            this.inArguments = inArguments;
            this.outArguments = outArguments;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var variablesInstance = variables;
            var arguments = this.inArguments.Where(arg => !outArguments.ContainsKey(arg.Value) && variablesInstance[arg.Value] != null)
                .ToDictionary(arg => arg.Key, arg => (outArguments.ContainsKey(arg.Value) ? null : variablesInstance[arg.Value]));

            var codeActivities = variables["CodeActivites"] as Dictionary<string, CodeActivity>;
            var codeActivity = codeActivities[CodeActivityName];
            var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
            var workflowContext = new XrmWorkflowContext();
            workflowContext.PrimaryEntityId = primaryEntity.Id;
            workflowContext.PrimaryEntityName = primaryEntity.LogicalName;

            if (factory is IServiceProvider serviceProvider)
            {
                var pluginExecutionContext = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
                workflowContext.InitiatingUserId = pluginExecutionContext.InitiatingUserId;
                workflowContext.UserId = pluginExecutionContext.UserId;
            }
            
            var invoker = new WorkflowInvoker(codeActivity);
            invoker.Extensions.Add(trace);
            invoker.Extensions.Add(workflowContext);
            invoker.Extensions.Add(factory);
            var variablesPostExecution = invoker.Invoke(arguments);
            foreach (var outArg in outArguments)
            {
                variables[outArg.Key] = variablesPostExecution[outArg.Value];
            }

            foreach (var outArg in outArguments.Where(arg => variablesPostExecution[arg.Value] is EntityReference))
            {
                var reference = variablesPostExecution[outArg.Value] as EntityReference;
                var retrieved = orgService.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                var regex = new Regex(@"[\w]+(?=_)");
                var entityVariableName = regex.Match(outArg.Key).Value + "_entity";
                variables[$"CreatedEntities(\"{entityVariableName}\")"] = retrieved;
            }
        }
    }
}
