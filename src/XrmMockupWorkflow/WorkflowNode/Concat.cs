using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Concat : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public Concat(string[][] Parameters, string VariableName)
        {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var variablesInstance = variables;
            var strings = Parameters[0].Select(p => (string)variablesInstance[p]).ToArray();
            variables[VariableName] = string.Concat(strings);
        }
    }
}
