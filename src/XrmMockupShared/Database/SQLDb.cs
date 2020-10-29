using DG.Tools.XrmMockup.Database;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace XrmMockupShared.Database
{
    class SQLDb : IXrmDb
    {
        public DbTable this[string tableName] => throw new NotImplementedException();

        public void Add(Entity xrmEntity, bool withReferenceChecks = true)
        {
            throw new NotImplementedException();
        }

        public void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true)
        {
            throw new NotImplementedException();
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
