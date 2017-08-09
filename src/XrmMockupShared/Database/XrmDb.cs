using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Metadata;

namespace DG.Tools.XrmMockup.Database {

    internal class XrmDb {
        private Dictionary<string, DbTable> TableDict = new Dictionary<string, DbTable>();
        private Dictionary<string, EntityMetadata> EntityMetadata;

        public XrmDb(Dictionary<string, EntityMetadata> entityMetadata) {
            this.EntityMetadata = entityMetadata;
        }

        public DbTable this[string TableName] {
            get {
                if (!TableDict.ContainsKey(TableName) && EntityMetadata.ContainsKey(TableName)) TableDict[TableName] = new DbTable(EntityMetadata[TableName]);
                return TableDict[TableName];
            }
        }

        public void Add(Entity xrmEntity, bool withReferenceChecks = true) {
            var dbEntity = DbRow.FromEntity(xrmEntity, withReferenceChecks ? this : null);
            if (dbEntity.Id != Guid.Empty) {
                if (this[dbEntity.Table.TableName][dbEntity.Id] != null) {
                    throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}' and id '{xrmEntity.Id}', but a record already exists with that Id.");
                }
            } else {
                dbEntity.Id = Guid.NewGuid();
            }

            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }

        public void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true) {
            foreach (var xrmEntity in xrmEntities) Add(xrmEntity, withReferenceChecks);
        }


        public void Update(Entity xrmEntity, bool withReferenceChecks = true) {
            var currentDbRow = GetDbRow(xrmEntity);

            var dbEntity = DbRow.FromEntity(xrmEntity, withReferenceChecks ? this : null);
            this[dbEntity.Table.TableName][dbEntity.Id] = dbEntity;
        }



        internal DbRow GetDbRow(EntityReference reference) {
            DbRow currentDbRow = null;

            if (reference.Id != Guid.Empty) {
                currentDbRow = TableDict[reference.LogicalName][reference.Id];
                if (currentDbRow == null) {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id}' " +
                        "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");
                }
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            // Try fetching with key attributes if any
            else if (reference.KeyAttributes?.Count > 0) {
                currentDbRow = TableDict[reference.LogicalName].FirstOrDefault(row => reference.KeyAttributes.All(kv => row[kv.Key] == kv.Value));

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

        internal DbRow GetDbRowOrNull(Entity xrmEntity) {
            try {
                return GetDbRow(xrmEntity);
            } catch (Exception) {
                return null;
            }
        }

        internal DbRow GetDbRowOrNull(string logicalName, Guid id) {
            try {
                return GetDbRow(logicalName, id);
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

        internal Entity GetEntityOrNull(string logicalName, Guid id) {
            try {
                return GetEntity(logicalName, id);
            } catch (Exception) {
                return null;
            }
        }
        #endregion

    }
}
