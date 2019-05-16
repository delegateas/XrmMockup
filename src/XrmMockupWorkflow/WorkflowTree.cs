using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Activities;
using Microsoft.Xrm.Sdk.Workflow;
using DG.Tools.XrmMockup;
using Microsoft.Xrm.Sdk.Metadata;
using System.Text;

namespace WorkflowExecuter {
    [Serializable]
    [KnownType(typeof(CreateVariable))]
    [KnownType(typeof(SelectFirstNonNull))]
    [KnownType(typeof(Arithmetic))]
    [KnownType(typeof(Aggregate))]
    [KnownType(typeof(Concat))]
    [KnownType(typeof(AddTime))]
    [KnownType(typeof(SubtractTime))]
    [KnownType(typeof(DiffInTime))]
    [KnownType(typeof(Trim))]
    [KnownType(typeof(Skip))]
    [KnownType(typeof(GetEntityProperty))]
    [KnownType(typeof(ConditionExp))]
    [KnownType(typeof(Condition))]
    [KnownType(typeof(LogicalComparison))]
    [KnownType(typeof(SetEntityProperty))]
    [KnownType(typeof(SetAttributeValue))]
    [KnownType(typeof(CreateEntity))]
    [KnownType(typeof(UpdateEntity))]
    [KnownType(typeof(AssignEntity))]
    [KnownType(typeof(ActivityList))]
    [KnownType(typeof(TerminateWorkflow))]
    [KnownType(typeof(RollUp))]
    [KnownType(typeof(CallCodeActivity))]
    [KnownType(typeof(ConvertType))]
    [KnownType(typeof(Assign))]
    [KnownType(typeof(SendEmail))]
    internal class WorkflowTree {
        [DataMember]
        public IWorkflowNode StartActivity;
        [DataMember]
        public Dictionary<string, object> Variables;
        [DataMember]
        public bool? TriggerOnCreate;
        [DataMember]
        public bool? TriggerOnDelete;
        [DataMember]
        public HashSet<string> TriggerFieldsChange;
        [DataMember]
        public workflow_runas? RunAs;
        [DataMember]
        public Workflow_Scope? Scope;
        [DataMember]
        public workflow_stage? CreateStage;
        [DataMember]
        public workflow_stage? UpdateStage;
        [DataMember]
        public workflow_stage? DeleteStage;
        [DataMember]
        public Workflow_Mode? Mode;
        [DataMember]
        public Guid? Owner;
        [DataMember]
        public string PrimaryEntityLogicalname;
        [DataMember]
        public Dictionary<string, CodeActivity> CodeActivites;
        [DataMember]
        public List<WorkflowArgument> Input;
        [DataMember]
        public List<WorkflowArgument> Output;
        [DataMember]
        public string WorkflowName { get; set; }


        public WorkflowTree(IWorkflowNode StartActivity) : this(StartActivity, false, false, new HashSet<string>(),
            workflow_runas.CallingUser, Workflow_Scope.User, workflow_stage.Postoperation, workflow_stage.Postoperation,
            workflow_stage.Postoperation, Workflow_Mode.Realtime, null, "", new Dictionary<string, CodeActivity>(),
            new List<WorkflowArgument>(), new List<WorkflowArgument>()) { }

        public WorkflowTree(IWorkflowNode StartActivity, bool? TriggerOnCreate, bool? TriggerOnDelete,
            HashSet<string> TriggerFieldsChange, workflow_runas? RunAs, Workflow_Scope? Scope,
            workflow_stage? CreateStage, workflow_stage? UpdateStage, workflow_stage? DeleteStage, Workflow_Mode? Mode,
            Guid? Owner, string PrimaryEntityLogicalname, Dictionary<string, CodeActivity> CodeActivites,
            List<WorkflowArgument> Input, List<WorkflowArgument> Output) {
            this.Variables = new Dictionary<string, object>();
            this.StartActivity = StartActivity;
            this.TriggerOnCreate = TriggerOnCreate;
            this.TriggerOnDelete = TriggerOnDelete;
            this.TriggerFieldsChange = TriggerFieldsChange;
            this.RunAs = RunAs;
            this.Scope = Scope;
            this.CreateStage = CreateStage;
            this.UpdateStage = UpdateStage;
            this.DeleteStage = DeleteStage;
            this.Mode = Mode;
            this.Owner = Owner;
            this.PrimaryEntityLogicalname = PrimaryEntityLogicalname;
            this.CodeActivites = CodeActivites;
            this.Input = Input;
            this.Output = Output;
        }

        

        public WorkflowTree Execute(Entity primaryEntity, TimeSpan timeOffset, 
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            if (primaryEntity.Id == Guid.Empty) {
                throw new WorkflowException("The primary entity must have an id");
            }
            Reset();
            Variables["InputEntities(\"primaryEntity\")"] = primaryEntity;
            Variables["ExecutionTime"] = DateTime.Now.Add(timeOffset);
            var transactioncurrencyid = "transactioncurrencyid";
            if (primaryEntity.Attributes.ContainsKey(transactioncurrencyid)) {
                var currencyRef = primaryEntity.GetAttributeValue<EntityReference>(transactioncurrencyid);
                var exchangerate = "exchangerate";
                var currency = orgService.Retrieve("transactioncurrency", currencyRef.Id, new ColumnSet(exchangerate));
                Variables["ExchangeRate"] = currency[exchangerate];
            }
            StartActivity.Execute(ref Variables, timeOffset, orgService, factory, trace);
            return this;
        }

        internal void Reset() {
            Variables["True"] = true;
            Variables["False"] = false;
            Variables["ExchangeRate"] = null;
            Variables["CodeActivites"] = CodeActivites;
            Variables["InputEntities(\"primaryEntity\")"] = null;
            Variables["Wait"] = null;
        }

        internal void HardReset() {
            Variables = new Dictionary<string, object>();
            Reset();
        }
    }


    internal class WaitInfo {
        public ActivityList Element { get; private set; }
        public int ElementIndex { get; private set; }
        public Dictionary<string, object> VariablesInstance { get; private set; }
        public EntityReference PrimaryEntity { get; private set; }

        public WaitInfo(ActivityList Element, int ElementIndex, Dictionary<string, object> VariablesInstance, EntityReference PrimaryEntity) {
            this.Element = Element;
            this.ElementIndex = ElementIndex;
            this.VariablesInstance = VariablesInstance;
            this.PrimaryEntity = PrimaryEntity;
        }
    }


    [DataContract]
    public class WorkflowArgument {
        public enum DirectionType { Input, Output };
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public bool Required { get; private set; }
        [DataMember]
        public bool IsTarget { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        [DataMember]
        public DirectionType Direction { get; private set; }
        [DataMember]
        public string EntityLogicalName { get; private set; }

        public WorkflowArgument(string Name, bool Required, bool IsTarget, string Description, DirectionType Direction, string EntityLogicalName) {
            this.Name = Name;
            this.Required = Required;
            this.IsTarget = IsTarget;
            this.Description = Description;
            this.Direction = Direction;
            this.EntityLogicalName = EntityLogicalName;
        }

    }


    public interface IWorkflowNode {
        void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset, IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace);
    }

    [DataContract]
    internal class CreateVariable : IWorkflowNode {
        [DataMember]
        public object[][] Parameters { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public CreateVariable(object[][] Parameters, string TargetType, string VariableName) {
            this.Parameters = Parameters;
            this.TargetType = TargetType;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset, 
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            switch (TargetType) {
                case "String":
                    variables[VariableName] = ((string)Parameters[0][1]).Contains("(Arguments)") ? variables[(string)Parameters[0][1]] : (string)Parameters[0][1];
                    break;
                case "Boolean": {
                        var value = ((string)Parameters[0][1]).ToLower();
                        variables[VariableName] = value == "1" || value == "true" ? true : false;
                    }
                    break;
                case "Int32":
                    variables[VariableName] = int.Parse((string)Parameters[0][1]);
                    break;
                case "Guid":
                    variables[VariableName] = Guid.Parse((string)Parameters[0][1]);
                    break;
                case "Decimal":
                    variables[VariableName] = decimal.Parse((string)Parameters[0][1]);
                    break;
                case "OptionSetValue":
                    variables[VariableName] = new OptionSetValue(int.Parse((string)Parameters[0][1]));
                    break;
                case "Money":
                    variables[VariableName] = new Money(decimal.Parse((string)Parameters[0][1]));
                    break;
                case "EntityReference":
                    if (Parameters[0].Count() == 5 && ((string)Parameters[0][0]).Contains("EntityReference")) {
                        var entRef = new EntityReference((string)Parameters[0][1], (Guid)variables[(string)Parameters[0][3]]);
                        entRef.Name = (string)Parameters[0][2];
                        variables[VariableName] = entRef;
                    } else if (Parameters[0].Count() == 3 && ((string)Parameters[0][0]).Contains("EntityReference")) {
                        if (!variables.ContainsKey((string)Parameters[0][1])) {
                            throw new WorkflowException($"The variable '{(string)Parameters[0][1]}' has not been initialized");
                        }

                        variables[VariableName] = variables[(string)Parameters[0][1]];
                    } else if (Parameters[0].Count() == 3 && ((string)Parameters[0][0]).Contains("Guid")) {
                        variables[VariableName] = new Guid((string)Parameters[0][1]);
                    }
                    break;
                case "DateTime": {
                        var value = (string)Parameters[0][1];
                        if (int.TryParse(value, out int result)) {
                            variables[VariableName] = result;
                        } else {
                            variables[VariableName] = DateTime.Parse(value);
                        }
                    }
                    break;
                case "[System.DateTime.MinValue]": {
                        variables[VariableName] = DateTime.MinValue;
                    }
                    break;
                case "Object": {
                        if (Parameters[0][1] as string == "[System.DateTime.MinValue]") {
                            variables[VariableName] = DateTime.MinValue;
                        } else if (Parameters[0][1] as string == "[System.DateTime.MaxValue]") {
                            variables[VariableName] = DateTime.MaxValue;
                        } else {
                            variables[VariableName] = null;
                        }
                    }
                    break;
                case "XrmTimeSpan": {
                        var param = Parameters[0].Select(s => int.Parse(s as string)).ToArray();
                        variables[VariableName] = new XrmTimeSpan(param[4], param[3], param[0], param[1], param[2]);
                    }
                    break;
                case "EntityCollection": {
                        var variablesInstance = variables;
                        var entities = Parameters[0].Skip(1)
                            .Select(v => variablesInstance[(string)v] as EntityReference)
                            .Where(r => r != null)
                            .Select(r => orgService.Retrieve(r.LogicalName, r.Id, new ColumnSet(true)));
                        variables[VariableName] = new EntityCollection(entities.ToList());
                    }
                    break;
                default:
                    throw new WorkflowException($"Unknown target type: {TargetType}.");
            }
        }
    }

    [DataContract]
    internal class Persist : IWorkflowNode {
        public Persist() {
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset, 
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            variables["Wait"] = null;
        }
    }

    [DataContract]
    internal class SelectFirstNonNull : IWorkflowNode {
        [DataMember]
        public string[] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public SelectFirstNonNull(string[] Parameters, string VariableName) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            object value = null;
            foreach (var param in Parameters) {
                if (variables.ContainsKey(param) && variables[param] != null) {
                    value = variables[param];
                    break;
                }
            }

            variables[VariableName] = value;

        }
    }

    [DataContract]
    internal class Arithmetic : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Arithmetic(string[][] Parameters, string TargetType, string VariableName, string Method) {
            this.Parameters = Parameters;
            this.TargetType = TargetType;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var var1 = variables[Parameters[0][0]];
            var var2 = variables[Parameters[0][1]];
            var variablesInstance = variables;

            if (TargetType == null) {
                var nonNull = Parameters[0].Select(v => variablesInstance[v]).FirstOrDefault(v => v != null);
                if (nonNull == null) {
                    variables[VariableName] = null;
                    return;
                }
                TargetType = nonNull.GetType().Name;
            }

            if (TargetType == "String" && Method == "Add") {
                var strings = Parameters[0].Select(v => variablesInstance[v] as string);
                variables[VariableName] = String.Concat(strings);
                return;
            }

            if (TargetType == "EntityCollection" && Method == "Add") {
                var entities = Parameters[0]
                    .Select(v => variablesInstance[v] as EntityReference)
                    .Where(r => r != null)
                    .Select(r => orgService.Retrieve(r.LogicalName, r.Id, new ColumnSet(true)));
                variables[VariableName] = new EntityCollection(entities.ToList());
                return;
            }

            if (var1 == null || var2 == null) {
                variables[VariableName] = null;
                return;
            }

            if (TargetType == "DateTime") {
                if (var2 is XrmTimeSpan) {
                    if (Method == "Add") {
                        variables[VariableName] = ((DateTime)var1).AddXrmTimeSpan((XrmTimeSpan)var2);
                        return;
                    }

                    if (Method == "Subtract") {
                        variables[VariableName] = ((DateTime)var1).SubtractXrmTimeSpan((XrmTimeSpan)var2);
                        return;
                    }
                }
                throw new NotImplementedException("Unknown timespan type when adding datetimes");
            }

            decimal? dec1 = null;
            decimal? dec2 = null;

            switch (TargetType) {
                case "Money":
                    dec1 = var1 is Money ? (var1 as Money).Value : (decimal)var1;
                    dec2 = var2 is Money ? (var2 as Money).Value : (decimal)var2;
                    break;
                case "Int32":
                    dec1 = (int)var1;
                    dec2 = (int)var2;
                    break;
                case "Decimal":
                    dec1 = (decimal)var1;
                    dec2 = (decimal)var2;
                    break;
                default:
                    break;
            }

            if (!dec1.HasValue || !dec2.HasValue) {
                throw new NotImplementedException($"Unknown target type '{TargetType}'");
            }

            decimal? result = null;
            switch (Method) {
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

            if (!result.HasValue) {
                throw new NotImplementedException($"Unknown arithmetic method '{Method}'");
            }

            switch (TargetType) {
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
    }

    [DataContract]
    internal class Aggregate : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Aggregate(string[][] Parameters, string VariableName, string Method) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var parameterKey = Parameters[0][0];
            var parameters = variables[parameterKey] as IEnumerable<object>;
            var paramType = parameters.FirstOrDefault();
            var variablesInstance = variables;

            if (paramType == null) {
                variables[VariableName] = null;
                return;
            }

            IEnumerable<decimal> comparableParameters = null;
            if (paramType is int) {
                comparableParameters = parameters.Select(p => (decimal)p);
            } else if (paramType is Money) {
                comparableParameters = parameters.Select(p => (p as Money).Value);
            } else if (paramType is decimal) {
                comparableParameters = parameters.Select(p => (decimal)p);
            } else if (paramType is DateTime) {
                comparableParameters = parameters.Select(p => (decimal)((DateTime)p).Ticks);
            }

            if (comparableParameters == null) {
                throw new NotImplementedException($"Unknown type when aggregating '{paramType}'");
            }

            decimal? result = null;
            switch (Method) {
                case "Maximum":
                    result = comparableParameters.Max();
                    break;
                case "Minimum":
                    result = comparableParameters.Min();
                    break;
                case "Sum":
                    if (!(paramType is DateTime)) result = comparableParameters.Sum();
                    break;
                case "Average":
                    if (!(paramType is DateTime)) result = comparableParameters.Average();
                    break;
                default:
                    break;
            }

            if (!result.HasValue) {
                throw new NotImplementedException($"Unknown aggregate method '{Method}'");
            }

            if (paramType is int) {
                variables[VariableName] = (int)result.Value;
            } else if (paramType is Money) {
                variables[VariableName] = new Money(result.Value);
            } else if (paramType is decimal) {
                variables[VariableName] = result.Value;
            } else if (paramType is DateTime) {
                variables[VariableName] = new DateTime((long)result.Value);
            }
        }
    }


    [DataContract]
    internal class Concat : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public Concat(string[][] Parameters, string VariableName) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var variablesInstance = variables;
            var strings = Parameters[0].Select(p => (string)variablesInstance[p]).ToArray();
            variables[VariableName] = string.Concat(strings);
        }
    }

    [DataContract]
    internal class AddTime : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public AddTime(string[][] Parameters, string VariableName, string Amount) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var toAdd = variables[Parameters[0][0]] as int?;
            var date = variables[Parameters[0][1]] as DateTime?;
            if (toAdd.HasValue && date.HasValue) {
                switch (Amount) {
                    case "AddDays":
                        variables[VariableName] = date.Value.AddDays(toAdd.Value);
                        break;
                    case "AddHours":
                        variables[VariableName] = date.Value.AddHours(toAdd.Value);
                        break;
                    case "AddMonths":
                        variables[VariableName] = date.Value.AddMonths(toAdd.Value);
                        break;
                    case "AddWeeks":
                        variables[VariableName] = date.Value.AddDays(7 * toAdd.Value);
                        break;
                    case "AddYears":
                        variables[VariableName] = date.Value.AddYears(toAdd.Value);
                        break;
                }
            } else {
                variables[VariableName] = null;
            }
        }
    }

    [DataContract]
    internal class SubtractTime : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public SubtractTime(string[][] Parameters, string VariableName, string Amount) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var toSubtract = variables[Parameters[0][0]] as int?;
            var date = variables[Parameters[0][1]] as DateTime?;
            if (toSubtract.HasValue && date.HasValue) {
                switch (Amount) {
                    case "SubtractDays":
                        variables[VariableName] = date.Value.AddDays(-toSubtract.Value);
                        break;
                    case "SubtractHours":
                        variables[VariableName] = date.Value.AddHours(-toSubtract.Value);
                        break;
                    case "SubtractMonths":
                        variables[VariableName] = date.Value.AddMonths(-toSubtract.Value);
                        break;
                    case "SubtractWeeks":
                        variables[VariableName] = date.Value.AddDays(-7 * toSubtract.Value);
                        break;
                    case "SubtractYears":
                        variables[VariableName] = date.Value.AddYears(-toSubtract.Value);
                        break;
                }
            } else {
                variables[VariableName] = null;
            }
        }
    }

    [DataContract]
    internal class DiffInTime : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Amount { get; private set; }

        public DiffInTime(string[][] Parameters, string VariableName, string Amount) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Amount = Amount;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var date1 = variables[Parameters[0][0]] as DateTime?;
            var date2 = variables[Parameters[0][1]] as DateTime?;
            if (date1.HasValue && date2.HasValue) {
                var timespan = date1.Value.Subtract(date2.Value);
                switch (Amount) {
                    case "DiffInDays":
                        variables[VariableName] = timespan.Days;
                        break;
                    case "DiffInHours":
                        variables[VariableName] = timespan.Hours;
                        break;
                    case "DiffInMinutes":
                        variables[VariableName] = timespan.Minutes;
                        break;
                    case "DiffInMonths":
                        variables[VariableName] = Utility.GetDiffMonths(date1.Value, date2.Value);
                        break;
                    case "DiffInWeeks":
                        variables[VariableName] = timespan.Days * 7;
                        break;
                    case "DiffInYears":
                        variables[VariableName] = Utility.GetDiffYears(date1.Value, date2.Value);
                        break;
                }
            } else {
                variables[VariableName] = null;
            }
        }
    }

    [DataContract]
    internal class Trim : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string Method { get; private set; }

        public Trim(string[][] Parameters, string VariableName, string Method) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
            this.Method = Method;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var word = (string)variables[Parameters[0][0]];
            var trimLength = int.Parse((string)variables[Parameters[0][1]]);
            switch (Method) {
                case "TrimLeft":
                    variables[VariableName] = word.Substring(trimLength);
                    break;
                case "TrimRight":
                    variables[VariableName] = word.Substring(0, word.Length - trimLength);
                    break;
                default:
                    throw new NotImplementedException($"Unknown trim method '{Method}'");
            }
        }
    }

    [DataContract]
    internal class RetrieveLastExecutionTime : IWorkflowNode {
        [DataMember]
        public string VariableName { get; private set; }

        public RetrieveLastExecutionTime(string VariableName) {
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            variables[VariableName] = variables["ExecutionTime"];
        }
    }

    [DataContract]
    internal class RetrieveCurrentTime : IWorkflowNode {
        [DataMember]
        public string VariableName { get; private set; }

        public RetrieveCurrentTime(string VariableName) {
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            variables[VariableName] = DateTime.Now.Add(timeOffset);
        }
    }

    [DataContract]
    internal class CustomOperationArguments : IWorkflowNode {
        [DataMember]
        public string[][] Parameters { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }

        public CustomOperationArguments(string[][] Parameters, string VariableName) {
            this.Parameters = Parameters;
            this.VariableName = VariableName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var variableName = "{" + Parameters[0][0] + "(Arguments)}";
            variables[VariableName] = variables.ContainsKey(variableName) ? variables[variableName] : null;
        }
    }

    [DataContract]
    internal class Skip : IWorkflowNode {
        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
        }
    }

    [DataContract]
    internal class GetEntityProperty : IWorkflowNode {
        [DataMember]
        public string Attribute { get; private set; }
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string EntityLogicalName { get; private set; }
        [DataMember]
        public string VariableName { get; private set; }
        [DataMember]
        public string TargetType { get; private set; }

        public GetEntityProperty(string Attribute, string EntityId, string EntityLogicalName, string VariableName, string TargetType) {
            this.Attribute = Attribute;
            this.EntityId = EntityId;
            this.EntityLogicalName = EntityLogicalName;
            this.VariableName = VariableName;
            this.TargetType = TargetType;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            Entity entity = null;
            if (EntityId.Contains("related_")) {
                var regex = new Regex(@"_.+#");
                var relatedAttr = regex.Match(EntityId).Value.TrimEdge();
                var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
                if (!primaryEntity.Attributes.ContainsKey(relatedAttr)) {
                    variables[VariableName] = null;
                    return;
                }
                var entRef = primaryEntity.Attributes[relatedAttr] as EntityReference;
                if (entRef == null)
                {
                    variables[VariableName] = null;
                    return;
                }
                entity = orgService.Retrieve(EntityLogicalName, entRef.Id, new ColumnSet(true));
                
                if (!variables.ContainsKey(EntityId))
                {
                    variables.Add(EntityId, entity);
                }

            } else {
                entity = variables.ContainsKey(EntityId) ? variables[EntityId] as Entity : null;
            }

            if (entity == null)
            {
                variables[VariableName] = null;
                return;
            }

            if (entity.LogicalName != EntityLogicalName) {
                variables[VariableName] = null;
                return;
            }

            if (Attribute == "!Process_Custom_Attribute_URL_")
            {
                variables[VariableName] = "https://somedummycrm.crm.dynamics.com/main.aspx?someparametrs";
                return;
            }

            if (!entity.Attributes.ContainsKey(Attribute)) {
                variables[VariableName] = null;
                return;
            }
            var attr = entity.Attributes[Attribute];
            if (TargetType == "EntityReference") {
                if (attr is Guid)
                {
                    attr = new EntityReference(EntityLogicalName, (Guid)attr);
                }
                else if (!(attr is EntityReference))
                {
                    throw new InvalidCastException($"Cannot convert {attr.GetType().Name} to {TargetType}");
                }
            }
            if (TargetType == "String")
            {
                if (attr is OptionSetValue)
                {
                    attr = Util.GetOptionSetValueLabel(entity.LogicalName, Attribute, attr as OptionSetValue, orgService);
                }
                else if (attr is bool)
                {
                    attr = Util.GetBooleanLabel(entity.LogicalName, Attribute, (bool) attr, orgService);
                }
                else if (attr is EntityReference)
                {
                    attr = Util.GetPrimaryName(attr as EntityReference, orgService);
                }
                else if (attr is Money)
                {
                    // TODO: should respect record currency and user format preferences
                    attr = $"{(attr as Money).Value:C}";
                }
                else if (attr is int)
                {
                    // TODO: should respect user format preferences
                    attr = $"{((int) attr):N0}";
                }
                else if (attr != null && !(attr is string))
                {
                    throw new InvalidCastException($"Cannot convert {attr.GetType().Name} to {TargetType}");
                }
            }
            variables[VariableName] = attr;
        }
    }

    [DataContract]
    internal class ConditionExp : IWorkflowNode {
        [DataMember]
        public ConditionOperator Operator { get; private set; }
        [DataMember]
        public string[] Parameters { get; private set; }
        [DataMember]
        public string Operand { get; private set; }
        [DataMember]
        public string ReturnName { get; private set; }

        

        public ConditionExp(string Operator, string[] Parameters, string Operand, string ReturnName) {
            this.Operator = (ConditionOperator)Enum.Parse(typeof(ConditionOperator), Operator);
            this.Parameters = Parameters;
            this.Operand = Operand;
            this.ReturnName = ReturnName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var operand = variables[Operand];
            var variablesInstance = variables;
            var parameters = Parameters != null && Parameters.Count() == 1 ? Parameters[0].Split(',').Select(p => variablesInstance[p.Trim()]).ToArray() : null;

            if (Operator != ConditionOperator.NotNull && Operator != ConditionOperator.Null && (operand == null || parameters == null)) {
                variables[ReturnName] = null;
                return;
            }

            switch (Operator) {
                case ConditionOperator.Equal:
                    variables[ReturnName] = operand.Equals(parameters[0]);
                    break;

                case ConditionOperator.NotEqual:
                    variables[ReturnName] = !operand.Equals(parameters[0]);
                    break;

                case ConditionOperator.BeginsWith:
                    variables[ReturnName] = ((string)operand).StartsWith((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotBeginWith:
                    variables[ReturnName] = !((string)operand).StartsWith((string)parameters[0]);
                    break;
                    
                case ConditionOperator.EndsWith:
                    variables[ReturnName] = ((string)operand).EndsWith((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotEndWith:
                    variables[ReturnName] = !((string)operand).EndsWith((string)parameters[0]);
                    break;

                case ConditionOperator.Contains:
                    variables[ReturnName] = ((string)operand).Contains((string)parameters[0]);
                    break;

                case ConditionOperator.DoesNotContain:
                    variables[ReturnName] = !((string)operand).Contains((string)parameters[0]);
                    break;

                case ConditionOperator.NotNull:
                    variables[ReturnName] = operand != null;
                    break;

                case ConditionOperator.Null:
                    variables[ReturnName] = operand == null;
                    break;

                case ConditionOperator.In:
                    variables[ReturnName] = parameters.Any(x => x.Equals(operand));
                    break;

                case ConditionOperator.NotIn: 
                    variables[ReturnName] = !parameters.Any(x => x.Equals(operand));
                    break;

                case ConditionOperator.On:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) == 0;
                    break;

                case ConditionOperator.OnOrAfter:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) >= 0;
                    break;

                case ConditionOperator.OnOrBefore:
                    variables[ReturnName] = ((DateTime)operand).CompareTo((DateTime)parameters[0]) <= 0;
                    break;

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterEqual:
                case ConditionOperator.LessThan:
                case ConditionOperator.LessEqual:
                    decimal? comparableOperand = null;
                    decimal? comparableParameter = null;
                    if (parameters[0] == null) {
                        variables[ReturnName] = false;
                        break;
                    }
                    if (parameters[0] is int) {
                        comparableOperand = (int)operand;
                        comparableParameter = (int)parameters[0];
                    } else if (parameters[0] is Money) {
                        comparableOperand = (operand as Money).Value;
                        comparableParameter = (parameters[0] as Money).Value;
                    } else if (parameters[0] is decimal) {
                        comparableOperand = (decimal)operand;
                        comparableParameter = (decimal)parameters[0];
                    } else if (parameters[0] is DateTime) {
                        comparableOperand = ((DateTime)operand).Ticks;
                        comparableParameter = ((DateTime)parameters[0]).Ticks;
                    }

                    if (comparableParameter == null) {
                        throw new NotImplementedException($"Unknown type when trying to compare");
                    }

                    switch (Operator) {
                        case ConditionOperator.GreaterThan:
                            variables[ReturnName] = comparableOperand > comparableParameter;
                            break;
                        case ConditionOperator.GreaterEqual:
                            variables[ReturnName] = comparableOperand >= comparableParameter;
                            break;
                        case ConditionOperator.LessThan:
                            variables[ReturnName] = comparableOperand < comparableParameter;
                            break;
                        case ConditionOperator.LessEqual:
                            variables[ReturnName] = comparableOperand <= comparableParameter;
                            break;
                    }
                    break;

                default:
                    throw new NotImplementedException($"Unknown operator '{Operator}'");
            }
        }
    }

    [DataContract]
    internal class Condition : IWorkflowNode {
        [DataMember]
        public string GuardId { get; private set; }
        [DataMember]
        public IWorkflowNode Then { get; private set; }
        [DataMember]
        public IWorkflowNode Otherwise { get; internal set; }

        public Condition(string GuardId, IWorkflowNode Then, IWorkflowNode Otherwise) {
            this.GuardId = GuardId;
            this.Then = Then;
            this.Otherwise = Otherwise;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            if (variables[GuardId] != null && (bool)variables[GuardId]) {
                Then.Execute(ref variables, timeOffset, orgService, factory, trace);
            } else {
                Otherwise.Execute(ref variables, timeOffset, orgService, factory, trace);
            }
        }
    }

    [DataContract]
    internal class LogicalComparison : IWorkflowNode {
        [DataMember]
        public LogicalOperator Operator { get; private set; }
        [DataMember]
        public string LeftOperand { get; private set; }
        [DataMember]
        public string RightOperand { get; private set; }
        [DataMember]
        public string Result { get; private set; }

        public LogicalComparison(string Operator, string LeftOperand, string RightOperand, string Result) {
            this.Operator = (LogicalOperator)Enum.Parse(typeof(LogicalOperator), Operator);
            this.LeftOperand = LeftOperand;
            this.RightOperand = RightOperand;
            this.Result = Result;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var left = (bool?)variables[LeftOperand];
            var right = (bool?)variables[RightOperand];

            if (!left.HasValue || !right.HasValue) {
                variables[Result] = null;
                return;
            }

            switch (Operator) {
                case LogicalOperator.Or:
                    variables[Result] = left.Value || right.Value;
                    break;
                case LogicalOperator.And:
                    variables[Result] = left.Value && right.Value;
                    break;
                default:
                    throw new NotImplementedException($"Unknown operator '{Operator}'");
            }
        }
    }

    [DataContract]
    internal class Assign : IWorkflowNode {
        [DataMember]
        public string To { get; private set; }
        [DataMember]
        public string Value { get; private set; }

        public Assign(string To, string Value) {
            this.To = To;
            this.Value = Value;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var comparaters = new string[] { "<", "<=", "==", ">=", ">" };
            if (comparaters.Any(c => Value.Contains(c))) {
                var comparater = comparaters.First(c => Value.Contains(c));
                var sides = Value.Split(new[] { comparater }, StringSplitOptions.None).Select(s => s.Trim()).ToArray();
                var left = sides[0].ToCorrectType(variables, timeOffset);
                var right = sides[1].ToCorrectType(variables, timeOffset);
                if (left is DateTime && right is DateTime) {
                    var comparison = ((DateTime)left).CompareTo((DateTime)right);
                    switch (comparater) {
                        case "<":
                            variables[To] = comparison < 0;
                            break;
                        case "<=":
                            variables[To] = comparison <= 0;
                            break;
                        case "==":
                            variables[To] = comparison == 0;
                            break;
                        case ">=":
                            variables[To] = comparison >= 0;
                            break;
                        case ">":
                            variables[To] = comparison > 0;
                            break;
                    }
                    return;
                }

                throw new NotImplementedException("Unknown type when assigning with a value containing comparaters");
            }

            if (Value.Contains(".Id")) {
                var toEntity = variables[To.Replace(".Id", "")] as Entity;
                var valueEntity = variables[Value.Replace(".Id", "")] as Entity;
                toEntity.Id = valueEntity.Id;
                return;
            }

            if (Value.Contains("CreatedEntities(") && Value.Contains("#Temp") && variables.ContainsKey(To)) {
                var tmp = variables[Value] as Entity;
                var to = variables[To] as Entity;
                foreach (var attr in tmp.Attributes) {
                    to.Attributes[attr.Key] = attr.Value;
                }
                return;
            }

            if (Value.Contains(".VisualBasic.IIf(")) {
                var regex = new Regex(@"\(.+\)");
                var parameters = regex.Match(Value).Value.TrimEdge().Split(',').Select(s => s.Trim()).ToArray();
                if (parameters[0].Contains(".VisualBasic.IsNothing(")) {
                    var variable = regex.Match(parameters[0]).Value.TrimEdge();
                    variables[To] = !variables.ContainsKey(variable) || variables[variable] == null ?
                        parameters[1].ToCorrectType(variables, timeOffset) : parameters[2].ToCorrectType(variables, timeOffset);
                    return;
                }
                throw new NotImplementedException("Unknown condition in IIf in workflow");
            }

            variables[To] = Value.ToCorrectType(variables, timeOffset);
        }
    }

    [DataContract]
    internal class Postpone : IWorkflowNode {
        [DataMember]
        public string BlockExecution { get; private set; }
        [DataMember]
        public string PostponeUntil { get; private set; }

        public Postpone(string BlockExecution, string PostponeUntil) {
            this.BlockExecution = BlockExecution;
            this.PostponeUntil = PostponeUntil;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            if (BlockExecution == "True") {
                variables["Wait"] = null;
            }
        }
    }

    [DataContract]
    internal class ConvertType : IWorkflowNode {
        [DataMember]
        public string Input { get; private set; }
        [DataMember]
        public string Type { get; private set; }
        [DataMember]
        public string Result { get; private set; }

        public ConvertType(string Input, string Type, string Result) {
            this.Input = Input;
            this.Type = Type;
            this.Result = Result;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {

            switch (Type) {
                case "EntityReference":
                    if (variables[Input] is EntityReference) {
                        variables[Result] = variables[Input];
                        break;
                    }
                    throw new NotImplementedException($"Unknown input when trying to convert type {variables[Input].GetType().Name} to entityreference");
                default:
                    if (variables[Input] == null)
                    {
                        variables[Result] = null;
                        break;
                    }
                    else if (Type.ToLower() == variables[Input].GetType().Name.ToLower())
                    {
                        variables[Result] = variables[Input];
                        break;
                    }
                    throw new NotImplementedException($"Unknown input when trying to convert type {variables[Input].GetType().Name} to {Type}");
            }
        }
    }


    [DataContract]
    internal class SetEntityProperty : IWorkflowNode {
        [DataMember]
        public string Attribute { get; private set; }
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string VariableId { get; private set; }

        public SetEntityProperty(string Attribute, string ParametersId, string VariableId) {
            this.Attribute = Attribute;
            this.EntityId = ParametersId;
            this.VariableId = VariableId;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            if (!variables.ContainsKey(VariableId)) {
                Console.WriteLine($"The attribute '{Attribute}' was not created with id '{VariableId}' before being set");
                variables[VariableId] = null;
            }
            var attr = variables[VariableId];
            if (attr is Money) {
                var exchangeRate = variables["ExchangeRate"] as decimal?;
                var amount = (attr as Money).Value * exchangeRate.GetValueOrDefault(1.0m);
                attr = new Money(amount);
            }
            (variables[EntityId] as Entity).Attributes[Attribute] = attr;
        }
    }

    [DataContract]
    internal class SetAttributeValue : IWorkflowNode {
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public SetAttributeValue(string VariableId, string EntityName) {
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var relatedlinked = "relatedlinked";
            if (VariableId.Contains(relatedlinked)) {
                var reg = new Regex(@"\([^#]*\#");
                variables[relatedlinked + "_" + EntityName] = reg.Match(VariableId).Value.Replace("\"", "").Replace(relatedlinked + "_", "").TrimEdge();
                return;
            }

            if (!variables.ContainsKey(VariableId)) {
                throw new WorkflowException($"The variable with id '{VariableId}' before being set, check the workflow has the correct format.");
            }

            var entity = variables[VariableId] as Entity;
            orgService.Update(entity);

        }
    }

    [DataContract]
    internal class CreateEntity : IWorkflowNode {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public CreateEntity(string EntityId, string VariableId, string EntityName) {
            this.EntityId = EntityId;
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var entity = variables[VariableId] as Entity;
            entity.Id = EntityId == null ? Guid.NewGuid() : new Guid(EntityId);
            entity[entity.LogicalName + "id"] = entity.Id;
            orgService.Create(entity);
        }
    }

    [DataContract]
    internal class UpdateEntity : IWorkflowNode {
        [DataMember]
        public string VariableId { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }

        public UpdateEntity(string VariableId, string EntityName) {
            this.VariableId = VariableId;
            this.EntityName = EntityName;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            orgService.Update(variables[VariableId] as Entity);
        }
    }

    [DataContract]
    internal class AssignEntity : IWorkflowNode {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string OwnerId { get; private set; }

        public AssignEntity(string EntityId, string OwnerId) {
            this.EntityId = EntityId;
            this.OwnerId = OwnerId;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            if (!variables.ContainsKey(OwnerId)) {
                throw new WorkflowException($"There is no variable with the id '{OwnerId}'");
            }
            var entity = variables[EntityId] as Entity;
            var assignee = variables[OwnerId] as EntityReference;
            var req = new AssignRequest() {
                Target = entity.ToEntityReference(),
                Assignee = assignee
            };
            orgService.Execute(req);

        }
    }

    [DataContract]
    internal class SetState : IWorkflowNode {
        [DataMember]
        public string EntityId { get; private set; }
        [DataMember]
        public string EntityIdKey { get; private set; }
        [DataMember]
        public string EntityName { get; private set; }
        [DataMember]
        public OptionSetValue StateCode { get; private set; }
        [DataMember]
        public OptionSetValue StatusCode { get; private set; }

        public SetState(string EntityKey, string EntityIdKey, string EntityName, OptionSetValue StateCode, OptionSetValue StatusCode) {
            this.EntityId = EntityKey;
            this.EntityIdKey = EntityIdKey;
            this.EntityName = EntityName;
            this.StateCode = StateCode;
            this.StatusCode = StatusCode;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var entity = variables[EntityId] as Entity;
            if (entity.LogicalName != EntityName) {
                throw new WorkflowException($"primary entity has logicalname '{entity.LogicalName}' instead of '{EntityName}'");
            }

            var req = new SetStateRequest();
            req.EntityMoniker = entity.ToEntityReference();
            req.State = StateCode;
            req.Status = StatusCode;
            orgService.Execute(req);
        }
    }

    [DataContract]
    internal class ActivityList : IWorkflowNode {
        [DataMember]
        public IWorkflowNode[] Activities { get; private set; }

        public ActivityList(IWorkflowNode[] Activities) {
            this.Activities = Activities;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            Execute(0, ref variables, timeOffset, orgService, factory, trace);
        }

        public void Execute(int loopStart, ref Dictionary<string, object> variables, TimeSpan timeOffset,
             IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            for (int i = loopStart; i < Activities.Length; i++) {
                if (Activities[i] is WaitStart) {
                    var primaryEntityreference = (variables["InputEntities(\"primaryEntity\")"] as Entity).ToEntityReference();
                    variables["Wait"] = new WaitInfo(this, i, new Dictionary<string, object>(variables), primaryEntityreference);
                }
                Activities[i].Execute(ref variables, timeOffset, orgService, factory, trace);
            }
        }
    }

    [DataContract]
    internal class WaitStart : IWorkflowNode {
        public WaitStart() { }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {

        }
    }

    [DataContract]
    internal class TerminateWorkflow : IWorkflowNode {
        [DataMember]
        public OperationStatus status { get; private set; }
        [DataMember]
        public string messageId { get; private set; }

        public TerminateWorkflow(OperationStatus status, string messageId) {
            this.status = status;
            this.messageId = messageId;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var sb = new StringBuilder($"Workflow exited with status '{status}'");
            if (variables[messageId] != null && (variables[messageId] as string != "")) {
                sb.Append($", the reason was '{variables[messageId]}'");
            }
            if (status == OperationStatus.Canceled)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Canceled, variables[messageId].ToString());
            }
            else
            {
                Console.WriteLine(sb.ToString());
            }
        }
    }

    [DataContract]
    internal class RollUp : IWorkflowNode {
        [DataMember]
        public string HierarchicalRelationshipName { get; private set; }
        [DataMember]
        public string AggregateResult { get; private set; }
        [DataMember]
        public List<IWorkflowNode> Filter { get; private set; }
        [DataMember]
        public List<Entity> Filtered { get; private set; }
        [DataMember]
        public List<IWorkflowNode> Aggregation { get; private set; }
        private string FilterResult;

        public RollUp(string hierarchicalRelationshipName, string filterResult, string aggregateResult,
            List<IWorkflowNode> filter, List<IWorkflowNode> aggregation) {
            this.HierarchicalRelationshipName = hierarchicalRelationshipName;
            this.FilterResult = filterResult;
            this.AggregateResult = aggregateResult;
            this.Filter = filter;
            this.Aggregation = aggregation;
            this.Filtered = new List<Entity>();
        }


        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var primaryEntityKey = "InputEntities(\"primaryEntity\")";
            var relation = Filter.First(x => x is SetAttributeValue) as SetAttributeValue;
            relation.Execute(ref variables, timeOffset, orgService, factory, trace);
            var relatedEntities = Util.GetRelatedEntities(relation.EntityName, variables, orgService);
            foreach (var entity in relatedEntities) {
                var tmpVariables = variables;
                tmpVariables[primaryEntityKey] = entity;
                foreach (var node in Filter) {
                    node.Execute(ref tmpVariables, timeOffset, null, factory, trace);
                }
                if (FilterResult == null || (bool)tmpVariables[FilterResult]) {
                    Filtered.Add(entity);
                }
            }

            var relatedField = (Aggregation[0] as GetEntityProperty).Attribute;
            var filteredLocation = (Aggregation[0] as GetEntityProperty).VariableName;

            if (Filtered.Any(e => e.Attributes.ContainsKey(relatedField) && e.Attributes[relatedField] is Money)) {
                var targetExchangeRate = (decimal?)variables["ExchangeRate"];
                if (!targetExchangeRate.HasValue) {
                    var primary = variables[primaryEntityKey] as Entity;
                    throw new WorkflowException($"Entity with logicalname '{primary.LogicalName}' and id '{primary.Id}'" +
                        " has no transactioncurrency. Make sure to update your metadata.");
                }

                var exchangerate = "exchangerate";
                variables[filteredLocation] =
                    Filtered.Where(e => e.Attributes.ContainsKey(relatedField))
                    .Select(e => new Money(
                        (e.Attributes[relatedField] as Money).Value * (targetExchangeRate.Value / (decimal)e.Attributes[exchangerate])));
            } else {
                variables[filteredLocation] = Filtered.Select(e => e.Attributes.ContainsKey(relatedField) ? e.Attributes[relatedField] : null).ToList();
            }

            Aggregation[1].Execute(ref variables, timeOffset, orgService, factory, trace);
        }
    }

    [DataContract]
    internal class CallCodeActivity : IWorkflowNode {
        [DataMember]
        public string CodeActivityName;
        [DataMember]
        public Dictionary<string, string> inArguments;
        [DataMember]
        public Dictionary<string, string> outArguments;


        public CallCodeActivity(string CodeActivityName, Dictionary<string, string> inArguments, Dictionary<string, string> outArguments) {
            this.CodeActivityName = CodeActivityName;
            this.inArguments = inArguments;
            this.outArguments = outArguments;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace) {
            var variablesInstance = variables;
            var arguments = this.inArguments.Where(arg => !outArguments.ContainsKey(arg.Value)).ToDictionary(arg => arg.Key,
                arg => (outArguments.ContainsKey(arg.Value) ? null : variablesInstance[arg.Value]));

            var codeActivities = variables["CodeActivites"] as Dictionary<string, CodeActivity>;

            if (codeActivities==null || !codeActivities.ContainsKey(CodeActivityName))
            {
                //cannot run the code activity!
                Console.WriteLine("Cannot Execute '"+ CodeActivityName + "' workflow activity as it does not exist in the CodeActivities List. Have you registered it?");
                return;
            }
           

            var codeActivity = codeActivities[CodeActivityName];
            var primaryEntity = variables["InputEntities(\"primaryEntity\")"] as Entity;
            var workflowContext = new XrmWorkflowContext();
            workflowContext.PrimaryEntityId = primaryEntity.Id;
            workflowContext.PrimaryEntityName = primaryEntity.LogicalName;

            Console.WriteLine("Executing Workflow Activity:" + CodeActivityName + " for entity: " + primaryEntity.LogicalName);

            var invoker = new WorkflowInvoker(codeActivity);
            invoker.Extensions.Add(trace);
            invoker.Extensions.Add(workflowContext);
            invoker.Extensions.Add(factory);
            var variablesPostExecution = invoker.Invoke(arguments);
            foreach (var outArg in outArguments) {
                variables[outArg.Key] = variablesPostExecution[outArg.Value];
            }

            foreach (var outArg in outArguments.Where(arg => variablesPostExecution[arg.Value] is EntityReference)) {
                var reference = variablesPostExecution[outArg.Value] as EntityReference;
                var retrieved = orgService.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                var regex = new Regex(@"[\w]+(?=_)");
                var entityVariableName = regex.Match(outArg.Key).Value + "_entity";
                variables[$"CreatedEntities(\"{entityVariableName}\")"] = retrieved;
            }
        }
    }


    [DataContract]
    internal class SendEmail : IWorkflowNode
    {
        [DataMember]
        public string EntityId;
        [DataMember]
        public string DisplayName;
        [DataMember]
        public string Entity;

        public SendEmail(string entityId, string displayName, string entity)
        {
            EntityId = entityId;
            DisplayName = displayName;
            Entity = entity;
        }

        public void Execute(ref Dictionary<string, object> variables, TimeSpan timeOffset,
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (EntityId == "{x:Null}")
            {
                orgService.Create(variables[Entity.TrimEdge()] as Entity);
            }
        }
    }

    static class Util {
        public static DataCollection<Entity> GetRelatedEntities(string relatedEntityName, Dictionary<string, object> variables,
            IOrganizationService orgService) {
            var primaryEntityKey = "InputEntities(\"primaryEntity\")";
            var relatedlinked = "relatedlinked";
            QueryExpression query = new QueryExpression();
            query.EntityName = relatedEntityName;
            query.ColumnSet = new ColumnSet(true);
            Relationship relationship = new Relationship();
            relationship.SchemaName = variables[relatedlinked + "_" + relatedEntityName] as string;
            RelationshipQueryCollection relatedEntity = new RelationshipQueryCollection();
            relatedEntity.Add(relationship, query);
            RetrieveRequest request = new RetrieveRequest();
            request.RelatedEntitiesQuery = relatedEntity;
            request.ColumnSet = new ColumnSet();
            var primaryEntity = variables[primaryEntityKey] as Entity;
            request.Target = primaryEntity.ToEntityReference();
            RetrieveResponse response = (RetrieveResponse)orgService.Execute(request);
            return response.Entity.RelatedEntities[relationship].Entities;
        }

        public static string GetPrimaryName(EntityReference entityReference, IOrganizationService orgService)
        {
            var primaryNameAttribute = ((RetrieveEntityResponse)orgService.Execute(new RetrieveEntityRequest
            {
                LogicalName = entityReference.LogicalName,
                EntityFilters = EntityFilters.Entity
            })).EntityMetadata.PrimaryNameAttribute;
            var referencedEntity = orgService.Retrieve(entityReference.LogicalName, entityReference.Id, new ColumnSet(primaryNameAttribute));
            return referencedEntity.GetAttributeValue<string>(primaryNameAttribute);
        }

        public static string GetOptionSetValueLabel(string entityLogicalName, string attributeLogicalName, OptionSetValue optionSetValue, IOrganizationService orgService)
        {
            var optionSetMetadata = ((PicklistAttributeMetadata)((RetrieveAttributeResponse)orgService.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName
            })).AttributeMetadata).OptionSet;

            var option = optionSetMetadata.Options.SingleOrDefault(x => x.Value == optionSetValue.Value);
            return option.Label.UserLocalizedLabel.Label;
        }

        public static string GetBooleanLabel(string entityLogicalName, string attributeLogicalName, bool value, IOrganizationService orgService)
        {
            var booleanMetadata = ((BooleanAttributeMetadata)((RetrieveAttributeResponse)orgService.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName
            })).AttributeMetadata).OptionSet;
            var option = value ? booleanMetadata.TrueOption : booleanMetadata.FalseOption;
            return option.Label.UserLocalizedLabel.Label;
        }

        public static XrmWorkflowContext GetDefaultContext() {
            var userId = Guid.NewGuid();
            return new XrmWorkflowContext() {
                Depth = 1,
                IsExecutingOffline = false,
                MessageName = "Create",
                UserId = userId,
                InitiatingUserId = userId,
                InputParameters = new ParameterCollection(),
                OutputParameters = new ParameterCollection(),
                SharedVariables = new ParameterCollection(),
                PreEntityImages = new EntityImageCollection(),
                PostEntityImages = new EntityImageCollection()
            };
        }
    }


}
