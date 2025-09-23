using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class SetAttributeValue : IWorkflowNode
    {
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public SetAttributeValue(string VariableId, string EntityName)
        {
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var relatedlinked = "relatedlinked";
            if (VariableId.Contains(relatedlinked))
            {
                var reg = new Regex(@"\([^#]*\#");
                variables[relatedlinked + "_" + EntityName] = reg.Match(VariableId).Value.Replace("\"", "").Replace(relatedlinked + "_", "").TrimEdge();
                return;
            }

            if (!variables.ContainsKey(VariableId))
            {
                throw new WorkflowException($"The variable with id '{VariableId}' before being set, check the workflow has the correct format.");
            }

            var entity = variables[VariableId] as Entity;
            orgService.Update(entity);

        }
    }
}
