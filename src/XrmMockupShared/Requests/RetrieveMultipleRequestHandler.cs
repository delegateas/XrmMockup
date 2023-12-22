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
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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

            if (queryExpr.EntityName == null)
            {
                throw new FaultException("The 'RetrieveMultiple' method does not support entities of type 'none'");
            }

            FillAliasIfEmpty(queryExpr);

            db.PrefillDBWithOnlineData(queryExpr);
            var rows = db.GetDBEntityRows(queryExpr.EntityName);

            var entityMetadata = metadata.EntityMetadata[queryExpr.EntityName];

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            var rowBag = new ConcurrentBag<DbRow>();
            //don't add the rows by passing in via the constructor as it can change the order
            foreach (var row in rows)
            {
                rowBag.Add(row);
            }

            foreach (var row in rowBag)
            {
                core.ExecuteCalculatedFields(row);
            }
            //get the rows again to include the calulated values
            rows = db.GetDBEntityRows(queryExpr.EntityName);
#endif

            var collection = new ConcurrentBag<KeyValuePair<DbRow, Entity>>();

            Parallel.ForEach(rows, row =>
            {
                var entity = row.ToEntity();

                var toAdd = core.GetStronglyTypedEntity(entity, row.Metadata, null);

                if (queryExpr.LinkEntities.Count > 0)
                {
                    //foreach (var linkEntity in queryExpr.LinkEntities)
                    Parallel.ForEach(queryExpr.LinkEntities, linkEntity =>
                    {
                        var alliasedValues = GetAliasedValuesFromLinkentity(linkEntity, entity, toAdd, db);
                        var matchingValues = alliasedValues.Where(e => Utility.MatchesCriteria(e, queryExpr.Criteria));
                        Parallel.ForEach(matchingValues, m =>
                        {
                            if (security.HasPermission(m, AccessRights.ReadAccess, userRef))
                            {
                                Utility.SetFormmattedValues(db, m, entityMetadata);
                                collection.Add(new KeyValuePair<DbRow, Entity>(row, m));
                            }
                        });
                    });
                }
                else if (Utility.MatchesCriteria(toAdd, queryExpr.Criteria))
                {
                    if (security.HasPermission(toAdd, AccessRights.ReadAccess, userRef))
                    {
                        Utility.SetFormmattedValues(db, toAdd, entityMetadata);
                        collection.Add(new KeyValuePair<DbRow, Entity>(row, toAdd));
                    }
                }
            });
            
            var orders = queryExpr.Orders;
            var orderedCollection = new EntityCollection();
            // TODO: Check the order that the orders are executed in is correct
            if (orders == null || orders.Count == 0)
            {
                orderedCollection.Entities.AddRange(collection.OrderBy(x => x.Key.Sequence).Select(y=>y.Value));
            }
            if (orders.Count > 2) {
                throw new MockupException("Number of orders are greater than 2, unsupported in crm");
            } else if (orders.Count == 1) {
                if (orders.First().OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection.OrderBy(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => x.Key.Sequence)
                        .Select(y => y.Value));
                else
                    orderedCollection.Entities.AddRange(collection.OrderByDescending(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName])).Select(y => y.Value));
            } else if (orders.Count == 2) {
                if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection
                        .OrderBy(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[1].AttributeName]))
                        .ThenBy(x => x.Key.Sequence)
                        .Select(y => y.Value));

                else if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(collection
                        .OrderBy(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[1].AttributeName]))
                        .ThenBy(x => x.Key.Sequence)
                        .Select(y => y.Value));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection
                        .OrderByDescending(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[1].AttributeName]))
                        .ThenBy(x => x.Key.Sequence)
                        .Select(y => y.Value));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(collection
                        .OrderByDescending(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => Utility.GetComparableAttribute(x.Value.Attributes[orders[1].AttributeName]))
                        .ThenBy(x => x.Key.Sequence)
                        .Select(y => y.Value));
            }

            var colToReturn = new EntityCollection();

            if (orderedCollection.Entities.Count != 0) {
                Parallel.ForEach(orderedCollection.Entities, entity =>
                {
                    KeepAttributesAndAliasAttributes(entity, queryExpr.ColumnSet);
                }
                );
                colToReturn = orderedCollection;
            } else {
                Parallel.ForEach(collection, kvp =>
                {
                    KeepAttributesAndAliasAttributes(kvp.Value, queryExpr.ColumnSet);
                 }
               );
                colToReturn = new EntityCollection(collection.Select(x=>x.Value).ToList());
            }

            // According to docs, should return -1 if ReturnTotalRecordCount set to false
            colToReturn.TotalRecordCount = queryExpr.PageInfo.ReturnTotalRecordCount ? colToReturn.Entities.Count : -1;

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