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
            if (queryExpr == null) {
                queryExpr = XmlHandling.FetchXmlToQueryExpression(fetchExpr.Query);
            }

            var collection = new EntityCollection();
            db.PrefillDBWithOnlineData(queryExpr);
            var rows = db.GetDBEntityRows(queryExpr.EntityName);
            if (db[queryExpr.EntityName].Count() > 0)
            {
                foreach (var row in rows)
                {
                    if (!Utility.MatchesCriteria(row, queryExpr.Criteria)) continue;
                    var entity = row.ToEntity();
                    var toAdd = core.GetStronglyTypedEntity(entity, row.Metadata, null);

                    if (queryExpr.LinkEntities.Count > 0) {
                        foreach (var linkEntity in queryExpr.LinkEntities) {
                            collection.Entities.AddRange(
                                GetAliasedValuesFromLinkentity(linkEntity, entity, toAdd, db));
                        }
                    } else {
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
                if (!Utility.MatchesCriteria(linkedRow, linkEntity.LinkCriteria)) continue;
                var linkedEntity = linkedRow.ToEntity();

                if (linkedEntity.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName)) {
                    var linkedAttr = Utility.ConvertToComparableObject(
                        linkedEntity.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = Utility.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr)) {
                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                metadata.EntityMetadata.GetMetadata(toAdd.LogicalName), linkedEntity.Attributes, linkEntity.Columns);

                        if (linkEntity.LinkEntities.Count > 0) {
                            var subEntities = new List<Entity>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities) {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                subEntities.AddRange(
                                    GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedEntity, aliasedEntity, db));
                            }
                            collection.AddRange(subEntities);
                        } else {
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

        private Entity GetEntityWithAliasAttributes(string alias, Entity toAdd, EntityMetadata metadata, AttributeCollection attributes,
                ColumnSet columns) {
            var parentClone = core.GetStronglyTypedEntity(toAdd, metadata, null);
            foreach (var attr in columns.Columns) {
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
    }
}