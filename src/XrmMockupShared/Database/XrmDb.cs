using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools.XrmMockup.Database {

    internal class XrmDb {
        private Dictionary<string, DbTable> TableDict = new Dictionary<string, DbTable>();
        private Dictionary<string, EntityMetadata> EntityMetadata;
        private OrganizationServiceProxy OnlineProxy;

        public XrmDb(Dictionary<string, EntityMetadata> entityMetadata, OrganizationServiceProxy onlineProxy) {
            this.EntityMetadata = entityMetadata;
            this.OnlineProxy = onlineProxy;
        }

        public DbTable this[string tableName] {
            get {
                if (!TableDict.ContainsKey(tableName)) {
                    if (!EntityMetadata.TryGetValue(tableName, out EntityMetadata entityMetadata)) {
                        throw new MockupException($"No EntityMetadata found for entity with logical name '{tableName}'.");
                    }
                    TableDict[tableName] = new DbTable(entityMetadata);
                }
                return TableDict[tableName];
            }
        }

        public void Add(Entity xrmEntity, bool withReferenceChecks = true) {
            var dbEntity = ToDbRow(xrmEntity, withReferenceChecks);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public DbRow ToDbRow(Entity xrmEntity, bool withReferenceChecks = true)
        {
            var primaryIdKey = this[xrmEntity.LogicalName].Metadata.PrimaryIdAttribute;
            if (!xrmEntity.Attributes.ContainsKey(primaryIdKey))
            {
                xrmEntity[primaryIdKey] = xrmEntity.Id;
            }

            var dbEntity = DbRow.FromEntity(xrmEntity, this, withReferenceChecks);
            if (dbEntity.Id != Guid.Empty)
            {
                if (this[dbEntity.Table.TableName][dbEntity.Id] != null)
                {
                    throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}' and id '{xrmEntity.Id}', but a record already exists with that Id.");
                }
            }
            else
            {
                dbEntity.Id = Guid.NewGuid();
            }

            return dbEntity;
        }

        public void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true) {
            foreach (var xrmEntity in xrmEntities) Add(xrmEntity, withReferenceChecks);
        }

        internal IEnumerable<DbRow> GetDBEntityRows(string EntityLogicalName)
        {
            return this[EntityLogicalName];
        }

        public void Update(Entity xrmEntity, bool withReferenceChecks = true) {
            var currentDbRow = GetDbRow(xrmEntity);

            var dbEntity = DbRow.FromEntity(xrmEntity, withReferenceChecks ? this : null);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public void Delete(Entity xrmEntity) {
            this[xrmEntity.LogicalName].Remove(xrmEntity.Id);
        }

        internal bool HasRow(EntityReference reference) {
            return this[reference.LogicalName][reference.Id] != null;
        }

        internal bool HasTable(string tableName)
        {
            return TableDict.ContainsKey(tableName);
        }

        internal bool IsValidEntity(string entityLogicalName)
        {
            return EntityMetadata.TryGetValue(entityLogicalName, out EntityMetadata entityMetadata);
        }

        internal void PrefillDBWithOnlineData(QueryExpression queryExpr)
        {
            if (OnlineProxy != null)
            {
                var onlineEntities = OnlineProxy.RetrieveMultiple(queryExpr).Entities;
                foreach (var onlineEntity in onlineEntities)
                {
                    if (this[onlineEntity.LogicalName][onlineEntity.Id] == null)
                    {
                        Add(onlineEntity, true);
                    }
                }
            }
        }

        internal DbRow GetDbRow(EntityReference reference, bool withReferenceCheck = true) {
            DbRow currentDbRow = null;

            if (reference?.Id != Guid.Empty) {                
                currentDbRow = this[reference.LogicalName][reference.Id];
                if (currentDbRow == null && OnlineProxy != null) {
                    if (!withReferenceCheck)
                        currentDbRow = DbRow.MakeDBRowRef(reference, this);
                    else
                    {
                        var onlineEntity = OnlineProxy.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                        Add(onlineEntity, withReferenceCheck);
                        currentDbRow = this[reference.LogicalName][reference.Id];
                    }
                }
                if (currentDbRow == null) {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            // Try fetching with key attributes if any
            else if (reference?.KeyAttributes?.Count > 0) {
                currentDbRow = this[reference.LogicalName].FirstOrDefault(row => reference.KeyAttributes.All(kv => row[kv.Key] == kv.Value));

                if (currentDbRow == null) {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with key attributes '{reference.KeyAttributes.ToPrettyString()}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }
#endif
            // No identification given for the entity, throw error
            else {
                throw new FaultException($"Missing a form of identification for the desired record in order to retrieve it.");
            }

            return currentDbRow;
        }


        internal DbRow GetDbRow(Entity xrmEntity) {
            return GetDbRow(xrmEntity.ToEntityReferenceWithKeyAttributes());
        }


        internal DbRow GetDbRow(string logicalName, Guid id) {
            return GetDbRow(new EntityReference(logicalName, id));
        }

        public Entity GetEntity(string logicalName, Guid id) {
            return GetDbRow(logicalName, id).ToEntity();
        }

        public Entity GetEntity(EntityReference reference) {
            return GetDbRow(reference).ToEntity();
        }


        #region GetOrNull
        internal DbRow GetDbRowOrNull(EntityReference reference) {
            try {
                return GetDbRow(reference);
            } catch (Exception) {
                return null;
            }
        }

        internal Entity GetEntityOrNull(EntityReference reference) {
            try {
                return GetEntity(reference);
            } catch (Exception) {
                return null;
            }
        }
        #endregion

    }
}
