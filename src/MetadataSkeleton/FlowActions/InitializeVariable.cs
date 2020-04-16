using System;
using System.Collections.Generic;
using System.Text;

namespace MetadataSkeleton.FlowActions
{
    public class InitializeVariable : IFlowAction
    {
        private readonly string name;
        private readonly string type;
        private readonly string valueExpr;

        public InitializeVariable(string name, string type, string valueExpr)
        {
            this.name = name;
            this.type = type;
            this.valueExpr = valueExpr;
        }

        public FlowStatus Execute(GlobalFlowStructures globalFlowStructures)
        {
            var value = GetValue(valueExpr, globalFlowStructures);
            var castedValue = ConvertType(value, type);
            if (castedValue == null) return FlowStatus.Failed;/*throw new Exception($"Unable to cast {valueExpr} to type {type}");*/

            globalFlowStructures.Variables[name] = castedValue;
            return FlowStatus.Succeeded;
        }

        private string GetKey(string s, string fromKey, string toKey)
        {
            int pFrom = s.IndexOf(fromKey) + fromKey.Length;
            int pTo = s.LastIndexOf(toKey);
            return s.Substring(pFrom, pTo - pFrom);
        }

        private object GetValue(string valueExpr, GlobalFlowStructures globalFlowStructures)
        {
            var triggerBodyStart = "@{triggerBody()['";
            var triggerBodyEnd = "']}";
            if (valueExpr.StartsWith(triggerBodyStart))
            {
                var key = GetKey(valueExpr, triggerBodyStart, triggerBodyEnd);
                if (!globalFlowStructures.TriggerBody.ContainsKey(key)) return null;
                return globalFlowStructures.TriggerBody[key];
            }

            return null;
        }

        private object ConvertType(object value, string type)
        {
            switch(type)
            {
                case "boolean": return value is bool ? value : null;
                case "int": return value is int ? value : null;
                case "float": return value is float ? value : null;
                case "string": return value as string;
                case "object":
                case "array": return value;
                default: return null;
            }
        }

    }
}
