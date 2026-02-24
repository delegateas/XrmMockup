using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Activities;
using DG.Tools.XrmMockup.Internal;

namespace WorkflowExecuter
{
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
    internal class WorkflowTree
    {
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


        public WorkflowTree(IWorkflowNode StartActivity) : this(StartActivity, false, false, new HashSet<string>(),
            workflow_runas.CallingUser, Workflow_Scope.User, workflow_stage.Postoperation, workflow_stage.Postoperation,
            workflow_stage.Postoperation, Workflow_Mode.Realtime, null, "", new Dictionary<string, CodeActivity>(),
            new List<WorkflowArgument>(), new List<WorkflowArgument>())
        { }

        public WorkflowTree(IWorkflowNode StartActivity, bool? TriggerOnCreate, bool? TriggerOnDelete,
            HashSet<string> TriggerFieldsChange, workflow_runas? RunAs, Workflow_Scope? Scope,
            workflow_stage? CreateStage, workflow_stage? UpdateStage, workflow_stage? DeleteStage, Workflow_Mode? Mode,
            Guid? Owner, string PrimaryEntityLogicalname, Dictionary<string, CodeActivity> CodeActivites,
            List<WorkflowArgument> Input, List<WorkflowArgument> Output)
        {
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
            IOrganizationService orgService, IOrganizationServiceFactory factory, ITracingService trace)
        {
            if (primaryEntity.Id == Guid.Empty)
            {
                throw new WorkflowException("The primary entity must have an id");
            }
            Reset();
            Variables["InputEntities(\"primaryEntity\")"] = primaryEntity;
            Variables["ExecutionTime"] = DateTime.Now.Add(timeOffset);
            var transactioncurrencyid = "transactioncurrencyid";
            if (primaryEntity.Attributes.ContainsKey(transactioncurrencyid))
            {
                var currencyRef = primaryEntity.GetAttributeValue<EntityReference>(transactioncurrencyid);
                var exchangerate = "exchangerate";
                var currency = orgService.Retrieve("transactioncurrency", currencyRef.Id, new ColumnSet(exchangerate));
                Variables["ExchangeRate"] = currency[exchangerate];
            }
            StartActivity.Execute(ref Variables, timeOffset, orgService, factory, trace);
            return this;
        }

        internal void Reset()
        {
            Variables["True"] = true;
            Variables["False"] = false;
            Variables["ExchangeRate"] = null;
            Variables["CodeActivites"] = CodeActivites;
            Variables["InputEntities(\"primaryEntity\")"] = null;
            Variables["Wait"] = null;
        }

        internal void HardReset()
        {
            Variables = new Dictionary<string, object>();
            Reset();
        }
    }
}
