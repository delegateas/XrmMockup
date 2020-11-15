using DG.Tools.XrmMockup;
using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace XrmMockupShared.Database
{
    class SQLDb : IXrmDb
    {

        private Dictionary<string, EntityMetadata> EntityMetadata;
        private string ConnectionString;
        private string[] RetainTables;

        public SQLDb(Dictionary<string, EntityMetadata> entityMetadata, string connectionString,bool recreateDatabase,string[] retainTables)
        {
            this.EntityMetadata = entityMetadata;
            this.ConnectionString = connectionString;
            this.RetainTables = retainTables;
            if (RetainTables == null)
                RetainTables = new string[0];

            if (recreateDatabase)
            {
                InitialiseDatabase();
            }
            
        }

        public void ResetAccessTeams()
        {
            string sql = "delete from team where teamtype = 1;delete from teammembership";
            ExecuteNonQuery(sql);
        }

        private void InitialiseDatabase()
        {
            foreach (var entityMeta in this.EntityMetadata.Where(x=> !RetainTables.Contains(x.Key)))
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

            var primaryAttribute = entityMeta.Value.Attributes.Where(x => x.LogicalName == entityMeta.Value.PrimaryIdAttribute).Single();

            var createIndex = string.Empty;

            var createSQL = $"create table [{entityMeta.Key}] ( ";

            foreach (var attr in entityMeta.Value.Attributes
                                                    .Where(x => x.AttributeType != AttributeTypeCode.Virtual
                                                             && x.AttributeType != AttributeTypeCode.EntityName
                                                             && x.AttributeType != AttributeTypeCode.ManagedProperty
                                                             && x.AttributeOf == null).OrderBy(x => x.LogicalName))
            {
                createSQL += $" [{attr.LogicalName}] {GetFieldDataType(attr)}, ";

                if (attr.LogicalName == primaryAttribute.LogicalName)
                {
                    createSQL += $"CONSTRAINT PK_{entityMeta.Key}_{attr.LogicalName} PRIMARY KEY CLUSTERED({attr.LogicalName}),";
                }

                //, 

                if (attr.AttributeType == AttributeTypeCode.Customer || attr.AttributeType == AttributeTypeCode.Lookup || attr.AttributeType == AttributeTypeCode.Owner || GetFieldDataType(attr) == "uniqueidentifier")
                {
                    createIndex += $"CREATE INDEX ix_{attr.LogicalName} ON {entityMeta.Key} ({attr.LogicalName});";
                }


            }

            createSQL = createSQL.Trim().TrimEnd(new char[] { ',' });
            createSQL += " )";

            ExecuteNonQuery(createSQL);
            ExecuteNonQuery(createIndex);
        }

        private void CreateTable(string tableName)
        {
            DropTableIfExists(tableName);

            var createSQL = $"create table [{tableName}] ( ";
            createSQL += $" [{tableName}id] uniqueidentifier, ";
            createSQL += $"CONSTRAINT PK_{tableName}_{tableName}2id PRIMARY KEY CLUSTERED({tableName}id),";
            createSQL += $" [name] nvarchar(250), ";
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
                    return $"nvarchar (1500)";
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

        private DataTable ExecuteReader(string sql, List<SqlParameter> paramValues = null)
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
                    SqlDataReader reader = cmd.ExecuteReader();

                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                    
                }
            }
        }

        private void ExecuteNonQuery(string sql, List<SqlParameter> paramValues = null)
        {

            if (string.IsNullOrEmpty(sql))
            {
                return;
            }

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

            var primaryAttributes = sqlAttributes.Where(x => x.IsPrimaryId.Value);
            if (primaryAttributes.Count() > 1)
            {
                primaryAttributes = primaryAttributes.Where(x => x.LogicalName == xrmEntity.LogicalName + "id");
            }
            var primaryAttribute = primaryAttributes.SingleOrDefault();

            if (xrmEntity.Id == null || xrmEntity.Id == Guid.Empty)
            {
                xrmEntity[primaryAttribute.LogicalName] = Guid.NewGuid();
                xrmEntity.Id = (Guid) xrmEntity[primaryAttribute.LogicalName];
            }
            if (!xrmEntity.Contains(primaryAttribute.LogicalName) || xrmEntity[primaryAttribute.LogicalName] == null)
            {
            
            }

            var sqlFields = new List<string>();
            var sqlValues = new List<string>();
            var paramValues = new List<SqlParameter>();

            sqlFields.Add($"[{primaryAttribute.LogicalName}]");
            sqlValues.Add($"@{primaryAttribute.LogicalName}");
            paramValues.Add(new SqlParameter($"@{primaryAttribute.LogicalName}", xrmEntity.Id));

            //string sqlFields = string.Empty;
            //string sqlValues = string.Empty;

            foreach (var attr in sqlAttributes.Except(new List<AttributeMetadata>() { primaryAttribute }))
            {
                if (xrmEntity.Contains(attr.LogicalName))
                {
                    sqlFields.Add($"[{attr.LogicalName}]");
                    sqlValues.Add($"@{attr.LogicalName}");
                    paramValues.Add(new SqlParameter($"@{attr.LogicalName}", GetSqlValue(xrmEntity, attr)));
                }
            }

            var fieldSQL = string.Join(",", sqlFields);
            var valueSQL = string.Join(",", sqlValues);
            
            var insertSQL = $"insert into [{xrmEntity.LogicalName}] ({fieldSQL}) VALUES ({valueSQL})";

            try
            {
                ExecuteNonQuery(insertSQL, paramValues);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot insert duplicate key"))
                {
                    throw new FaultException(ex.Message);
                }
                else
                {
                    throw;
                }
            }

            
        }

        private object GetSqlValue(Entity xrmEntity, AttributeMetadata attr)
        {

            if (xrmEntity[attr.LogicalName] == null)
            {
                return DBNull.Value;
            }
            else
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
                    case AttributeTypeCode.Money:
                        return xrmEntity.GetAttributeValue<Money>(attr.LogicalName).Value;
                    case AttributeTypeCode.DateTime:
                        if (xrmEntity.GetAttributeValue<DateTime>(attr.LogicalName) == null
                             ||
                             xrmEntity.GetAttributeValue<DateTime>(attr.LogicalName) == DateTime.MinValue)
                        {
                            return DBNull.Value;
                        }
                        else
                        {
                            return (xrmEntity[attr.LogicalName]);
                        }
                    case AttributeTypeCode.PartyList:

                        string field = string.Empty;
                        if (xrmEntity[attr.LogicalName] is EntityReference)
                        {
                            field += $"{(xrmEntity[attr.LogicalName] as EntityReference).LogicalName};{(xrmEntity[attr.LogicalName] as EntityReference).Id.ToString()}";
                        }
                        else
                        {
                            foreach (var e in (xrmEntity[attr.LogicalName] as EntityCollection).Entities)
                            {
                                if (e.LogicalName == "systemuser")
                                {
                                    field += $"{e.LogicalName};{e.Id.ToString()},";
                                }
                                else
                                {
                                    var party = e.Attributes["partyid"] as EntityReference;
                                    field += $"{party.LogicalName};{party.Id.ToString()},";
                                }


                            }
                        }
                        field = TrimTrailingComma(field);

                        return field;

                    default:
                        return (xrmEntity[attr.LogicalName]);
                }
            }
        }

        private string TrimTrailingComma(string sql)
        {
            return sql.Trim().TrimEnd(new char[] { ',' });
        }
        private string TrimTrailingAND(string sql)
        {
            return sql.Trim().TrimEnd(new char[] { 'A', 'N', 'D' });
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
            throw new NotSupportedException();
        }

        public void Delete(Entity xrmEntity)
        {
            var metaData = EntityMetadata[xrmEntity.LogicalName];

            var sql = $"delete from [{xrmEntity.LogicalName}] where [{metaData.PrimaryIdAttribute}] = '{xrmEntity.Id.ToString()}'";
            ExecuteReader(sql);
            
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

        private string GetSqlConditionOperator(ConditionOperator op)
        {
            switch (op)
            {
                case ConditionOperator.Like:
                    return " like ";
                case ConditionOperator.NotLike:
                    return " not like ";
                case ConditionOperator.Equal:
                    return " = ";
                case ConditionOperator.NotEqual:
                    return " <> ";
                case ConditionOperator.NotNull:
                    return " is not null ";
                case ConditionOperator.Null:
                    return " is null ";
                default:
                    return string.Empty;

            }
        }

        public IEnumerable<Entity> GetEntities(string tableName, IEnumerable<ConditionExpression> filters = null)
        {
            return InternalGetEntities(tableName, filters, true);
        }


        private IEnumerable<Entity> InternalGetEntities(string tableName, IEnumerable<ConditionExpression> filters = null, bool formatted = true)
        {

            var lookupAttributes = EntityMetadata[tableName]
                                        .Attributes
                                        .Where(x => (x is LookupAttributeMetadata)
                                                && !((x as LookupAttributeMetadata).AttributeType == AttributeTypeCode.PartyList)
                                                && !((x as LookupAttributeMetadata).LogicalName.ToLower() == "regardingobjectid"))
                                        .Select(x => x as LookupAttributeMetadata);

            var sql = $"select [{tableName}0].*, ";

            var sqlParams = new List<SqlParameter>();

            //left join to all the lookups to get the formatted value attributes
            string joins = GetLookupJoins(tableName, lookupAttributes, ref sql);
            sql = TrimTrailingComma(sql);
            sql += $" from [{tableName}] as [{tableName}0] ";
            sql += joins;

            if (filters != null && filters.Count() > 0)
            {
                filters = filters.Where(x => !string.IsNullOrEmpty(x.AttributeName));
                filters = filters.Where(x => !string.IsNullOrEmpty(GetSqlConditionOperator(x.Operator)));

                if (filters.Any())
                {
                    sql += " where ";

                    foreach (var filter in filters)
                    {
                        if (filter.Operator == ConditionOperator.NotNull || filter.Operator == ConditionOperator.Null)
                        {
                            sql += $" [{tableName}0].[{filter.AttributeName}] {GetSqlConditionOperator(filter.Operator)} AND";
                        }
                        else
                        {
                            sql += $" [{tableName}0].[{filter.AttributeName}] {GetSqlConditionOperator(filter.Operator)} @{filter.AttributeName} AND";
                            sqlParams.Add(new SqlParameter($"@{filter.AttributeName}", filter.Values[0]));
                        }

                    }
                }
            }

            sql = TrimTrailingAND(sql);


            var data = ExecuteReader(sql, sqlParams);
            var entities = new List<Entity>();
            foreach (var row in data.Rows)
            {

                var toAdd = RowToEntity((DataRow)row, tableName);
                
                if (formatted)
                    SetFormattedValues(toAdd, (DataRow)row);

                entities.Add(toAdd);
            }
            return entities;
        }

        private string GetLookupJoins(string tableName, IEnumerable<LookupAttributeMetadata> lookupAttributes, ref string sql)
        {
            var joins = string.Empty;
            if (lookupAttributes != null)
            {
                int i = 0;
                foreach (var lookupAttr in lookupAttributes.Where(x=>x.Targets.Any()))
                {
                    i++;

                    if (this.EntityMetadata.ContainsKey(lookupAttr.Targets[0]))
                    {
                        if (lookupAttr.LogicalName.ToLower() == "regardingobjectid")
                        {
                            var a = 1;
                        }

                        sql += $"[{lookupAttr.Targets[0]}{i.ToString()}].[{this.EntityMetadata[lookupAttr.Targets[0]].PrimaryNameAttribute}] as {lookupAttr.LogicalName}_formatted , ";

                        joins += $" left join [{lookupAttr.Targets[0]}] as [{lookupAttr.Targets[0]}{i.ToString()}] ";
                        joins += $" on [{tableName}0].[{lookupAttr.LogicalName }]  = ";
                        joins += $" [{lookupAttr.Targets[0]}{i.ToString()}].[{this.EntityMetadata[lookupAttr.Targets[0]].PrimaryIdAttribute}] ";

                        if (lookupAttr.LogicalName == "transactioncurrencyid")
                        {
                            sql += $"[transactioncurrency_a_{i.ToString()}].[currencysymbol] as {lookupAttr.LogicalName}_currencysymbol_formatted , ";
                        
                            joins += $" left join [transactioncurrency] as [transactioncurrency_a_{i.ToString()}] ";
                            joins += $" on [{tableName}0].[{lookupAttr.LogicalName }]  = ";
                            joins += $" [transactioncurrency_a_{i.ToString()}].[transactioncurrencyid] ";

                        }

                    }
                }
            }

            return joins;
        }

        public Entity GetEntity(EntityReference reference)
        {
            return InternalGetEntity(reference, true);
        }

        private Entity GetUnformattedEntity(EntityReference reference)
        {
            return InternalGetEntity(reference, false);
        }

        public IEnumerable<Entity> GetUnformattedEntities(string tableName)
        {
            return InternalGetEntities(tableName,null, false);
        }

        private Entity InternalGetEntity(EntityReference reference, bool formatted)
        {

            var metaData = EntityMetadata[reference.LogicalName];
            var lookupAttributes = EntityMetadata[reference.LogicalName]
                                        .Attributes
                                        .Where(x => (x is LookupAttributeMetadata)
                                                && !((x as LookupAttributeMetadata).AttributeType == AttributeTypeCode.PartyList))
                                        .Select(x => x as LookupAttributeMetadata);

            DataRow row;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)

            if (reference.Id == null || reference.Id == Guid.Empty && reference.KeyAttributes.Any())
            {
                var sql = $"select * from [{reference.LogicalName}] where ";
                var sqlFields = new List<string>();
                var sqlParams = new List<SqlParameter>();

                foreach (var attr in reference.KeyAttributes)
                {
                    sqlFields.Add($"[{attr.Key}] = @{attr.Key}");
                    sqlParams.Add(new SqlParameter($"@{attr.Key}", attr.Value));
                }

                sql += string.Join(" and ", sqlFields);

                var data = ExecuteReader(sql,sqlParams);
                if (data.Rows.Count == 0)
                {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id.ToString()}' does not exist.");
                }
                row = data.Rows[0];
            }
            else
#endif
            {
                var sql = $"select [{reference.LogicalName}0].* , ";
                string joins = GetLookupJoins(reference.LogicalName, lookupAttributes, ref sql);
                sql = TrimTrailingComma(sql);
                sql += $" from [{reference.LogicalName}] as [{reference.LogicalName}0] ";
                sql += joins;
                sql += $" where [{reference.LogicalName}0].[{metaData.PrimaryIdAttribute}] = '{reference.Id.ToString()}'";
                var data = ExecuteReader(sql);
                if (data.Rows.Count == 0)
                {
                    throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id.ToString()}' does not exist.");
                }
                row = data.Rows[0];
            }

            var e = RowToEntity(row, reference.LogicalName);
            
            if (formatted)
                SetFormattedValues(e);
            
            return e;
        }

        internal void SetFormattedValues(Entity entity, DataRow row = null)
        {
            var validMetadata = this.EntityMetadata[entity.LogicalName].Attributes
                .Where(a => Utility.IsValidForFormattedValues(a));

            validMetadata = validMetadata.Except(validMetadata.Where(x => x.AttributeType == AttributeTypeCode.PartyList));

            var formattedValues = new List<KeyValuePair<string, string>>();
            foreach (var a in entity.Attributes)
            {
                if (a.Value == null) continue;
                var metadataAtt = validMetadata.Where(m => m.LogicalName == a.Key).FirstOrDefault();

                if (metadataAtt != null)
                {
                    if (metadataAtt is LookupAttributeMetadata)
                    {
                        if (entity[metadataAtt.LogicalName] is string && (string)entity[metadataAtt.LogicalName] == Guid.Empty.ToString())
                        {
                            //shouldnt happen as lookups should be entity references...
                            continue;
                        }

                        var entityRef = entity[metadataAtt.LogicalName] as EntityReference;
                        if (entityRef != null)
                        {

                            if (row != null && row.Table.Columns.Contains($"{metadataAtt.LogicalName}_formatted"))
                            {
                                if (row[$"{metadataAtt.LogicalName}_formatted"] != DBNull.Value)
                                {
                                    var LookupPair = new KeyValuePair<string, string>(a.Key, (string)row[$"{metadataAtt.LogicalName}_formatted"]);
                                    formattedValues.Add(LookupPair);

                                    (a.Value as EntityReference).Name = (string)row[$"{metadataAtt.LogicalName}_formatted"];

                                }
                            }

                        }

                    }

                    else if (metadataAtt is MoneyAttributeMetadata)
                    {
                        //{lookupAttr.LogicalName}_currencysymbol_formatted
                        var symbol = (string)row[$"transactioncurrencyid_currencysymbol_formatted"];
                        var value = (a.Value as Money).Value.ToString();

                        var formattedValuePair = new KeyValuePair<string, string>(a.Key, symbol + value);
                        formattedValues.Add(formattedValuePair);
                    }

                    else
                    {
                        var formattedValuePair = new KeyValuePair<string, string>(a.Key, GetFormattedValueLabel(metadataAtt, a.Value, entity));
                        if (formattedValuePair.Value != null)
                        {
                            formattedValues.Add(formattedValuePair);
                        }
                    }
                    
                }


            }

            if (formattedValues.Count > 0)
            {
                entity.FormattedValues.AddRange(formattedValues);
            }
        }

        internal string GetFormattedValueLabel(AttributeMetadata metadataAtt, object value, Entity entity)
        {
            if (metadataAtt is PicklistAttributeMetadata)
            {
                var optionset = (metadataAtt as PicklistAttributeMetadata).OptionSet.Options
                    .Where(opt => opt.Value == (value as OptionSetValue).Value).FirstOrDefault();
                return optionset.Label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is BooleanAttributeMetadata)
            {
                var booleanOptions = (metadataAtt as BooleanAttributeMetadata).OptionSet;
                var label = (bool)value ? booleanOptions.TrueOption.Label : booleanOptions.FalseOption.Label;
                return label.UserLocalizedLabel.Label;
            }

            

            if (metadataAtt is IntegerAttributeMetadata ||
                metadataAtt is DateTimeAttributeMetadata ||
                metadataAtt is MemoAttributeMetadata ||
                metadataAtt is DoubleAttributeMetadata ||
                metadataAtt is DecimalAttributeMetadata)
            {
                return value.ToString();
            }

            return null;
        }

        private Entity RowToEntity(DataRow row,string logicalName)
        {
            var entity = new Entity(logicalName);
            
            var primaryAttributes = this.EntityMetadata[logicalName].Attributes.Where(x => x.IsPrimaryId.Value);
            if (primaryAttributes.Count() > 1)
            {
                primaryAttributes = primaryAttributes.Where(x => x.LogicalName == logicalName + "id");
            }
            var primaryAttribute = primaryAttributes.SingleOrDefault();
            entity.Id = (Guid)row[primaryAttribute.LogicalName];

            EntityReference owner = null;
            if (row.Table.Columns.Contains("owningteam") && row["owningteam"] != DBNull.Value)
            {
                owner = new EntityReference("team", (Guid)row["owningteam"]);
            }
            else if (row.Table.Columns.Contains("ownerid") && row["ownerid"] != DBNull.Value)
            {
                owner = new EntityReference("systemuser", (Guid)row["ownerid"]); 
            }

            if (owner != null)
                entity["ownerid"] = owner;

            foreach (var attr in this.EntityMetadata[logicalName].Attributes.Where(x=>x.LogicalName != "ownerid"))
            {
                if (row.Table.Columns.Contains(attr.LogicalName))
                {
                    if (row[attr.LogicalName] != System.DBNull.Value)
                    {
                        entity[attr.LogicalName] = GetDynamicsValue(row, attr);
                    }

                }
            }

            return entity;
        }

        private object GetDynamicsValue(DataRow row, AttributeMetadata attr)
        {
            switch (attr.AttributeType)
            {

                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.Customer:
                    return new EntityReference((attr as LookupAttributeMetadata).Targets[0], (Guid)row[attr.LogicalName]);

                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.Picklist:
                    return new OptionSetValue((int)row[attr.LogicalName]);
                case AttributeTypeCode.Money:
                    return new Money((decimal)row[attr.LogicalName]);
                case AttributeTypeCode.PartyList:

                    var partyList = new List<Entity>();

                    foreach (var p in (row[attr.LogicalName] as string).Split(new char[] { ',' }))
                    {
                        var parts = p.Split(new char[] { ';' });
                        var er = new EntityReference(parts[0], Guid.Parse(parts[1]));
                        var ap = new Entity("activityparty");
                        ap["partyid"] = er;
                        partyList.Add(ap);
                    }

                    return partyList;
                default:
                    return (row[attr.LogicalName]);

            }
        }

        public Entity GetEntity(string logicalName, Guid id)
        {
            return GetEntity(new EntityReference(logicalName, id));
        }


        public bool HasRow(EntityReference entityReference)
        {
            var metaData = EntityMetadata[entityReference.LogicalName];

            var sql = $"select * from [{entityReference.LogicalName}] where [{metaData.PrimaryIdAttribute}] = '{entityReference.Id.ToString()}'";
            var data = ExecuteReader(sql);
            return data.Rows.Count > 0;
        }

        public bool IsValidEntity(string logicalName)
        {
            throw new NotImplementedException();
        }

        public void PrefillDBWithOnlineData(QueryExpression queryExpr)
        {
            
        }

        public DbRow ToDbRow(Entity xrmEntity, bool withReferenceChecks = true)
        {
            throw new NotImplementedException();
        }

        public void Update(Entity xrmEntity, bool withReferenceChecks = true)
        {
            var entityMetadata = this.EntityMetadata[xrmEntity.LogicalName];
            var sqlAttributes = entityMetadata.Attributes.Where(x => x.AttributeType != AttributeTypeCode.Virtual && x.AttributeType != AttributeTypeCode.EntityName && x.AttributeType != AttributeTypeCode.ManagedProperty);

            var primaryAttributes = sqlAttributes.Where(x => x.IsPrimaryId.Value);
            if (primaryAttributes.Count() > 1)
            {
                primaryAttributes = primaryAttributes.Where(x => x.LogicalName == xrmEntity.LogicalName + "id");
            }
            var primaryAttribute = primaryAttributes.SingleOrDefault();

            var sqlFields = new List<string>();
            var paramValues = new List<SqlParameter>();
            
            foreach (var attr in sqlAttributes.Except(new List<AttributeMetadata>() { primaryAttribute }))
            {
                if (xrmEntity.Contains(attr.LogicalName))
                {

                    sqlFields.Add($"[{attr.LogicalName}] = @{ attr.LogicalName}");
                    paramValues.Add(new SqlParameter($"@{attr.LogicalName}", GetSqlValue(xrmEntity, attr)));
                }
            }

            var fieldSQL = string.Join(",", sqlFields);
            
            var insertSQL = $"update [{xrmEntity.LogicalName}] set {fieldSQL} where {primaryAttribute.LogicalName} = '{xrmEntity.Id.ToString()}'";

            try
            {
                ExecuteNonQuery(insertSQL, paramValues);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot insert duplicate key"))
                {
                    throw new FaultException(ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        public Entity GetEntityOrNull(EntityReference reference)
        {
            var metaData = EntityMetadata[reference.LogicalName];
            DataRow row;

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)

            if (reference.Id == null || reference.Id == Guid.Empty && reference.KeyAttributes.Any())
            {
                var sql = $"select * from [{reference.LogicalName}] where ";
                var sqlFields = new List<string>();
                var sqlParams = new List<SqlParameter>();

                foreach (var attr in reference.KeyAttributes)
                {
                    sqlFields.Add($"[{attr.Key}] = @{attr.Key}");
                    sqlParams.Add(new SqlParameter($"@{attr.Key}", attr.Value));
                }

                sql += string.Join(" and ", sqlFields);

                var data = ExecuteReader(sql,sqlParams);
                if (data.Rows.Count == 0)
                {
                    return null;
                }
                row = data.Rows[0];
                return RowToEntity(row, reference.LogicalName);
            }
            else
#endif
            {

                if (reference.Id == Guid.Empty)
                {
                    return null;
                }

                var sql = $"select * from {reference.LogicalName} where {metaData.PrimaryIdAttribute} = '{reference.Id.ToString()}'";
                var data = ExecuteReader(sql);
                if (data.Rows.Count == 0)
                {
                    return null;
                }
                else
                {
                    row = data.Rows[0];
                    return RowToEntity(row, reference.LogicalName);
                }

                

            }
        }

        public IEnumerable<Entity> GetCallerTeamMembership(Guid callerId)
        {
            var sql = "select tm.*,t.teamtype as team_teamtype from teammembership tm left join team t on tm.teamid = t.teamid where tm.systemuserid = @systemuserid";
            var sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@systemuserid", callerId));
            var rows = ExecuteReader(sql, sqlParams);

            var entities = new List<Entity>();

            foreach (var row in rows.Rows)
            {
                var e = RowToEntity((DataRow)row, "teammembership");
                e.Attributes["team_teamtype"] = (row as DataRow)["team_teamtype"];
                entities.Add(e); 
            }

            return entities;
        }
    }
}
