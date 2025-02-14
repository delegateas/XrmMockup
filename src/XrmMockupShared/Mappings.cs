using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DG.Tools.XrmMockup {

    internal static partial class Mappings {

        public static Dictionary<Type, string> EntityImageProperty = new Dictionary<Type, string>()
        {
            { typeof(AssignRequest), "Target" },
            { typeof(CreateRequest), "Target" },
            { typeof(DeleteRequest), "Target" },
            { typeof(DeliverIncomingEmailRequest), "EmailId" },
            { typeof(DeliverPromoteEmailRequest), "EmailId" },
            { typeof(ExecuteWorkflowRequest), "Target" },
            { typeof(MergeRequest), "Target" },
            { typeof(SendEmailRequest), "EmailId" },
            { typeof(SetStateRequest), "EntityMoniker" },
            { typeof(UpdateRequest), "Target" },
            { typeof(AssociateRequest), "Target" },
            { typeof(DisassociateRequest), "Target" },
            { typeof(RetrieveRequest), "Target" },
            { typeof(CreateMultipleRequest), "Targets" },
            { typeof(UpdateMultipleRequest), "Targets" },
        };

        public static Dictionary<Type, EventOperation?> RequestToEventOperation = new Dictionary<Type, EventOperation?>()
        {
            { typeof(AssignRequest), EventOperation.Assign },
            { typeof(AssociateRequest), EventOperation.Associate },
            { typeof(CreateRequest), EventOperation.Create },
            { typeof(DeleteRequest), EventOperation.Delete },
            { typeof(DisassociateRequest), EventOperation.Disassociate },
            { typeof(GrantAccessRequest), EventOperation.GrantAccess },
            { typeof(MergeRequest), EventOperation.Merge },
            { typeof(ModifyAccessRequest), EventOperation.ModifyAccess },
            { typeof(RetrieveRequest), EventOperation.Retrieve },
            { typeof(RetrieveMultipleRequest), EventOperation.RetrieveMultiple },
            { typeof(RetrievePrincipalAccessRequest), EventOperation.RetrievePrincipalAccess },
            //{ typeof(RetrieveSharedPrincipalAccessRequest), EventOperation.RetrieveSharedPrincipalAccess }, // No such request
            { typeof(RevokeAccessRequest), EventOperation.RevokeAccess },
            { typeof(SetStateRequest), EventOperation.SetState },
            //{ typeof(SetStateDynamicEntityRequest), EventOperation.SetStateDynamicEntity }, // No such request
            { typeof(UpdateRequest), EventOperation.Update },
            { typeof(WinOpportunityRequest), EventOperation.Win },
            { typeof(LoseOpportunityRequest), EventOperation.Lose },
            { typeof(CloseIncidentRequest), EventOperation.Close },
            { typeof(CreateMultipleRequest), EventOperation.CreateMultiple },
            { typeof(UpdateMultipleRequest), EventOperation.UpdateMultiple }
        };

        public static Dictionary<EventOperation, EventOperation> RequestToMultipleRequest = new Dictionary<EventOperation, EventOperation>()
        {
            { EventOperation.Create, EventOperation.CreateMultiple },
            { EventOperation.Update, EventOperation.UpdateMultiple }
        };

        public static EventOperation? SingleOperationFromMultiple(EventOperation operation)
        {
            if (!RequestToMultipleRequest.ContainsValue(operation)) return null;
            return RequestToMultipleRequest.First(x => x.Value == operation).Key;
        }

        public static Type EventOperationToRequest(EventOperation operation)
        {
            if (!RequestToEventOperation.ContainsValue(operation)) return null;
            return RequestToEventOperation.First(x => x.Value == operation).Key;
        }

        public static readonly Dictionary<string, ConditionOperator> ConditionalOperator = new Dictionary<string, ConditionOperator>
        {
            { "between", ConditionOperator.Between },
            { "eq", ConditionOperator.Equal },
            { "eq-businessid", ConditionOperator.EqualBusinessId },
            { "eq-userid", ConditionOperator.EqualUserId },
            { "eq-userteams", ConditionOperator.EqualUserTeams },
            { "ge", ConditionOperator.GreaterEqual },
            { "gt", ConditionOperator.GreaterThan },
            { "in", ConditionOperator.In },
            { "in-fiscal-period", ConditionOperator.InFiscalPeriod },
            { "in-fiscal-period-and-year", ConditionOperator.InFiscalPeriodAndYear },
            { "in-fiscal-year", ConditionOperator.InFiscalYear },
            { "in-or-after-fiscal-period-and-year", ConditionOperator.InOrAfterFiscalPeriodAndYear },
            { "in-or-before-fiscal-period-and-year", ConditionOperator.InOrBeforeFiscalPeriodAndYear },
            { "last-seven-days", ConditionOperator.Last7Days },
            { "last-fiscal-period", ConditionOperator.LastFiscalPeriod },
            { "last-fiscal-year", ConditionOperator.LastFiscalYear },
            { "last-month", ConditionOperator.LastMonth },
            { "last-week", ConditionOperator.LastWeek },
            { "last-x-days", ConditionOperator.LastXDays },
            { "last-x-fiscal-periods", ConditionOperator.LastXFiscalPeriods },
            { "last-x-fiscal-years", ConditionOperator.LastXFiscalYears },
            { "last-x-hours", ConditionOperator.LastXHours },
            { "last-x-months", ConditionOperator.LastXMonths },
            { "last-x-weeks", ConditionOperator.LastXWeeks },
            { "last-x-years", ConditionOperator.LastXYears },
            { "last-year", ConditionOperator.LastYear },
            { "le", ConditionOperator.LessEqual },
            { "lt", ConditionOperator.LessThan },
            { "next-seven-days", ConditionOperator.Next7Days },
            { "next-fiscal-period", ConditionOperator.NextFiscalPeriod },
            { "next-fiscal-year", ConditionOperator.NextFiscalYear },
            { "next-month", ConditionOperator.NextMonth },
            { "next-week", ConditionOperator.NextWeek },
            { "next-x-days", ConditionOperator.NextXDays },
            { "next-x-fiscal-periods", ConditionOperator.NextXFiscalPeriods },
            { "next-x-fiscal-years", ConditionOperator.NextXFiscalYears },
            { "next-x-hours", ConditionOperator.NextXHours },
            { "next-x-months", ConditionOperator.NextXMonths },
            { "next-x-weeks", ConditionOperator.NextXWeeks },
            { "next-x-years", ConditionOperator.NextXYears },
            { "next-year", ConditionOperator.NextYear },
            { "not-between", ConditionOperator.NotBetween },
            //{ "ne", ConditionOperator.NotEqual },
            { "ne-businessid", ConditionOperator.NotEqualBusinessId },
            { "ne-userid", ConditionOperator.NotEqualUserId },
            { "not-in", ConditionOperator.NotIn },
            { "not-null", ConditionOperator.NotNull },
            //{ "ne", ConditionOperator.NotOn },
            { "null", ConditionOperator.Null },
            { "olderthan-x-months", ConditionOperator.OlderThanXMonths },
            { "on", ConditionOperator.On },
            { "on-or-after", ConditionOperator.OnOrAfter },
            { "on-or-before", ConditionOperator.OnOrBefore },
            { "this-fiscal-period", ConditionOperator.ThisFiscalPeriod },
            { "this-fiscal-year", ConditionOperator.ThisFiscalYear },
            { "this-month", ConditionOperator.ThisMonth },
            { "this-week", ConditionOperator.ThisWeek },
            { "this-year", ConditionOperator.ThisYear },
            { "today", ConditionOperator.Today },
            { "tomorrow", ConditionOperator.Tomorrow },
            { "yesterday", ConditionOperator.Yesterday }
        };


        public static EntityReference GetPrimaryEntityReferenceFromRequest(OrganizationRequest request)
        {
            switch (request)
            {
                case RetrieveRequest retrieveRequest:
                    return retrieveRequest.Target;
                case CreateRequest createRequest:
                    return createRequest.Target.ToEntityReferenceWithKeyAttributes();
                case UpdateRequest updateRequest:
                    return updateRequest.Target.ToEntityReferenceWithKeyAttributes();
                case DeleteRequest deleteRequest:
                    return deleteRequest.Target;
                case SetStateRequest setStateRequest:
                    return setStateRequest.EntityMoniker;
                case AssignRequest assignRequest:
                    return assignRequest.Target;
                case AssociateRequest associateRequest:
                    return associateRequest.Target;
                case DisassociateRequest disassociateRequest:
                    return disassociateRequest.Target;
                case MergeRequest mergeRequest:
                    return mergeRequest.Target;
                case WinOpportunityRequest winOpportunityRequest:
                    return winOpportunityRequest.OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
                case LoseOpportunityRequest loseOpportunityRequest:
                    return loseOpportunityRequest.OpportunityClose.GetAttributeValue<EntityReference>("opportunityid");
                case RetrieveMultipleRequest retrieveMultipleRequest:
                    return GetPrimaryEntityReferenceFromQuery(retrieveMultipleRequest.Query);
                case CloseIncidentRequest closeIncidentRequest:
                    return closeIncidentRequest.IncidentResolution?.GetAttributeValue<EntityReference>("incidentid");
                case CreateMultipleRequest createMultipleRequest:
                    return new EntityReference(createMultipleRequest.Targets.EntityName, Guid.Empty);
                case UpdateMultipleRequest updateMultipleRequest:
                    return new EntityReference(updateMultipleRequest.Targets.EntityName, Guid.Empty);
            }

            return null;
        }

        public static EntityReference GetPrimaryEntityReferenceFromQuery(QueryBase query)
        {
            switch (query)
            {
                case FetchExpression fe: return new EntityReference(XmlHandling.FetchXmlToQueryExpression(fe.Query).EntityName, Guid.Empty);
                case QueryExpression qe: return new EntityReference(qe.EntityName, Guid.Empty);
                case QueryByAttribute qba: return new EntityReference(qba.EntityName, Guid.Empty);
                default: return null;
            }
        }
    }
}
