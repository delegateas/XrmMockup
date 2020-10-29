using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Database
{
    internal interface IXrmDb
    {
        DbTable this[string tableName] { get; }

        void Add(Entity xrmEntity, bool withReferenceChecks = true);
        void AddRange(IEnumerable<Entity> xrmEntities, bool withReferenceChecks = true);
        IXrmDb Clone();
        void Delete(Entity xrmEntity);
        Entity GetEntity(EntityReference reference);
        Entity GetEntity(string logicalName, Guid id);
        DbRow ToDbRow(Entity xrmEntity, bool withReferenceChecks = true);
        void Update(Entity xrmEntity, bool withReferenceChecks = true);
        bool HasRow(EntityReference entityReference);
        IEnumerable<DbRow> GetDBEntityRows(string teamMembership);
        DbRow GetDbRow(Entity entity);
        DbRow GetDbRowOrNull(EntityReference entityReference);
        Entity GetEntityOrNull(EntityReference entityReference);
        bool IsValidEntity(string logicalName);
        DbRow GetDbRow(EntityReference reference, bool v = true);
        void PrefillDBWithOnlineData(QueryExpression queryExpr);
    }
}