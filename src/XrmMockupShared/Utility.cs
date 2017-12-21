﻿using System;
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
using DG.Tools.XrmMockup.Database;
using System.Xml.Linq;

namespace DG.Tools.XrmMockup {
    internal static class Utility {

        internal static HashSet<string> Activities = new HashSet<string>() {
            "appointment",
            "email",
            "fax",
            "incidentresolution",
            "letter",
            "opportunityclose",
            "salesorderclose",
            "phonecall",
            "quoteclose",
            "task",
            "serviceappointment",
            "campaignresponse",
            "campaignactivity",
            "bulkoperation"
        };

        internal static Dictionary<string, OptionSetValue> ActivityTypeCode = new Dictionary<string, OptionSetValue>() {
            { "appointment", new OptionSetValue(4201) },
            { "email", new OptionSetValue(4204) },
            { "fax", new OptionSetValue(4206) },
            { "incidentresolution", new OptionSetValue(4207) },
            { "letter", new OptionSetValue(4208) },
            { "opportunityclose", new OptionSetValue(4209) },
            { "salesorderclose", new OptionSetValue(4210) },
            { "phonecall", new OptionSetValue(4211) },
            { "quoteclose", new OptionSetValue(4212) },
            { "task", new OptionSetValue(4214) },
            { "serviceappointment", new OptionSetValue(4251) },
            { "campaignresponse", new OptionSetValue(4401) },
            { "campaignactivity", new OptionSetValue(4402) },
            { "bulkoperation", new OptionSetValue(4406) },
        };

        public static Entity CloneEntity(this Entity entity) {
            if (entity == null) return null;
            return CloneEntity(entity, null, null);
        }

        public static Entity CloneEntity(this Entity entity, EntityMetadata metadata, ColumnSet cols) {
            var clone = new Entity(entity.LogicalName) {
                Id = entity.Id
            };

            if (metadata?.PrimaryIdAttribute != null) {
                clone[metadata.PrimaryIdAttribute] = entity.Id;
            }
            clone.EntityState = entity.EntityState;

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            clone.KeyAttributes = entity.KeyAttributes;
#endif

            return clone.SetAttributes(entity.Attributes, metadata, cols);
        }

        public static bool IsSettableAttribute(string attrName, EntityMetadata metadata) {
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

        internal static void RemoveUnsettableAttributes(string actionType, EntityMetadata metadata, Entity entity) {
            if (entity == null) return;

            switch (actionType) {
                case "Create":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForCreate == false)).Select(a => a.Key).ToArray());
                    break;
                case "Retrieve":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForRead == false)).Select(a => a.Key).ToArray());
                    break;
                case "Update":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForUpdate == false)).Select(a => a.Key).ToArray());
                    break;
            }
        }

        internal static void RemoveAttribute(Entity entity, params string[] attrNames) {
            foreach (var attrName in attrNames) {
                if (entity.Attributes.ContainsKey(attrName)) entity.Attributes.Remove(attrName);
            }
        }

        internal static void CloseOpportunity(Core core, OpportunityState state, OptionSetValue status, Entity opportunityClose, EntityReference userRef) {
            var setStateHandler = core.RequestHandlers.Find(x => x is SetStateRequestHandler);
            var req = new SetStateRequest() {
                EntityMoniker = opportunityClose.GetAttributeValue<EntityReference>("opportunityid"),
                State = new OptionSetValue((int)state),
                Status = status
            };
            setStateHandler.Execute(req, userRef);

            var create = new CreateRequest {
                Target = opportunityClose
            };
            core.Execute(create as OrganizationRequest, userRef);
        }


        public static EntityMetadata GetMetadata(this Dictionary<string, EntityMetadata> metadata, string logicalName) {
            if (!metadata.ContainsKey(logicalName)) {
                throw new FaultException($"Couldn't find metadata for the logicalname '{logicalName}'. Run the MetadataGenerator again, and check that the logicalname is specified in the config file.");
            }
            return metadata[logicalName];
        }

        internal static void SetFullName(MetadataSkeleton metadata, Entity dbEntity) {
            var fullnameFormat =
                (FullNameConventionCode)metadata.BaseOrganization.GetAttributeValue<OptionSetValue>("fullnameconventioncode").Value;
            var first = dbEntity.GetAttributeValue<string>("firstname");
            if (first == null) first = "";
            var middle = dbEntity.GetAttributeValue<string>("middlename");
            if (middle == null) middle = "";
            var last = dbEntity.GetAttributeValue<string>("lastname");
            if (last == null) last = "";
            switch (fullnameFormat) {
                case FullNameConventionCode.FirstLast:
                    dbEntity["fullname"] = first != "" ? first + " " + last : last;
                    break;
                case FullNameConventionCode.LastFirst:
                    dbEntity["fullname"] = first != "" ? last + ", " + first : last;
                    break;
                case FullNameConventionCode.LastNoSpaceFirst:
                    dbEntity["fullname"] = first != "" ? last + first : last;
                    break;
                case FullNameConventionCode.LastSpaceFirst:
                    dbEntity["fullname"] = first != "" ? last + " " + first : last;
                    break;
                case FullNameConventionCode.FirstMiddleLast:
                    dbEntity["fullname"] = first;
                    if (middle != "") dbEntity["fullname"] += " " + middle;
                    dbEntity["fullname"] += (string)dbEntity["fullname"] != "" ? " " + last : last;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.FirstMiddleInitialLast:
                    dbEntity["fullname"] = first;
                    if (middle != "") dbEntity["fullname"] += " " + middle[0] + ".";
                    dbEntity["fullname"] += (string)dbEntity["fullname"] != "" ? " " + last : last;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.LastFirstMiddle:
                    dbEntity["fullname"] = last;
                    if (first != "") dbEntity["fullname"] += ", " + first;
                    if (middle != "") dbEntity["fullname"] += (string)dbEntity["fullname"] == last ? ", " + middle : " " + middle;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.LastFirstMiddleInitial:
                    dbEntity["fullname"] = last;
                    if (first != "") dbEntity["fullname"] += ", " + first;
                    if (middle != "") dbEntity["fullname"] +=
                            (string)dbEntity["fullname"] == last ? ", " + middle[0] + "." : " " + middle[0] + ".";
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;

            }
            if (dbEntity["fullname"] != null) {
                (dbEntity["fullname"] as string).TrimStart().TrimEnd();
            }
        }

        internal static RelationshipMetadataBase GetRelationshipMetadataDefaultNull(Dictionary<string, EntityMetadata> entityMetadata, string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                return null;
            }
            RelationshipMetadataBase relationshipBase;
            foreach (var meta in entityMetadata) {
                relationshipBase = meta.Value.ManyToManyRelationships.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                relationshipBase = meta.Value.ManyToManyRelationships.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                var oneToManyBases = meta.Value.ManyToOneRelationships.Concat(meta.Value.OneToManyRelationships);
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
            }
            return null;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        internal static void CheckStatusTransitions(EntityMetadata metadata, Entity newEntity, Entity prevEntity) {
            if (newEntity == null || prevEntity == null) return;
            if (!newEntity.Attributes.ContainsKey("statuscode") || !prevEntity.Attributes.ContainsKey("statuscode")) return;
            if (newEntity.LogicalName != prevEntity.LogicalName || newEntity.Id != prevEntity.Id) return;

            var newValue = newEntity["statuscode"] as OptionSetValue;
            var prevValue = prevEntity["statuscode"] as OptionSetValue;

            if (metadata.EnforceStateTransitions != true) return;

            var optionsMeta = (metadata.Attributes
                .FirstOrDefault(a => a is StatusAttributeMetadata) as StatusAttributeMetadata)
                .OptionSet.Options;
            if (!optionsMeta.Any(o => o.Value == newValue.Value)) return;

            var prevValueOptionMeta = optionsMeta.FirstOrDefault(o => o.Value == prevValue.Value) as StatusOptionMetadata;
            if (prevValueOptionMeta == null) return;

            var transitions = prevValueOptionMeta.TransitionData;
            if (transitions != null && transitions != "") {
                var ns = XNamespace.Get("http://schemas.microsoft.com/crm/2009/WebServices");
                var doc = XDocument.Parse(transitions).Element(ns + "allowedtransitions");
                if (doc.Descendants(ns + "allowedtransition")
                    .Where(x => x.Attribute("tostatusid").Value == newValue.Value.ToString())
                    .Any()) {
                    return;
                }
            }
            throw new FaultException($"Trying to switch {newEntity.LogicalName} from status {prevValue.Value} to {newValue.Value}");
        }
#endif

        internal static EntityReference GetBaseCurrency(MetadataSkeleton metadata) {
            return metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
        }

        private static int GetBaseCurrencyPrecision(MetadataSkeleton metadata) {
            return metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");
        }

        private static void HandleBaseCurrencies(MetadataSkeleton metadata, XrmDb db, Entity entity) {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr)) {
                return;
            }
            var currency = db.GetEntity(LogicalNames.TransactionCurrency, entity.GetAttributeValue<EntityReference>(transAttr).Id);
            var attributesMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            if (!currency.GetAttributeValue<decimal?>("exchangerate").HasValue) {
                throw new FaultException($"No exchangerate specified for transactioncurrency '{entity.GetAttributeValue<EntityReference>(transAttr)}'");
            }

            var baseCurrency = GetBaseCurrency(metadata);
            foreach (var attr in entity.Attributes.ToList()) {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && !attr.Key.EndsWith("_base")) {
                    if (entity.GetAttributeValue<EntityReference>(transAttr) == baseCurrency) {
                        entity[attr.Key + "_base"] = attr.Value;
                    } else {
                        if (attr.Value is Money money) {
                            var value = money.Value / currency.GetAttributeValue<decimal?>("exchangerate").Value;
                            entity[attr.Key + "_base"] = new Money(value);
                        }
                    }
                }
            }
        }

        internal static void HandlePrecision(MetadataSkeleton metadata, XrmDb db, Entity entity) {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr)) {
                return;
            }
            var currency = db.GetEntity(LogicalNames.TransactionCurrency, entity.GetAttributeValue<EntityReference>(transAttr).Id);
            var attributesMetadata = metadata.EntityMetadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            var baseCurrencyPrecision = GetBaseCurrencyPrecision(metadata);
            foreach (var attr in entity.Attributes.ToList()) {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && attr.Value != null) {
                    var attributeMetadata = attributesMetadata.First(m => m.LogicalName == attr.Key) as MoneyAttributeMetadata;
                    int? precision = null;
                    switch (attributeMetadata.PrecisionSource) {
                        case 0:
                            precision = attributeMetadata.Precision;
                            break;
                        case 1:
                            precision = baseCurrencyPrecision;
                            break;
                        case 2:
                            precision = currency.GetAttributeValue<int?>("currencyprecision");
                            break;
                    }

                    if (!precision.HasValue) {
                        switch (attributeMetadata.PrecisionSource) {
                            case 0:
                                throw new MockupException($"No precision set for field '{attr.Key}' on entity '{entity.LogicalName}'");
                            case 1:
                                throw new MockupException($"No precision set for organization. Please check you have the correct metadata");
                            case 2:
                                throw new MockupException($"No precision set for currency. Make sure you set the precision for your own currencies");
                        }
                    }

                    var rounded = Math.Round(((Money)attr.Value).Value, precision.Value);
                    if (rounded < (decimal)attributeMetadata.MinValue.Value || rounded > (decimal)attributeMetadata.MaxValue.Value) {
                        throw new FaultException($"'{attr.Key}' was outside the ranges '{attributeMetadata.MinValue}','{attributeMetadata.MaxValue}' with value '{rounded}' ");
                    }
                    entity[attr.Key] = new Money(rounded);
                }
            }
        }

        internal static void HandleCurrencies(MetadataSkeleton metadata, XrmDb db, Entity entity) {
            HandleBaseCurrencies(metadata, db, entity);
            HandlePrecision(metadata, db, entity);
        }

        internal static bool HasCircularReference(Dictionary<string, EntityMetadata> metadata, Entity entity) {
            if (!metadata.ContainsKey(entity.LogicalName)) return false;
            var attributeMetadata = metadata.GetMetadata(entity.LogicalName).Attributes;
            var references = entity.Attributes.Where(a => attributeMetadata.FirstOrDefault(m => m.LogicalName == a.Key) is LookupAttributeMetadata).Select(a => a.Value);
            foreach (var r in references) {
                if (r == null) continue;
                Guid guid;
                if (r is EntityReference) {
                    guid = (r as EntityReference).Id;
                } else if (r is Guid) {
                    guid = (Guid)r;
                } else if (r is EntityCollection) {
                    continue;
                } else {
                    throw new NotImplementedException($"{r.GetType()} not implemented in HasCircularReference");
                }
                if (guid == entity.Id) return true;
            }
            return false;
        }

        internal static void SetOwner(XrmDb db, Security dataMethods, MetadataSkeleton metadata, Entity entity, EntityReference owner) {
            var ownershipType = metadata.EntityMetadata.GetMetadata(entity.LogicalName).OwnershipType;

            if (!ownershipType.HasValue) {
                throw new MockupException($"No ownership type set for '{entity.LogicalName}'");
            }

            if (ownershipType.Value.HasFlag(OwnershipTypes.UserOwned) || ownershipType.Value.HasFlag(OwnershipTypes.TeamOwned)) {
                if (db.GetDbRowOrNull(owner) == null) {
                    throw new FaultException($"Owner referenced with id '{owner.Id}' does not exist");
                }

                var prevOwner = entity.Attributes.ContainsKey("ownerid") ? entity["ownerid"] : null;
                entity["ownerid"] = owner;

                if (!dataMethods.HasPermission(entity, AccessRights.ReadAccess, owner)) {
                    entity["ownerid"] = prevOwner;
                    throw new FaultException($"Trying to assign '{entity.LogicalName}' with id '{entity.Id}'" +
                        $" to '{owner.LogicalName}' with id '{owner.Id}', but owner does not have read access for that entity");
                }

                entity["owningbusinessunit"] = null;
                entity["owninguser"] = null;
                entity["owningteam"] = null;


                if (entity.LogicalName != LogicalNames.SystemUser && entity.LogicalName != LogicalNames.Team) {
                    if (owner.LogicalName == LogicalNames.SystemUser && ownershipType.Value.HasFlag(OwnershipTypes.UserOwned)) {
                        entity["owninguser"] = owner;
                    } else if (owner.LogicalName == "team") {
                        entity["owningteam"] = owner;
                    } else {
                        throw new MockupException($"Trying to give owner to {owner.LogicalName} but ownershiptype is {ownershipType.ToString()}");
                    }
                    entity["owningbusinessunit"] = GetBusinessUnit(db, owner);
                }
            }
        }

        internal static EntityReference GetBusinessUnit(XrmDb db, EntityReference owner) {
            var user = db.GetEntityOrNull(owner);
            if (user == null) {
                return null;
            }
            var buRef = user.GetAttributeValue<EntityReference>("businessunitid");
            var bu = db.GetEntityOrNull(buRef);
            if (bu == null) {
                return null;
            }
            buRef.Name = bu.GetAttributeValue<string>("name");
            return buRef;
        }

        /*
         * Possible error with cast to Entity here, was (T) before
         * 
         * */
        public static Entity MakeProxyTypeEntity(Entity entity, Type ty) {
            var method = typeof(Entity).GetMethod("ToEntity").MakeGenericMethod(ty);
            return (Entity)method.Invoke(entity, null);
        }

        public static bool MatchesCriteria(DbRow row, FilterExpression criteria) {
            if (criteria.FilterOperator == LogicalOperator.And)
                return criteria.Filters.All(f => MatchesCriteria(row, f)) && criteria.Conditions.All(c => EvaluateCondition(row, c));
            else
                return criteria.Filters.Any(f => MatchesCriteria(row, f)) || criteria.Conditions.Any(c => EvaluateCondition(row, c));
        }


        private static bool EvaluateCondition(DbRow row, ConditionExpression condition) {
            if (condition.AttributeName == null) {
                return Matches(row.Id, condition.Operator, condition.Values);
            }

            var attr = row.GetColumn(condition.AttributeName);
            if (attr is DbRow related) attr = related.Id;
            var values = condition.Values.Select(v => ConvertToComparableObject(v));
            return Matches(attr, condition.Operator, values);
        }

        public static object ConvertToComparableObject(object obj) {
            if (obj is EntityReference entityReference)
                return entityReference.Id;

            else if (obj is Money money)
                return money.Value;

            else if (obj is AliasedValue aliasedValue)
                return aliasedValue.Value;

            else if (obj is OptionSetValue optionSetValue)
                return optionSetValue.Value;

            else if (obj != null && obj.GetType().IsEnum)
                return (int)obj;

            else
                return obj;
        }

        public static bool Matches(object attr, ConditionOperator op, IEnumerable<object> values) {
            switch (op) {
                case ConditionOperator.Null:
                    return attr == null;

                case ConditionOperator.NotNull:
                    return attr != null;

                case ConditionOperator.Equal:
                    return Equals(values.First(), attr);

                case ConditionOperator.NotEqual:
                    return !Equals(values.First(), attr);

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterEqual:
                case ConditionOperator.LessEqual:
                case ConditionOperator.LessThan:
                    return Compare((IComparable)attr, op, (IComparable)values.First());

                case ConditionOperator.Like:
                    if (attr == null)
                        return false;
                    var sAttr = (string)attr;
                    var pattern = (string)values.First();
                    if (pattern.First() == '%' && (pattern.Last() == '%')) {
                        return sAttr.Contains(pattern.Substring(1, pattern.Length - 1));
                    } else if (pattern.First() == '%') {
                        return sAttr.EndsWith(pattern.Substring(1));
                    } else if (pattern.Last() == '%') {
                        return sAttr.StartsWith(pattern.Substring(0, pattern.Length - 1));
                    } else {
                        throw new NotImplementedException($"The like matching for '{pattern}' has not been implemented yet");
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
                    throw new NotImplementedException($"The ConditionOperator '{op}' has not been implemented yet.");
            }

        }

        public static bool Compare(IComparable attr, ConditionOperator op, IComparable value) {
            // if atleast one of the two compare values are null. Then compare returns null
            if (attr == null || value == null)
                return false;
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

        internal static void Touch(Entity dbEntity, EntityMetadata metadata, TimeSpan timeOffset, EntityReference user) {
            if (IsSettableAttribute("modifiedon", metadata) && IsSettableAttribute("modifiedby", metadata)) {
                dbEntity["modifiedon"] = DateTime.Now.Add(timeOffset);
                dbEntity["modifiedby"] = user;
            }
        }

        internal static void PopulateEntityReferenceNames(Entity entity, XrmDb db) {
            foreach (var attr in entity.Attributes) {
                if (attr.Value is EntityReference eRef) {
                    var row = db.GetDbRowOrNull(eRef);
                    if (row != null) {
                        var nameAttr = row.Metadata.PrimaryNameAttribute;
                        eRef.Name = row.GetColumn<string>(nameAttr);
                    }
                }
            }
        }

        internal static object GetComparableAttribute(object attribute) {
            if (attribute is Money money) {
                return money.Value;
            }
            if (attribute is EntityReference eRef) {
                return eRef.Name;
            }
            if (attribute is OptionSetValue osv) {
                return osv.Value;
            }
            return attribute;
        }

        internal static RelationshipMetadataBase GetRelatedEntityMetadata(Dictionary<string, EntityMetadata> metadata, string entityType, string relationshipName) {
            if (!metadata.ContainsKey(entityType)) {
                return null;
            }
            var oneToMany = metadata.GetMetadata(entityType).OneToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (oneToMany != null) {
                return oneToMany;
            }

            var manyToOne = metadata.GetMetadata(entityType).ManyToOneRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToOne != null) {
                return manyToOne;
            }

            var manyToMany = metadata.GetMetadata(entityType).ManyToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToMany != null) {
                return manyToMany;
            }

            return null;
        }


        internal static MetadataSkeleton GetMetadata(string prefix) {
            var pathToMetadata = Path.Combine(prefix, "Metadata.xml");
            if (!File.Exists(pathToMetadata)) {
                throw new ArgumentException($"Could not find metadata file at '{pathToMetadata}'." +
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

        internal static Guid GetGuidFromReference(object reference) {
            return reference is EntityReference ? (reference as EntityReference).Id : (Guid)reference;
        }

        internal static EntityReference ToEntityReferenceWithKeyAttributes(this Entity entity) {
            var reference = entity.ToEntityReference();
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            reference.KeyAttributes = entity.KeyAttributes;
#endif
            return reference;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal static string ToPrettyString(this KeyAttributeCollection keys) {
            return "(" + String.Join(", ", keys.Select(x => $"{x.Key}:{x.Value}")) + ")";
        }
#endif

        internal static Entity ToActivityPointer(this Entity entity) {
            if (!Activities.Contains(entity.LogicalName)) return null;

            var pointer = new Entity("activitypointer") {
                Id = entity.Id
            };
            pointer["activityid"] = entity.Id;
            pointer["ownerid"] = entity.GetAttributeValue<EntityReference>("ownerid");
            pointer["activitytypecode"] = entity.GetAttributeValue<OptionSetValue>("activitytypecode");
            pointer["actualdurationminutes"] = entity.GetAttributeValue<int>("actualdurationminutes");
            pointer["actualend"] = entity.GetAttributeValue<DateTime>("actualend");
            pointer["actualstart"] = entity.GetAttributeValue<DateTime>("actualstart");
            pointer["description"] = entity.GetAttributeValue<string>("description");
            pointer["deliveryprioritycode"] = entity.GetAttributeValue<int>("deliveryprioritycode");
            pointer["isbilled"] = entity.GetAttributeValue<bool>("isbilled");
            pointer["isregularactivity"] = entity.GetAttributeValue<bool>("isregularactivity");
            pointer["isworkflowcreated"] = entity.GetAttributeValue<bool>("isworkflowcreated");
            pointer["prioritycode"] = entity.GetAttributeValue<OptionSetValue>("prioritycode");
            pointer["scheduleddurationminutes"] = entity.GetAttributeValue<int>("scheduleddurationminutes");
            pointer["scheduledend"] = entity.GetAttributeValue<DateTime>("scheduledend");
            pointer["scheduledstart"] = entity.GetAttributeValue<DateTime>("scheduledstart");
            pointer["senton"] = entity.GetAttributeValue<DateTime>("senton");
            pointer["subject"] = entity.GetAttributeValue<string>("subject");

            switch (entity.GetAttributeValue<OptionSetValue>("statecode").Value) {
                case 1:
                    pointer["statecode"] = new OptionSetValue(1);
                    pointer["statuscode"] = new OptionSetValue(2);
                    break;

                case 2:
                    pointer["statecode"] = new OptionSetValue(2);
                    pointer["statuscode"] = new OptionSetValue(3);
                    break;

                case 3:
                    pointer["statecode"] = new OptionSetValue(3);
                    pointer["statuscode"] = new OptionSetValue(4);
                    break;

                default:
                    pointer["statecode"] = new OptionSetValue(0);
                    pointer["statuscode"] = new OptionSetValue(1);
                    break;
            }
            return pointer;
        }
    }

    internal class LogicalNames {
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
    internal enum workflow_runas {

        [EnumMember()]
        Owner = 0,

        [EnumMember()]
        CallingUser = 1,
    }

    [DataContract()]
    internal enum workflow_stage {

        [EnumMember()]
        Preoperation = 20,

        [EnumMember()]
        Postoperation = 40,
    }

    [DataContract()]
    internal enum Workflow_Type {

        [EnumMember()]
        Definition = 1,

        [EnumMember()]
        Activation = 2,

        [EnumMember()]
        Template = 3,
    }

    [DataContract()]
    internal enum componentstate {

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
    internal enum Workflow_Scope {

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
    internal enum Workflow_Mode {

        [EnumMember()]
        Background = 0,

        [EnumMember()]
        Realtime = 1,
    }

    [DataContract()]
    internal enum Workflow_BusinessProcessType {

        [EnumMember()]
        BusinessFlow = 0,

        [EnumMember()]
        TaskFlow = 1,
    }

    [DataContract()]
    internal enum Workflow_Category {

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
    internal enum WorkflowState {

        [EnumMember()]
        Draft = 0,

        [EnumMember()]
        Activated = 1,
    }

    [DataContract()]
    internal enum Workflow_StatusCode {

        [EnumMember()]
        Draft = 1,

        [EnumMember()]
        Activated = 2,
    }

    [DataContract()]
    internal enum OpportunityState {

        [EnumMember()]
        Open = 0,

        [EnumMember()]
        Won = 1,

        [EnumMember()]
        Lost = 2,
    }

    [DataContract()]
    internal enum Opportunity_StatusCode {

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
