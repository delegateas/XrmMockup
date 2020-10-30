using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace XrmMockupShared.Database
{
    class SQLDb : IXrmDb
    {

        private Dictionary<string, EntityMetadata> EntityMetadata;
        private string ConnectionString;

        public SQLDb(Dictionary<string, EntityMetadata> entityMetadata, string connectionString,bool recreateDatabase)
        {
            this.EntityMetadata = entityMetadata;
            this.ConnectionString = connectionString;

            if (recreateDatabase)
            {
                InitialiseDatabase();
            }

        }

        private void InitialiseDatabase()
        {
            foreach (var entityMeta in this.EntityMetadata)
            {
                CreateTable(entityMeta);
            }
        }

        private void DropTableIfExists(string tableName)
        {

            string sql = $"IF (OBJECT_ID('{tableName}') IS NOT NULL )" +
                         "BEGIN " +
                         $"DROP TABLE {tableName} " +
                         "END";

            ExecuteNonQuery(sql);
        }

        private void CreateTable(KeyValuePair<string, EntityMetadata> entityMeta)
        {
            DropTableIfExists(entityMeta.Key);

            var createSQL = $"create table [{entityMeta.Key}] ( ";

            foreach (var attr in entityMeta.Value.Attributes.Where(x => x.AttributeType != AttributeTypeCode.Virtual && x.AttributeType != AttributeTypeCode.EntityName && x.AttributeType != AttributeTypeCode.ManagedProperty))
            {
                createSQL += $" [{attr.LogicalName}] {GetFieldDataType(attr)}, ";
            }

            createSQL = createSQL.Trim().TrimEnd(new char[] { ',' });
            createSQL += " )";

            ExecuteNonQuery(createSQL);
        }

        private string GetFieldDataType(AttributeMetadata attr)
        {
            switch (attr.AttributeType)
            {

                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.Customer:
                    return "uniqueidentifier";
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.Picklist:
                    return "int";
                case AttributeTypeCode.DateTime:
                    return "datetime";
                case AttributeTypeCode.BigInt:
                    return "bigint";
                case AttributeTypeCode.Boolean:
                    return "bit";
                case AttributeTypeCode.String:

                    var stringAttr = attr as StringAttributeMetadata;
                    if (stringAttr.MaxLength > 8000)
                    {
                        return $"nvarchar (max)";
                    }
                    else
                    {
                        return $"nvarchar ({(attr as StringAttributeMetadata).MaxLength.ToString()})";
                    }

                case AttributeTypeCode.PartyList:
                    return $"nvarchar (500)";
                case AttributeTypeCode.Memo:
                    return $"nvarchar (MAX)";
                case AttributeTypeCode.Money:
                    return $"money";
                case AttributeTypeCode.Decimal:
                    return $"decimal (20,{(attr as DecimalAttributeMetadata).Precision.ToString()})";
                case AttributeTypeCode.Double:
                    return $"float";

                default:
                    return "";
            }

        }

        private DbTable GetTable(string tableName)
        {
            var dataTable = ExecuteReader($"SELECT * FROM [{tableName}]");
            
            var dbTable = new DbTable(this.EntityMetadata[tableName]);

            foreach (var row in dataTable.Rows)
            { 
                //var dbRow = new DbRow()
            }

            return dbTable;

        }

        private DataTable ExecuteReader(string sql)
        {
            using (var conn = new SqlConnection(this.ConnectionString))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    SqlDataReader reader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                    
                }
            }
        }

        private void ExecuteNonQuery(string sql, List<SqlParameter> paramValues = null)
        {
            using (var conn = new SqlConnection(this.ConnectionString))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    if (paramValues != null)
                    {
                        cmd.Parameters.AddRange(paramValues.ToArray());
                    }
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        public DbTable this[string tableName] => throw new NotImplementedException();

        public void Add(Entity xrmEntity, bool withReferenceChecks = true)
        {
            var entityMetadata = this.EntityMetadata[xrmEntity.LogicalName];
            var sqlAttributes = entityMetadata.Attributes.Where(x => x.AttributeType != AttributeTypeCode.Virtual && x.AttributeType != AttributeTypeCode.EntityName && x.AttributeType != AttributeTypeCode.ManagedProperty);

            
            string sqlFields = string.Empty;
            string sqlValues = string.Empty;
            var paramValues = new List<SqlParameter>();
            
            foreach (var attr in sqlAttributes)
            {
                if (xrmEntity.Contains(attr.LogicalName))
                {
                    sqlFields += $" [{attr.LogicalName}] ,";
                    sqlValues += $" @{attr.LogicalName} ,";
                    paramValues.Add(new SqlParameter($"@{attr.LogicalName}",GetSqlValue(xrmEntity,attr)));
                }
            }

            sqlFields = TrimTrailingComma(sqlFields);
            sqlValues = TrimTrailingComma(sqlValues);

            var insertSQL = $"insert into [{xrmEntity.LogicalName}] ({sqlFields}) VALUES ({sqlValues})";

            ExecuteNonQuery(insertSQL,paramValues);
        }

        private object GetSqlValue(Entity xrmEntity, AttributeMetadata attr)
        {
            switch (attr.AttributeType)
            {

                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.Customer:
                    if (xrmEntity[attr.LogicalName] is EntityReference)
                    {
                        return xrmEntity.GetAttributeValue<EntityReference>(attr.LogicalName).Id;
                    }
                    else
                    {
                        //this shouldnt actually happen...
                        return xrmEntity[attr.LogicalName];
                    }

                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Picklist:
                    return xrmEntity.GetAttributeValue<OptionSetValue>(attr.LogicalName).Value;
                default:
                    return (xrmEntity[attr.LogicalName]);

            }
        }

        private string TrimTrailingComma(string sql)
        {
            return sql.Trim().TrimEnd(new char[] { ',' });
        }

        private string GetFieldValue(Entity xrmEntity, AttributeMetadata attr)
        {
            switch (attr.AttributeType)
            {

                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.Customer:
                    return "uniqueidentifier";
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.Picklist:
                    return "int";
                case AttributeTypeCode.DateTime:
                    return "datetime";
                case AttributeTypeCode.BigInt:
                    return "bigint";
                case AttributeTypeCode.Boolean:
                    return "bit";
                case AttributeTypeCode.String:

                    var stringAttr = attr as StringAttributeMetadata;
                    if (stringAttr.MaxLength > 8000)
                    {
                        return $"nvarchar (max)";
                    }
                    else
                    {
                        return $"nvarchar ({(attr as StringAttributeMetadata).MaxLength.ToString()})";
                    }

                case AttributeTypeCode.PartyList:
                    return $"nvarchar (500)";
                case AttributeTypeCode.Memo:
                    return $"nvarchar (MAX)";
                case AttributeTypeCode.Money:
                    return $"money";
                case AttributeTypeCode.Decimal:
                    return $"decimal (20,{(attr as DecimalAttributeMetadata).Precision.ToString()})";
                case AttributeTypeCode.Double:
                    return $"float";

                default:
                    return "";
            }
        }

        public void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true)
        {
            foreach(var xrmEntity in xrmEntities)
            {
                Add(xrmEntity, withReferenceChecks);
            }
        }

        public IXrmDb Clone()
        {
            throw new NotImplementedException();
        }

        public void Delete(Entity xrmEntity)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DbRow> GetDBEntityRows(string teamMembership)
        {
            throw new NotImplementedException();
        }

        public DbRow GetDbRow(Entity entity)
        {
            throw new NotImplementedException();
        }

        public DbRow GetDbRow(EntityReference reference, bool v)
        {

            // var row = new DbRow()


            throw new NotImplementedException();
        }

        public DbRow GetDbRow(EntityReference entityReference)
        {
            throw new NotImplementedException();
        }

        public DbRow GetDbRowOrNull(EntityReference entityReference)
        {
            throw new NotImplementedException();
        }

        public Entity GetEntity(EntityReference reference)
        {
            throw new NotImplementedException();
        }

        public Entity GetEntity(string logicalName, Guid id)
        {
            throw new NotImplementedException();
        }

        public object GetEntityOrNull(EntityReference entityReference)
        {
            throw new NotImplementedException();
        }

        public bool HasRow(EntityReference entityReference)
        {
            throw new NotImplementedException();
        }

        public bool IsValidEntity(string logicalName)
        {
            throw new NotImplementedException();
        }

        public void PrefillDBWithOnlineData(QueryExpression queryExpr)
        {
            throw new NotImplementedException();
        }

        public DbRow ToDbRow(Entity xrmEntity, bool withReferenceChecks = true)
        {
            throw new NotImplementedException();
        }

        public void Update(Entity xrmEntity, bool withReferenceChecks = true)
        {
            throw new NotImplementedException();
        }

        Entity IXrmDb.GetEntityOrNull(EntityReference entityReference)
        {
            throw new NotImplementedException();
        }
    }
}
