using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace DG.Tools.XrmMockup.Database {
    internal class DbTable : IEnumerable<DbRow> {
        public string TableName { get; set; }
        public EntityMetadata Metadata { get; set; }
        private Dictionary<Guid, DbRow> MainDict = new Dictionary<Guid, DbRow>();

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

        public IEnumerator<DbRow> GetEnumerator() {
            return MainDict.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return MainDict.Values.GetEnumerator();
        }
    }
}

