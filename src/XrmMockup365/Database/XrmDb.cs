using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System.Threading;
using DG.Tools.XrmMockup.Serialization;
using DG.Tools.XrmMockup.Internal;

namespace DG.Tools.XrmMockup.Database {

    internal class XrmDb {
        // Using ConcurrentDictionary for thread-safe table access in parallel test scenarios
        private ConcurrentDictionary<string, DbTable> TableDict = new ConcurrentDictionary<string, DbTable>();
        private Dictionary<string, EntityMetadata> EntityMetadata;
        private OrganizationServiceProxy OnlineProxy;
        private int sequence;

        public XrmDb(Dictionary<string, EntityMetadata> entityMetadata, OrganizationServiceProxy onlineProxy) {
            this.EntityMetadata = entityMetadata;
            this.OnlineProxy = onlineProxy;
            sequence = 0;
        }

        public DbTable this[string tableName] {
            get {
                return TableDict.GetOrAdd(tableName, name => {
                    if (!EntityMetadata.TryGetValue(name, out EntityMetadata entityMetadata)) {
                        throw new MockupException($"No EntityMetadata found for entity with logical name '{name}'.");
                    }
                    return new DbTable(entityMetadata);
                });
            }
        }

        public void Add(Entity xrmEntity, bool withReferenceChecks = true) 
        {
            int nextSequence = Interlocked.Increment(ref sequence);
            var dbEntity = ToDbRow(xrmEntity,nextSequence, withReferenceChecks);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public DbRow ToDbRow(Entity xrmEntity, int sequence, bool withReferenceChecks = true)
        {
            var primaryIdKey = this[xrmEntity.LogicalName].Metadata.PrimaryIdAttribute;
            if (!xrmEntity.Attributes.ContainsKey(primaryIdKey))
            {
                xrmEntity[primaryIdKey] = xrmEntity.Id;
            }

            var dbEntity = DbRow.FromEntity(xrmEntity, this, withReferenceChecks);
            dbEntity.Sequence = sequence;
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

        internal DbRow GetDbRow(EntityReference reference, bool withReferenceCheck = true)
        {
            DbRow currentDbRow = null;

            if (reference?.Id != Guid.Empty)
            {
                currentDbRow = this[reference.LogicalName][reference.Id];
                if (currentDbRow == null && OnlineProxy != null)
                {
                    if (!withReferenceCheck)
                        currentDbRow = DbRow.MakeDBRowRef(reference, this);
                    else
                    {
                        var onlineEntity = OnlineProxy.Retrieve(reference.LogicalName, reference.Id, new ColumnSet(true));
                        Add(onlineEntity, withReferenceCheck);
                        currentDbRow = this[reference.LogicalName][reference.Id];
                    }
                }
                if (currentDbRow == null)
                {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

            // Try fetching with key attributes if any
            else if (reference?.KeyAttributes?.Count > 0) {
                // Use ToList to create a snapshot for thread-safe enumeration
                currentDbRow = this[reference.LogicalName].ToList().FirstOrDefault(row => reference.KeyAttributes.All(kv => row[kv.Key] == kv.Value));

                if (currentDbRow == null) {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with key attributes '{reference.KeyAttributes.ToPrettyString()}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

            // No identification given for the entity, throw error
            else
            {
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

        internal bool TryGetDbRow(EntityReference reference, out DbRow dbRow)
        {
            DbRow currentDbRow = null;
            dbRow = null;

            if (reference?.Id != default && reference?.Id != Guid.Empty)
            {
                currentDbRow = this[reference.LogicalName][reference.Id];
                if (currentDbRow == null)
                {
                    return false;
                }
                else
                {
                    dbRow = currentDbRow;
                    return true;
                }
            }

            // Try fetching with key attributes if any
            else if (reference?.KeyAttributes?.Count > 0)
            {
                // Use ToList to create a snapshot for thread-safe enumeration
                currentDbRow = this[reference.LogicalName].ToList().FirstOrDefault(row => reference.KeyAttributes.All(kv => row[kv.Key] == kv.Value));

                if (currentDbRow == null)
                {
                    return false;
                }
                else
                {
                    dbRow = currentDbRow;
                    return true;
                }
            }

            // No identification given for the entity, return false
            else
            {
                return false;
            }
        }

        internal DbRow GetDbRowOrNull(EntityReference reference)
        {
            DbRow row;
            if (TryGetDbRow(reference, out row))
            {
                return row;
            }
            else
            {
                return null;
            }
        }

        internal Entity GetEntityOrNull(EntityReference reference)
        {
            DbRow row;
            if (TryGetDbRow(reference, out row))
            {
                return row.ToEntity();
            }
            else
            {
                return null;
            }
        }

        #endregion

        public XrmDb Clone()
        {
            var clonedTables = this.TableDict.ToDictionary(x => x.Key, x => x.Value.Clone());
            var clonedDB = new XrmDb(this.EntityMetadata, this.OnlineProxy)
            {
                TableDict = new ConcurrentDictionary<string, DbTable>(clonedTables)
            };

            return clonedDB;
        }
        public DbDTO ToSerializableDTO()
        {
            var jsonObj = new DbDTO
            {
                Tables = this.TableDict.ToDictionary(x => x.Key, x => x.Value.ToSerializableDTO())
            };
            return jsonObj;
        }
        public static XrmDb RestoreSerializableDTO(XrmDb current, DbDTO model)
        {
            var clonedTables = model.Tables.ToDictionary(x => x.Key, x => DbTable.RestoreSerializableDTO(new DbTable(current.EntityMetadata[x.Key]), x.Value));
            var clonedDB = new XrmDb(current.EntityMetadata, current.OnlineProxy)
            {
                TableDict = new ConcurrentDictionary<string, DbTable>(clonedTables)
            };

            foreach (var table in clonedTables)
            {
                table.Value.RestoreFromDTOPostProcess(clonedDB);
            }

            return clonedDB;
        }

        internal void ResetTable(string tableName)
        {
            TableDict[tableName] = new DbTable(this.EntityMetadata[tableName]);
        }
    }
}
