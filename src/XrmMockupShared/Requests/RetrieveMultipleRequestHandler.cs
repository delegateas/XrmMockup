using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using DG.Tools.XrmMockup.Database;

namespace DG.Tools.XrmMockup {
    internal class RetrieveMultipleRequestHandler : RequestHandler {
        internal RetrieveMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveMultiple") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef) {
            var request = MakeRequest<RetrieveMultipleRequest>(orgRequest);
            var queryExpr = request.Query as QueryExpression;
            var fetchExpr = request.Query as FetchExpression;
            var queryByAttr = request.Query as QueryByAttribute;
            if (fetchExpr != null) {
                queryExpr = XmlHandling.FetchXmlToQueryExpression(fetchExpr.Query);
            }
            else if (queryByAttr != null)
            {
                queryExpr = Utility.QueryByAttributeToQueryExpression(queryByAttr);
            }
            FillAliasIfEmpty(queryExpr);
            var collection = new EntityCollection();
            db.PrefillDBWithOnlineData(queryExpr);
            var rows = db.GetDBEntityRows(queryExpr.EntityName);
            if (db[queryExpr.EntityName].Count() > 0)
            {
                foreach (var row in rows)
                {
                    var entity = row.ToEntity();
                    var toAdd = core.GetStronglyTypedEntity(entity, row.Metadata, null);

                    Utility.SetFormmattedValues(db, toAdd, row.Metadata);

                    if (queryExpr.LinkEntities.Count > 0) {
                        foreach (var linkEntity in queryExpr.LinkEntities) {
                            var alliasedValues = GetAliasedValuesFromLinkentity(linkEntity, entity, toAdd, db);
                            collection.Entities.AddRange(
                                alliasedValues
                                .Where(e => Utility.MatchesCriteria(e, queryExpr.Criteria)));
                        }
                    } else if(Utility.MatchesCriteria(toAdd, queryExpr.Criteria)) {
                        collection.Entities.Add(toAdd);
                    }
                }
            }
            var filteredEntities = new EntityCollection();
            filteredEntities.Entities.AddRange(collection.Entities.Where(e => security.HasPermission(e, AccessRights.ReadAccess, userRef)));

            var orders = queryExpr.Orders;
            var orderedCollection = new EntityCollection();
            // TODO: Check the order that the orders are executed in is correct
            if (orders.Count > 2) {
                throw new MockupException("Number of orders are greater than 2, unsupported in crm");
            } else if (orders.Count == 1) {
                if (orders.First().OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(filteredEntities.Entities.OrderBy(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName])));
                else
                    orderedCollection.Entities.AddRange(filteredEntities.Entities.OrderByDescending(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName])));
            } else if (orders.Count == 2) {
                if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(filteredEntities.Entities
                        .OrderBy(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => Utility.GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(filteredEntities.Entities
                        .OrderBy(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => Utility.GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(filteredEntities.Entities
                        .OrderByDescending(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => Utility.GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(filteredEntities.Entities
                        .OrderByDescending(x => Utility.GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => Utility.GetComparableAttribute(x.Attributes[orders[1].AttributeName])));
            }

            var colToReturn = new EntityCollection();

            if (orderedCollection.Entities.Count != 0) {
                foreach (var entity in orderedCollection.Entities) {
                    KeepAttributesAndAliasAttributes(entity, queryExpr.ColumnSet);
                }
                colToReturn = orderedCollection;
            } else {
                foreach (var entity in filteredEntities.Entities) {
                    KeepAttributesAndAliasAttributes(entity, queryExpr.ColumnSet);
                }
                colToReturn = filteredEntities;
            }
            
            var resp = new RetrieveMultipleResponse();
            resp.Results["EntityCollection"] = colToReturn;
            return resp;
        }


        private List<Entity> GetAliasedValuesFromLinkentity(LinkEntity linkEntity, Entity parent, Entity toAdd, XrmDb db) {
            var collection = new List<Entity>();
            foreach (var linkedRow in db[linkEntity.LinkToEntityName]) {
                var linkedEntity = linkedRow.ToEntity();

                if (linkedEntity.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName)) {
                    var linkedAttr = Utility.ConvertToComparableObject(
                        linkedEntity.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = Utility.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr)) {
                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                metadata.EntityMetadata.GetMetadata(toAdd.LogicalName), linkedEntity.Attributes);

                        if (linkEntity.LinkEntities.Count > 0) {
                            var subEntities = new List<Entity>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities) {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                var alliasedLinkValues = GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedEntity, aliasedEntity, db);
                                subEntities.AddRange(alliasedLinkValues
                                        .Where(e => Utility.MatchesCriteria(e, linkEntity.LinkCriteria)));
                            }
                            collection.AddRange(subEntities);
                        } else if(Utility.MatchesCriteria(aliasedEntity, linkEntity.LinkCriteria)) {
                            collection.Add(aliasedEntity);
                        }

                    }
                }
            }
            if (linkEntity.JoinOperator == JoinOperator.LeftOuter && collection.Count == 0) {
                collection.Add(toAdd);
            }
            return collection;
        }

        private Entity GetEntityWithAliasAttributes(string alias, Entity toAdd, EntityMetadata metadata, AttributeCollection attributes) {
            var parentClone = core.GetStronglyTypedEntity(toAdd, metadata, null);
            foreach (var attr in attributes.Keys) {
                parentClone.Attributes.Add(alias + "." + attr, new AliasedValue(alias, attr, attributes[attr]));
            }
            return parentClone;
        }

        private void KeepAttributesAndAliasAttributes(Entity entity, ColumnSet toKeep) {
            var clone = entity.CloneEntity(metadata.EntityMetadata.GetMetadata(entity.LogicalName), toKeep);
            if (toKeep != null && !toKeep.AllColumns)
                clone.Attributes.AddRange(entity.Attributes.Where(x => x.Key.Contains(".")));
            entity.Attributes.Clear();
            entity.Attributes.AddRange(clone.Attributes);
        }

        private void FillAliasIfEmpty(QueryExpression expression)
        {
            if (expression.LinkEntities.Count == 0)
                return;

            int depth = 1;
            foreach (var le in expression.LinkEntities)
            {
                FillAliasIfEmpty(le, ref depth);
            }
        }

        private void FillAliasIfEmpty(LinkEntity linkEntity, ref int linkCount)
        {
            if(linkEntity.EntityAlias == null)
            {
                linkEntity.EntityAlias = $"{linkEntity.LinkToEntityName}{linkCount}";
                linkCount++;
            }
#if !(XRM_MOCKUP_2011)
            if (linkEntity.LinkCriteria != null)
            {
                FillEntityNameIfEmpty(linkEntity.LinkCriteria, linkEntity.EntityAlias);
            }
#endif
            if (linkEntity.LinkEntities.Count > 0)
            {
                foreach (var le in linkEntity.LinkEntities)
                {
                    FillAliasIfEmpty(le, ref linkCount);
                }
            }
        }
#if !(XRM_MOCKUP_2011)
        private void FillEntityNameIfEmpty(FilterExpression filter, string alias)
        {
            foreach (var cond in filter.Conditions)
            {
                FillEntityNameIfEmpty(cond, alias);
            }
            foreach (var subFilter in filter.Filters)
            {
                FillEntityNameIfEmpty(subFilter, alias);
            }
        }

        private void FillEntityNameIfEmpty(ConditionExpression condition, string alias)
        {
            if (condition.EntityName != null)
            {
                return;
            }
            condition.EntityName = alias;
        }
#endif
    }
}