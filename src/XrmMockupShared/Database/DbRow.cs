using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DG.Tools.XrmMockup.Database {

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
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
        
        public Dictionary<string, AttributeMetadata> AttributeMetadata {
            get {
                return Table.AttributeMetadata;
            }
        }

        protected string DebuggerDisplay {
            get {
                string display = Table.TableName;

                var name = GetColumn<string>(Metadata.PrimaryNameAttribute);
                if (!string.IsNullOrEmpty(name)) display += $" ({name})";
                if (Id != Guid.Empty) display += $" [{Id}]";

                return display;
            }
        }

        public AttributeMetadata GetAttributeMetadata(string key) {
            if (!AttributeMetadata.TryGetValue(key, out AttributeMetadata attrMetadata))
                throw new MockupException($"Unable to find attribute with the logical name '{key}' in metadata for '{Table.TableName}'");
            return attrMetadata;
        }


        public DbRow(DbTable table, Guid id, IEnumerable<KeyValuePair<string, object>> columns) {
            this.Table = table;
            this.Id = id;

            if (columns != null) {
                foreach (var col in columns) this[col.Key] = col.Value;
            }
        }

        public object this[string colName] {
            get {
                Columns.TryGetValue(colName, out object val);
                if (val is DbRow related && related.IsDeleted) {
                    Columns.Remove(colName);
                    return null;
                }
                return val;
            }
            set {
                if (ValidKeyAndValue(colName, value)) {
                    Columns[colName] = value;
                }
            }
        }

        public T GetColumn<T>(string colName) {
            return (T)this[colName];
        }

        public object GetColumn(string colName) {
            return this[colName];
        }

        public bool ColumnIsSet(string colName) {
            return this[colName] != null;
        }

        public bool ValidKeyAndValue(string key, object value) {
            if (value == null) return true;
            return DbAttributeTypeHelper.IsValidType(GetAttributeMetadata(key), value);
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
            var xrmEntity = new Entity(Table.TableName) {
                Id = Id
            };

            // Prune deleted references
            this.PruneDeletedReferences();

            var attributes = Columns.Select(ConvertToXrmKeyValue);
            xrmEntity.Attributes.AddRange(attributes);

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            var keys = Keys.Select(ConvertToXrmKeyValue);
            xrmEntity.KeyAttributes.AddRange(keys);
#endif
            return xrmEntity;
        }

        internal static DbRow FromEntity(Entity xrmEntity, XrmDb db, bool withReferenceChecks = true) {
            IEnumerable<KeyValuePair<string, object>> columns = xrmEntity.Attributes;

            if (withReferenceChecks == true) { // Convert EntityReferences to actual references if db is given
                columns = columns.Select(a => ConvertToDbKeyValue(a, db));
            }

            return new DbRow(db[xrmEntity.LogicalName], xrmEntity.Id, columns);
        }

        internal static KeyValuePair<string, object> ConvertToDbKeyValue(KeyValuePair<string, object> xrmAttribute, XrmDb db) {
            return new KeyValuePair<string, object>(xrmAttribute.Key, ConvertToDbValue(xrmAttribute.Value, db));
        }

        internal static object ConvertToDbValue(object value, XrmDb db) {
            if (value is OptionSetValue osv) {
                return osv.Value;
            }
            if (value is Money money) {
                return money.Value;
            }
            if (value is EntityReference reference) {
                return db.GetDbRow(reference);
            }
            return value;
        }

        internal KeyValuePair<string, object> ConvertToXrmKeyValue(KeyValuePair<string, object> col) {
            return new KeyValuePair<string, object>(col.Key, ConvertToXrmValue(col.Value, GetAttributeMetadata(col.Key)));
        }

        internal static object ConvertToXrmValue(object dbValue, AttributeMetadata attrMetadata) {
            if (attrMetadata is EnumAttributeMetadata && dbValue is int dbValueInt) {
                return new OptionSetValue(dbValueInt);
            }
            if (attrMetadata is LookupAttributeMetadata && dbValue is DbRow dbValueRow) {
                return dbValueRow.ToXrmEntityReference();
            }
            if (attrMetadata is MoneyAttributeMetadata && dbValue is decimal dbValueDecimal) {
                return new Money(dbValueDecimal);
            }
            if (attrMetadata is IntegerAttributeMetadata ||
                attrMetadata is BigIntAttributeMetadata) {
                return (int?)dbValue;
            }
            return dbValue;
        }


        public EntityReference ToXrmEntityReference() {
            return new EntityReference(Table.TableName, Id);
        }

    }
}
