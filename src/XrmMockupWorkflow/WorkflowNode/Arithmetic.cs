using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Workflow;

namespace WorkflowExecuter
{
    [DataContract]
    internal class Arithmetic : IWorkflowNode
    {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Arithmetic(string[][] Parameters, string TargetType, string VariableName, string Method)
        {
            this.Parameters = Parameters;
            this.TargetType = TargetType;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
    IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            var var1 = variables[Parameters[0][0]];
            var var2 = variables[Parameters[0][1]];
            var variablesInstance = variables;

            if (TargetType == null)
            {
                var nonNull = Parameters[0].Select(v => variablesInstance[v]).FirstOrDefault(v => v != null);
                if (nonNull == null)
                {
                    variables[VariableName] = null;
                    return;
                }
                TargetType = nonNull.GetType().Name;
            }

            if (TargetType == "String" && Method == "Add")
            {
                var strings = Parameters[0].Select(v => variablesInstance[v] as string);
                variables[VariableName] = String.Concat(strings);
                return;
            }

            if (TargetType == "EntityCollection" && Method == "Add")
            {
                var entities = Parameters[0]
                    .Select(v => variablesInstance[v] as EntityReference)
                    .Where(r => r != null)
                    .Select(r => orgService.Retrieve(r.LogicalName, r.Id, new ColumnSet(true)));
                variables[VariableName] = new EntityCollection(entities.ToList());
                return;
            }

            if (TargetType == "XrmTimeSpan")
            {
                if (var1 == null || var2 == null)
                {
                    variables[VariableName] = null;
                    return;
                }
            }
            if (TargetType == "DateTime")
            {
                if (var2 is XrmTimeSpan span)
                {
                    if (Method == "Add")
                    {
                        variables[VariableName] = ((DateTime)var1).AddXrmTimeSpan(span);
                        return;
                    }

                    if (Method == "Subtract")
                    {
                        variables[VariableName] = ((DateTime)var1).SubtractXrmTimeSpan(span);
                        return;
                    }
                }
                throw new NotImplementedException("Unknown timespan type when adding datetimes");
            }

            decimal? dec1 = null;
            decimal? dec2 = null;

            if (var1 == null && var2 == null)
            {
                variables[VariableName] = null;
                return;
            }

            switch (TargetType)
            {
                case "Money":
                    dec1 = ConvertMoneyToDecimal(var1);
                    dec2 = ConvertMoneyToDecimal(var2);
                    break;
                case "Int32":
                    dec1 = var1 == null ? 0 : (int)var1;
                    dec2 = var2 == null ? 0 : (int)var2;
                    break;
                case "Decimal":
                    dec1 = var1 == null ? 0 : Convert.ToDecimal(var1);
                    dec2 = var2 == null ? 0 : Convert.ToDecimal(var2);
                    break;
                default:
                    break;
            }

            if (!dec1.HasValue || !dec2.HasValue)
            {
                throw new NotImplementedException($"Unknown target type '{TargetType}'");
            }

            decimal? result = null;
            switch (Method)
            {
                case "Multiply":
                    result = dec1 * dec2;
                    break;
                case "Divide":
                    result = dec1 / dec2;
                    break;
                case "Subtract":
                    result = dec1 - dec2;
                    break;
                case "Add":
                    result = dec1 + dec2;
                    break;
                default:
                    break;
            }

            if (!result.HasValue)
            {
                throw new NotImplementedException($"Unknown arithmetic method '{Method}'");
            }

            switch (TargetType)
            {
                case "Money":
                    variables[VariableName] = new Money(result.Value);
                    break;
                case "Int32":
                    variables[VariableName] = (int)result.Value;
                    break;
                case "Decimal":
                    variables[VariableName] = result.Value;
                    break;
                default:
                    break;
            }
        }

        private static decimal? ConvertMoneyToDecimal(object value)
        {
            if (value is null) return 0;
            if (value is Money moneyValue) return moneyValue.Value;
            if (value is decimal decimalValue) return decimalValue;
            if (value is int intValue) return intValue;
            throw new NotImplementedException($"Unknown type when converting to money: {value.GetType()}");
        }
    }
}
