using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;

namespace DG.Tools.XrmMockup.Database {
    internal class DbTable : IEnumerable<DbRow> {
        private Dictionary<Guid, DbRow> MainDict = new Dictionary<Guid, DbRow>();

        public string TableName { get; set; }
        public EntityMetadata Metadata { get; set; }

        private Dictionary<string, AttributeMetadata> _attributeMetadata;
        public Dictionary<string, AttributeMetadata> AttributeMetadata {
            get {
                if (_attributeMetadata == null) _attributeMetadata = Metadata.Attributes.ToDictionary(x => x.LogicalName);
                return _attributeMetadata;
            }
        }

        public DbTable(EntityMetadata entityMetadata) {
            this.Metadata = entityMetadata;
            this.TableName = Metadata.LogicalName;
        }

        public DbRow this[Guid guid] {
            get {
                MainDict.TryGetValue(guid, out DbRow entity);
                return entity;
            }
            set {
                MainDict[guid] = value;
            }
        }

        public void Remove(Guid guid) {
            if (MainDict.TryGetValue(guid, out DbRow row)) {
                MainDict.Remove(guid);
                row.MarkAsDeleted();
            } else {
                throw new InvalidOperationException($"No record of type '{TableName}' exists with GUID={guid}");
            }
        }

        public IEnumerator<DbRow> GetEnumerator() {
            return MainDict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return MainDict.Values.GetEnumerator();
        }
    }
}

