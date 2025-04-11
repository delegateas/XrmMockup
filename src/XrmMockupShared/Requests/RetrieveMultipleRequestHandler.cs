using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup
{
    internal class RetrieveMultipleRequestHandler : RequestHandler
    {
        internal RetrieveMultipleRequestHandler(Core core, XrmDb db, MetadataSkeleton metadata, Security security) : base(core, db, metadata, security, "RetrieveMultiple") { }

        internal override OrganizationResponse Execute(OrganizationRequest orgRequest, EntityReference userRef)
        {
            var request = MakeRequest<RetrieveMultipleRequest>(orgRequest);
            var queryExpr = request.Query as QueryExpression;
            var fetchExpr = request.Query as FetchExpression;
            var queryByAttr = request.Query as QueryByAttribute;
            if (fetchExpr != null)
            {
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
                                Utility.SetFormattedValues(db, m, entityMetadata);
                                collection.Add(new KeyValuePair<DbRow, Entity>(row, m));
                            }
                        });
                    });
                }
                else if (Utility.MatchesCriteria(toAdd, queryExpr.Criteria))
                {
                    if (security.HasPermission(toAdd, AccessRights.ReadAccess, userRef))
                    {
                        Utility.SetFormattedValues(db, toAdd, entityMetadata);
                        collection.Add(new KeyValuePair<DbRow, Entity>(row, toAdd));
                    }
                }
            });

            var orders = queryExpr.Orders;
            IOrderedEnumerable<KeyValuePair<DbRow, Entity>> tempSortedList;

            // TODO: Check the order that the orders are executed in is correct
            if (orders == null || orders.Count == 0)
            {
                tempSortedList = collection.OrderBy(x => x.Key.Sequence);
            }
            else
            {
                Func<KeyValuePair<DbRow, Entity>, object> selector = x => x.Value.Attributes.TryGetValue(orders[0].AttributeName, out var a) ? Utility.GetComparableAttribute(a) : null;
                if (orders.First().OrderType == OrderType.Ascending)
                    tempSortedList = collection.OrderBy(selector);
                else
                    tempSortedList = collection.OrderByDescending(selector);

                foreach (var order in orders.Skip(1))
                {
                    selector = (x => x.Value.Attributes.TryGetValue(order.AttributeName, out var a) ? Utility.GetComparableAttribute(a) : null);
                    if (order.OrderType == OrderType.Ascending)
                        tempSortedList = tempSortedList.ThenBy(selector);
                    else
                        tempSortedList = tempSortedList.ThenByDescending(selector);

                }
                tempSortedList = tempSortedList.ThenBy(x => x.Key.Sequence);
            }

            // Convert to array to lock-in the ordering (if we are ordering by something that doesn't exist in the columnset, the ordering will fail afterwards)
            var orderedEntities = tempSortedList.Select(x => x.Value).ToArray();

            // Calculate formula fields before we filter the fetched columns
            Parallel.ForEach(orderedEntities, entity => core.ExecuteFormulaFields(entityMetadata, entity).GetAwaiter().GetResult());

            // Refine and filter the columns
            var entitiesToReturn = RefineEntityAttributes(orderedEntities, queryExpr.ColumnSet);

            if (queryExpr.Distinct)
            {
                var uniqueIds = new HashSet<Guid>();
                entitiesToReturn = entitiesToReturn.Where(entity => uniqueIds.Add(entity.Id));
            }

            if (queryExpr.TopCount.HasValue || (queryExpr.PageInfo?.Count ?? 0) > 0)
            {
                entitiesToReturn = entitiesToReturn.Take(queryExpr.TopCount ?? queryExpr.PageInfo.Count); // QueryExpression TopCount or Linq query Take operator
            }

            var colToReturn = new EntityCollection(entitiesToReturn.ToList());

            // According to docs, should return -1 if ReturnTotalRecordCount set to false
            colToReturn.TotalRecordCount = queryExpr.PageInfo.ReturnTotalRecordCount ? colToReturn.Entities.Count : -1;

            var resp = new RetrieveMultipleResponse();

            resp.Results["EntityCollection"] = colToReturn;
            return resp;

        }

        private IEnumerable<Entity> RefineEntityAttributes(IEnumerable<Entity> entities, ColumnSet columnSet)
        {
            Parallel.ForEach(entities, entity =>
            {
                KeepAttributesAndAliasAttributes(entity, columnSet);
            });
            return entities;
        }

        private List<Entity> GetAliasedValuesFromLinkentity(LinkEntity linkEntity, Entity parent, Entity toAdd, XrmDb db)
        {
            var collection = new List<Entity>();
            foreach (var linkedRow in db[linkEntity.LinkToEntityName])
            {
                var linkedEntity = linkedRow.ToEntity();

                if (linkedEntity.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName))
                {
                    var linkedAttr = Utility.ConvertToComparableObject(
                        linkedEntity.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = Utility.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr))
                    {
                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                metadata.EntityMetadata.GetMetadata(toAdd.LogicalName), linkedEntity.Attributes);

                        if (linkEntity.LinkEntities.Count > 0)
                        {
                            var subEntities = new List<Entity>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities)
                            {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                var alliasedLinkValues = GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedEntity, aliasedEntity, db);
                                subEntities.AddRange(alliasedLinkValues
                                        .Where(e => Utility.MatchesCriteria(e, linkEntity.LinkCriteria)));
                            }
                            collection.AddRange(subEntities);
                        }
                        else if (Utility.MatchesCriteria(aliasedEntity, linkEntity.LinkCriteria))
                        {
                            collection.Add(aliasedEntity);
                        }

                    }
                }
            }
            if (linkEntity.JoinOperator == JoinOperator.LeftOuter && collection.Count == 0)
            {
                collection.Add(toAdd);
            }
            return collection;
        }

        private Entity GetEntityWithAliasAttributes(string alias, Entity toAdd, EntityMetadata metadata, AttributeCollection attributes)
        {
            var parentClone = core.GetStronglyTypedEntity(toAdd, metadata, null);
            foreach (var attr in attributes.Keys)
            {
                parentClone.Attributes.Add(alias + "." + attr, new AliasedValue(alias, attr, attributes[attr]));
            }
            return parentClone;
        }

        private void KeepAttributesAndAliasAttributes(Entity entity, ColumnSet toKeep)
        {
            var clone = entity.CloneEntity(metadata.EntityMetadata.GetMetadata(entity.LogicalName), toKeep);
            if (toKeep != null && !toKeep.AllColumns)
                clone.Attributes.AddRange(entity.Attributes.Where(x => x.Key.Contains('.')));
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
            if (linkEntity.EntityAlias == null)
            {
                linkEntity.EntityAlias = $"{linkEntity.LinkToEntityName}{linkCount}";
                linkCount++;
            }
            if (linkEntity.LinkCriteria != null)
            {
                FillEntityNameIfEmpty(linkEntity.LinkCriteria, linkEntity.EntityAlias);
            }
            if (linkEntity.LinkEntities.Count > 0)
            {
                foreach (var le in linkEntity.LinkEntities)
                {
                    FillAliasIfEmpty(le, ref linkCount);
                }
            }
        }
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
    }
}