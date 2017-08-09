using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DG.Tools.XrmMockup.Database {

    internal class DbRow {

        public DbTable Table { get; }
        public Guid Id { get; set; }
        public bool IsDeleted { get; private set; } = false;

        private Dictionary<string, object> Columns = new Dictionary<string, object>();
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        private Dictionary<string, object> Keys = new Dictionary<string, object>();
#endif

        public EntityMetadata Metadata {
            get {
                return Table.Metadata;
            }
        }


        public DbRow(DbTable table, Guid id, IEnumerable<KeyValuePair<string, object>> columns) {
            this.Table = table;
            this.Id = id;

            if (columns != null) {
                foreach (var col in columns) this[col.Key] = col.Value;
            }
        }

        public object this[string attributeName] {
            get {
                Columns.TryGetValue(attributeName, out object val);
                if (val is DbRow related && related.IsDeleted) {
                    Columns.Remove(attributeName);
                    return null;
                }
                return val;
            }
            set {
                if (ValidKeyAndValue(attributeName, value)) {
                    Columns[attributeName] = value;
                }
            }
        }

        private bool ValidKeyAndValue(string key, object value) {
            // TODO: Add attribute metadata validation
            return true;
        }

        public void ApplyUpdates(IEnumerable<KeyValuePair<string, object>> columns) {
            foreach (var column in columns) this[column.Key] = column.Value;
        }


        private void PruneDeletedReferences() {
            foreach (var key in Columns.Keys.ToList()) {
                if (Columns[key] is DbRow related && related.IsDeleted) Columns.Remove(key);
            }
        }

        public void MarkAsDeleted() {
            this.IsDeleted = true;
        }

        public Entity ToEntity() {
            var xrmEntity = new Entity(Table.TableName);
            xrmEntity.Id = Id;

            // Convert references and prune afterwards if necessary
            this.PruneDeletedReferences();
            var attributes = Columns.Select(col => {
                if (col.Value is DbRow related) {
                    return new KeyValuePair<string, object>(col.Key, related.ToXrmEntityReference());
                }
                return col;
            });

            xrmEntity.Attributes.AddRange(attributes);

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            xrmEntity.KeyAttributes.AddRange(Keys);
#endif
            return xrmEntity;
        }

        internal static DbRow FromEntity(Entity xrmEntity, XrmDb db) {
            IEnumerable<KeyValuePair<string, object>> columns = xrmEntity.Attributes;

            if (db != null) { // Convert EntityReferences to actual references if db is given
                columns = columns.Select(a => ConvertToDbValue(a, db));
            }

            return new DbRow(db[xrmEntity.LogicalName], xrmEntity.Id, columns);
        }

        internal static KeyValuePair<string, object> ConvertToDbValue(KeyValuePair<string, object> xrmAttribute, XrmDb db) {
            if (xrmAttribute.Value is OptionSetValue osv) {
                return new KeyValuePair<string, object>(xrmAttribute.Key, osv.Value);
            }
            if (xrmAttribute.Value is Money money) {
                return new KeyValuePair<string, object>(xrmAttribute.Key, money.Value);
            }
            if (xrmAttribute.Value is EntityReference reference) {
                return new KeyValuePair<string, object>(xrmAttribute.Key, db.GetDbRow(reference));
            }
            return xrmAttribute;
        }

        public EntityReference ToXrmEntityReference() {
            return new EntityReference(Table.TableName, Id);
        }

    }
}
