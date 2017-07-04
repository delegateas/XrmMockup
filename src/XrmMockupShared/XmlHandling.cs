﻿using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DG.Tools {
    internal static class XmlHandling {

        public static QueryExpression FetchXmlToQueryExpression(string fetchXml) {
            var query = new QueryExpression();
            var fetch = XElement.Parse(fetchXml);
            var entity = fetch.Element("entity");
            var logicalName = entity.Attribute("name");
            var page = fetch.Attribute("page");
            var count = fetch.Attribute("count");

            query.EntityName = logicalName.Value;

            query.ColumnSet = new ColumnSet();
            if (entity.Element("all-attributes") != null) {
                query.ColumnSet.AllColumns = true;
            }
            foreach (var column in entity.Elements("attribute")) {
                query.ColumnSet.Columns.Add(column.Attribute("name").Value);
            }

            if (entity.Element("filter") != null) {
                query.Criteria = FilterExpFromXml(entity.Element("filter").ToString());
            }

            if (page == null && count != null) {
                query.TopCount = int.Parse(count.Value);
            } else if (page != null && count != null) {
                query.PageInfo = new PagingInfo { PageNumber = int.Parse(page.Value), Count = int.Parse(count.Value) };
            }

            foreach (var order in entity.Elements("order")) {
                var orderExp = new OrderExpression() {
                    AttributeName = order.Attribute("attribute").Value,
                    OrderType = order.Attribute("descending").Value == "false" ? OrderType.Ascending : OrderType.Descending
                };
            }

            int aliasCount = 0;

            foreach (var linkEntity in entity.Elements("link-entity")) {
                query.LinkEntities.Add(LinkEntityFromXml(logicalName.Value, linkEntity.ToString(), ref aliasCount));
            }

            return query;
        }

        public static FilterExpression FilterExpFromXml(string filterXml) {
            var filter = XElement.Parse(filterXml);
            var filterExp = new FilterExpression();

            if (filter.Attribute("type") == null || filter.Attribute("type").Value == "and") {
                filterExp.FilterOperator = LogicalOperator.And;
            } else if (filter.Attribute("type").Value == "or") {
                filterExp.FilterOperator = LogicalOperator.Or;
            }

            foreach (var condition in filter.Elements("condition")) {
                var attr = condition.Attribute("attribute").Value;
                var op = Utility.ConditionOperators[condition.Attribute("operator").Value];
                if (condition.HasElements) {
                    var values = new List<object>();
                    foreach (var value in condition.Elements("value")) {
                        values.Add(value.Value);
                    }

                    filterExp.AddCondition(attr, op, values);
                } else {
                    var value = condition.Attribute("value").Value;
                    filterExp.AddCondition(attr, op, value);

                }
            }

            foreach (var subFilter in filter.Elements("filter")) {
                filterExp.AddFilter(FilterExpFromXml(subFilter.ToString()));
            }

            return filterExp;
        }

        public static LinkEntity LinkEntityFromXml(string parentLogicalName, string linkXml, ref int aliasCount) {
            var link = XElement.Parse(linkXml);
            var joinOperator = link.Attribute("link-type");

            var linkEntity = new LinkEntity() {
                EntityAlias = $"{link.Attribute("name").Value}_{aliasCount}",
                LinkFromEntityName = parentLogicalName,
                LinkFromAttributeName = link.Attribute("to").Value,
                LinkToEntityName = link.Attribute("name").Value,
                LinkToAttributeName = link.Attribute("from").Value,
                Columns = new ColumnSet()
            };

            if (link.Element("all-attributes") != null) {
                linkEntity.Columns.AllColumns = true;
            }

            foreach (var column in link.Elements("attribute")) {
                linkEntity.Columns.Columns.Add(column.Attribute("name").Value);
            }

            if (joinOperator == null || joinOperator.Value == "inner") {
                linkEntity.JoinOperator = JoinOperator.Inner;
            } else if (joinOperator.Value == "outer") {
                linkEntity.JoinOperator = JoinOperator.LeftOuter;
            } else if (joinOperator.Value == "natural") {
                linkEntity.JoinOperator = JoinOperator.Natural;
            }

            if (link.Element("filter") != null) {
                linkEntity.LinkCriteria = FilterExpFromXml(link.Element("filter").ToString());
            }

            foreach (var subLink in link.Elements("link-entity")) {
                aliasCount++;
                linkEntity.LinkEntities.Add(LinkEntityFromXml(parentLogicalName, subLink.ToString(), ref aliasCount));
            }

            return linkEntity;


        }
    }
}
