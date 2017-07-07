using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.Reflection;
using Microsoft.Xrm.Sdk.Client;
using System.Globalization;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk.Metadata;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using System.IO;

namespace DG.Tools {
    internal static class Utility {

        public static Entity CloneEntity(this Entity entity) {
            if (entity == null) return null;
            return CloneEntity(entity, null, null);
        }

        public static Entity CloneEntity(this Entity entity, EntityMetadata metadata, ColumnSet cols) {
            var clone = new Entity(entity.LogicalName);
            clone.Id = entity.Id;
            if (metadata?.PrimaryIdAttribute != null) {
                clone[metadata.PrimaryIdAttribute] = entity.Id;
            }
            clone.EntityState = entity.EntityState;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            clone.KeyAttributes = entity.KeyAttributes;
#endif
            return clone.SetAttributes(entity.Attributes, metadata, cols);
        }

        private static bool IsSettableAttribute(string attrName, EntityMetadata metadata) {
            if (metadata == null || attrName.Contains(".")) {
                return true;
            }
            return metadata.Attributes.Any(a => a.LogicalName == attrName);
        }

        public static T SetAttributes<T>(this T entity, AttributeCollection attributes, EntityMetadata metadata) where T : Entity {
            return SetAttributes(entity, attributes, metadata, null);
        }

        public static T SetAttributes<T>(this T entity, AttributeCollection attributes, EntityMetadata metadata,
            ColumnSet colsToKeep) where T : Entity {

            if (colsToKeep != null && !colsToKeep.AllColumns) {
                HashSet<string> keep = new HashSet<string>(colsToKeep.Columns);
                foreach (var attr in attributes) {
                    if (!IsSettableAttribute(attr.Key, metadata)) {
                        throw new FaultException($"'{entity.LogicalName}' entity doesn't contain attribute with Name = '{attr.Key}'");
                    }
                    if (keep.Contains(attr.Key)) entity.Attributes[attr.Key] = attr.Value;
                }
            } else {
                foreach (var attr in attributes) {
                    if (!IsSettableAttribute(attr.Key, metadata)) {
                        throw new FaultException($"'{entity.LogicalName}' entity doesn't contain attribute with Name = '{attr.Key}'");
                    }
                    entity.Attributes[attr.Key] = attr.Value;
                }
            }
            return entity;
        }


        public static EntityMetadata GetMetadata(this Dictionary<string, EntityMetadata> metadata, string logicalName) {
            if (!metadata.ContainsKey(logicalName)) {
                throw new FaultException($"Couldn't find metadata for the logicalname '{logicalName}'. Run GenerateMetadata.cmd again, and check that the logicalname is specified in the config file.");
            }
            return metadata[logicalName];
        }

        /*
         * Possible error with cast to Entity here, was (T) before
         * 
         * */
        public static Entity MakeProxyTypeEntity(Entity entity, Type ty) {
            var method = typeof(Entity).GetMethod("ToEntity").MakeGenericMethod(ty);
            return (Entity)method.Invoke(entity, null);
        }


        public static bool MatchesCriteria(Entity entity, FilterExpression criteria) {
            if (criteria.FilterOperator == LogicalOperator.And)
                return criteria.Filters.All(f => MatchesCriteria(entity, f)) && criteria.Conditions.All(c => EvaluateCondition(entity, c));
            else
                return criteria.Filters.Any(f => MatchesCriteria(entity, f)) || criteria.Conditions.Any(c => EvaluateCondition(entity, c));
        }

        private static bool EvaluateCondition(Entity entity, ConditionExpression condition) {
            if (condition.AttributeName == null) {
                return Matches(entity.Id, condition.Operator, condition.Values);
            }

            if (!entity.Attributes.ContainsKey(condition.AttributeName)) {
                if (condition.Operator != ConditionOperator.Null)
                    return false;
                return true;
            }
            var attr = ConvertToComparableObject(entity.Attributes[condition.AttributeName]);
            var values = condition.Values.Select(v => ConvertToComparableObject(v));
            return Matches(attr, condition.Operator, values);
        }

        public static object ConvertToComparableObject(object obj) {
            object res = null;
            var entityReference = obj as EntityReference;
            var optionSetValue = obj as OptionSetValue;
            var money = obj as Money;
            var aliasedValue = obj as AliasedValue;
            if (obj is DateTime)
                res = (object)((DateTime)obj).ToString("u", (IFormatProvider)CultureInfo.InvariantCulture);
            else if (entityReference != null)
                res = (object)entityReference.Id;
            else if (money != null)
                res = (object)money.Value;
            else if (aliasedValue != null)
                res = (object)aliasedValue.Value;
            else if (optionSetValue != null)
                res = (object)optionSetValue.Value;
            else if (obj != null && obj.GetType().IsEnum)
                res = (object)(int)obj;
            else
                res = obj;
            return res;
        }

        public static bool Matches(object attr, ConditionOperator op, IEnumerable<object> values) {
            if (attr == null && op != ConditionOperator.Null && op != ConditionOperator.NotNull) return false;
            switch (op) {
                case ConditionOperator.Null:
                    return attr == null;
                case ConditionOperator.NotNull:
                    return attr != null;
                case ConditionOperator.Equal:
                    return attr.Equals(values.First());
                case ConditionOperator.NotEqual:
                    return !attr.Equals(values.First());

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterEqual:
                case ConditionOperator.LessEqual:
                case ConditionOperator.LessThan:
                    return Compare((IComparable)attr, op, (IComparable)values.First());
                case ConditionOperator.Like:
                    var sAttr = (string)attr;
                    var pattern = (string)values.First();
                    if (pattern.First() == '%' && (pattern.Last() == '%')) {
                        return sAttr.Contains(pattern.Substring(1, pattern.Length - 1));
                    } else if (pattern.First() == '%') {
                        return sAttr.EndsWith(pattern.Substring(1));
                    } else if (pattern.Last() == '%') {
                        return sAttr.StartsWith(pattern.Substring(0, pattern.Length - 1));
                    } else {
                        throw new NotImplementedException(
                            String.Format("The like matching for '{0}' has not been implemented yet", pattern));
                    }
                case ConditionOperator.NextXYears:
                    var now = DateTime.Now;
                    DateTime date;
                    if (attr is DateTime) {
                        date = (DateTime)attr;
                    } else {
                        date = DateTime.Parse((string)attr);
                    }
                    var x = int.Parse((string)values.First());
                    return now.Date <= date.Date && date.Date <= now.AddYears(x).Date;

                default:
                    throw new NotImplementedException(
                        String.Format("The ConditionOperator '{0}' has not been implemented yet.", op.ToString()));
            }

        }

        public static bool Compare(IComparable attr, ConditionOperator op, IComparable value) {
            switch (op) {
                case ConditionOperator.GreaterEqual:
                    return attr.CompareTo(value) >= 0;
                case ConditionOperator.GreaterThan:
                    return attr.CompareTo(value) > 0;
                case ConditionOperator.LessEqual:
                    return attr.CompareTo(value) <= 0;
                case ConditionOperator.LessThan:
                    return attr.CompareTo(value) < 0;

                default:
                    throw new MockupException("Invalid state.");
            }

        }

        public static IEnumerable<KeyValuePair<string, object>> GetProperties(object obj) {
            var ty = obj.GetType();
            var props = ty.GetProperties().Where(x => x.DeclaringType.UnderlyingSystemType == ty).ToList();
            return props.Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(obj)));
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException e) {
                return e.Types.Where(t => t != null);
            }
        }
        public static T? GetOptionSetValue<T>(this Entity entity, string attributeName) where T : struct, IComparable, IConvertible, IFormattable {
            var optionSet = entity.GetAttributeValue<OptionSetValue>(attributeName);
            if (optionSet != null) {
                return (T)Enum.ToObject(typeof(T), optionSet.Value);
            } else {
                return null;
            }
        }
        
        public static List<AccessRights> GetAccessRights(this AccessRights mask) {
            var rights = new List<AccessRights>();
            foreach (AccessRights right in Enum.GetValues(typeof(AccessRights))) {
                if (right == AccessRights.None) continue;
                if (mask.HasFlag(right)) {
                    rights.Add(right);
                }
            }
            return rights;
        }


        internal static MetadataSkeleton GetMetadata(string prefix) {
            var pathToMetadata = Path.Combine(prefix, "Metadata.xml");
            if (!File.Exists(pathToMetadata)) {
                throw new ArgumentException($"Could not find metadata file at '{pathToMetadata}'."+
                    " Be sure to run Metadata/GetMetadata.cmd to generate it after setting it up in Metadata/Config.fsx.");
            }
            var serializer = new DataContractSerializer(typeof(MetadataSkeleton));
            using (var stream = new FileStream(pathToMetadata, FileMode.Open)) {
                return (MetadataSkeleton)serializer.ReadObject(stream);
            }
        }

        internal static List<Entity> GetWorkflows(string prefix) {
            var pathToWorkflows = Path.Combine(prefix, "Workflows");
            var files = Directory.GetFiles(pathToWorkflows, "*.xml");
            var workflows = new List<Entity>();
            foreach (var file in files) {
                workflows.Add(GetWorkflow(file));
            }
            return workflows;
        }

        internal static Entity GetWorkflow(string path) {
            var serializer = new DataContractSerializer(typeof(Entity));
            using (var stream = new FileStream(path, FileMode.Open)) {
                return (Entity)serializer.ReadObject(stream);
            }
        }

        internal static List<SecurityRole> GetSecurityRoles(string prefix) {
            var pathToSecurity = Path.Combine(prefix, "SecurityRoles");
            var files = Directory.GetFiles(pathToSecurity, "*.xml");
            var securityRoles = new List<SecurityRole>();

            var serializer = new DataContractSerializer(typeof(SecurityRole));
            foreach (var file in files) {
                using (var stream = new FileStream(file, FileMode.Open)) {
                    securityRoles.Add((SecurityRole)serializer.ReadObject(stream));
                }
            }
            return securityRoles;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal static EntityReference ToEntityReferenceWithKeyAttributes(this Entity entity) {
            var reference = entity.ToEntityReference();
            reference.KeyAttributes = entity.KeyAttributes;
            return reference;
        }
#endif


        #region ConditionOperator Map

        public static readonly Dictionary<string, ConditionOperator> ConditionOperators = new Dictionary<string, ConditionOperator>
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

#endregion






    }

    public class MockupException : Exception {
        public MockupException(string message) : base(message) { }
        public MockupException(string message, params object[] args) : base(String.Format(message, args)) { }
    }

    public class LogicalNames {
        public const string TransactionCurrency = "transactioncurrency";
        public const string BusinessUnit = "businessunit";
        public const string SystemUser = "systemuser";
        public const string Workflow = "workflow";
        public const string Team = "team";
        public const string Contact = "contact";
        public const string Lead = "lead";
        public const string Opportunity = "opportunity";
    }

    [DataContract()]
    public enum workflow_runas {
        
        [EnumMember()]
        Owner = 0,
        
        [EnumMember()]
        CallingUser = 1,
    }
    
    [DataContract()]
    public enum workflow_stage {
        
        [EnumMember()]
        Preoperation = 20,
        
        [EnumMember()]
        Postoperation = 40,
    }
    
    [DataContract()]
    public enum Workflow_Type {
        
        [EnumMember()]
        Definition = 1,
        
        [EnumMember()]
        Activation = 2,
        
        [EnumMember()]
        Template = 3,
    }
    
    [DataContract()]
    public enum componentstate {
        
        [EnumMember()]
        Published = 0,
        
        [EnumMember()]
        Unpublished = 1,
        
        [EnumMember()]
        Deleted = 2,
        
        [EnumMember()]
        DeletedUnpublished = 3,
    }
    
    [DataContract()]
    public enum Workflow_Scope {
        
        [EnumMember()]
        User = 1,
        
        [EnumMember()]
        BusinessUnit = 2,
        
        [EnumMember()]
        ParentChildBusinessUnits = 3,
        
        [EnumMember()]
        Organization = 4,
    }
    
    [DataContract()]
    public enum Workflow_Mode {
        
        [EnumMember()]
        Background = 0,
        
        [EnumMember()]
        Realtime = 1,
    }
    
    [DataContract()]
    public enum Workflow_BusinessProcessType {
        
        [EnumMember()]
        BusinessFlow = 0,
        
        [EnumMember()]
        TaskFlow = 1,
    }
    
    [DataContract()]
    public enum Workflow_Category {
        
        [EnumMember()]
        Workflow = 0,
        
        [EnumMember()]
        Dialog = 1,
        
        [EnumMember()]
        BusinessRule = 2,
        
        [EnumMember()]
        Action = 3,
        
        [EnumMember()]
        BusinessProcessFlow = 4,
    }
    
    [DataContract()]
    public enum WorkflowState {
        
        [EnumMember()]
        Draft = 0,
        
        [EnumMember()]
        Activated = 1,
    }
    
    [DataContract()]
    public enum Workflow_StatusCode {
        
        [EnumMember()]
        Draft = 1,
        
        [EnumMember()]
        Activated = 2,
    }

    [DataContract()]
    public enum OpportunityState {

        [EnumMember()]
        Open = 0,

        [EnumMember()]
        Won = 1,

        [EnumMember()]
        Lost = 2,
    }

    [DataContract()]
    public enum Opportunity_StatusCode {

        [EnumMember()]
        InProgress = 1,

        [EnumMember()]
        OnHold = 2,

        [EnumMember()]
        Won = 3,

        [EnumMember()]
        Canceled = 4,

        [EnumMember()]
        OutSold = 5,
    }

}
