using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
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
        };

        public static Dictionary<Type, string> RequestToEventOperation = new Dictionary<Type, string>()
        {
            { typeof(AssignRequest), nameof(EventOperation.Assign).ToLower() },
            { typeof(AssociateRequest), nameof(EventOperation.Associate).ToLower() },
            { typeof(CreateRequest),nameof( EventOperation.Create).ToLower() },
            { typeof(DeleteRequest), nameof(EventOperation.Delete).ToLower() },
            { typeof(DisassociateRequest), nameof(EventOperation.Disassociate).ToLower() },
            { typeof(GrantAccessRequest), nameof(EventOperation.GrantAccess).ToLower() },
            { typeof(MergeRequest), nameof(EventOperation.Merge).ToLower() },
            { typeof(ModifyAccessRequest), nameof(EventOperation.ModifyAccess).ToLower() },
            { typeof(RetrieveRequest), nameof(EventOperation.Retrieve).ToLower() },
            { typeof(RetrieveMultipleRequest), nameof(EventOperation.RetrieveMultiple).ToLower() },
            { typeof(RetrievePrincipalAccessRequest), nameof(EventOperation.RetrievePrincipalAccess).ToLower() },
            //{ typeof(RetrieveSharedPrincipalAccessRequest), EventOperation.RetrieveSharedPrincipalAccess }, // No such request
            { typeof(RevokeAccessRequest), nameof(EventOperation.RevokeAccess).ToLower() },
            { typeof(SetStateRequest), nameof(EventOperation.SetState).ToLower() },
            //{ typeof(SetStateDynamicEntityRequest), EventOperation.SetStateDynamicEntity }, // No such request
            { typeof(UpdateRequest), nameof(EventOperation.Update).ToLower() },
            { typeof(WinOpportunityRequest), nameof(EventOperation.Win).ToLower() },
            { typeof(LoseOpportunityRequest), nameof(EventOperation.Lose).ToLower() },
            { typeof(CloseIncidentRequest), nameof(EventOperation.Close).ToLower() },


        };

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
