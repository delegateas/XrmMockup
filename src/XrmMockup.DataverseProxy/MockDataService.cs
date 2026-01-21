using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmMockup.DataverseProxy;

/// <summary>
/// Mock implementation of IOrganizationServiceAsync2 backed by in-memory data.
/// Used for testing proxy communication without connecting to Dataverse.
/// </summary>
internal class MockDataService : IOrganizationServiceAsync2
{
    private readonly Dictionary<(string LogicalName, Guid Id), Entity> _entities = new();

    public MockDataService(IEnumerable<Entity> entities)
    {
        foreach (var entity in entities)
        {
            _entities[(entity.LogicalName, entity.Id)] = entity;
        }
    }

    public Task<Entity> RetrieveAsync(string entityName, Guid id, ColumnSet columnSet, CancellationToken cancellationToken = default)
    {
        if (_entities.TryGetValue((entityName, id), out var entity))
        {
            return Task.FromResult(ApplyColumnSet(entity, columnSet));
        }

        throw new Exception($"Entity {entityName} with id {id} not found");
    }

    public Task<EntityCollection> RetrieveMultipleAsync(QueryExpression query, CancellationToken cancellationToken = default)
    {
        var matches = _entities.Values
            .Where(e => e.LogicalName == query.EntityName)
            .Where(e => MatchesCriteria(e, query.Criteria))
            .Select(e => ApplyColumnSet(e, query.ColumnSet))
            .ToList();

        return Task.FromResult(new EntityCollection(matches) { EntityName = query.EntityName });
    }

    private static Entity ApplyColumnSet(Entity entity, ColumnSet columnSet)
    {
        if (columnSet.AllColumns)
        {
            return CloneEntity(entity);
        }

        var result = new Entity(entity.LogicalName, entity.Id);
        foreach (var column in columnSet.Columns)
        {
            if (entity.Contains(column))
            {
                result[column] = entity[column];
            }
        }
        return result;
    }

    private static Entity CloneEntity(Entity entity)
    {
        var clone = new Entity(entity.LogicalName, entity.Id);
        foreach (var attr in entity.Attributes)
        {
            clone[attr.Key] = attr.Value;
        }
        return clone;
    }

    private static bool MatchesCriteria(Entity entity, FilterExpression filter)
    {
        if (filter == null || filter.Conditions.Count == 0 && filter.Filters.Count == 0)
        {
            return true;
        }

        var conditionResults = filter.Conditions.Select(c => MatchesCondition(entity, c));
        var filterResults = filter.Filters.Select(f => MatchesCriteria(entity, f));
        var allResults = conditionResults.Concat(filterResults).ToList();

        return filter.FilterOperator == LogicalOperator.And
            ? allResults.All(r => r)
            : allResults.Any(r => r);
    }

    private static bool MatchesCondition(Entity entity, ConditionExpression condition)
    {
        var attributeName = condition.AttributeName;
        var entityValue = entity.Contains(attributeName) ? entity[attributeName] : null;

        return condition.Operator switch
        {
            ConditionOperator.Equal => Equals(entityValue, condition.Values.FirstOrDefault()),
            ConditionOperator.NotEqual => !Equals(entityValue, condition.Values.FirstOrDefault()),
            ConditionOperator.Like => entityValue is string s && condition.Values.FirstOrDefault() is string pattern
                && s.Contains(pattern.Replace("%", "")),
            ConditionOperator.BeginsWith => entityValue is string s2 && condition.Values.FirstOrDefault() is string prefix
                && s2.StartsWith(prefix, StringComparison.OrdinalIgnoreCase),
            ConditionOperator.Null => entityValue == null,
            ConditionOperator.NotNull => entityValue != null,
            _ => true // Default to matching for unsupported operators
        };
    }

    // Synchronous methods - delegate to async versions
    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        => RetrieveAsync(entityName, id, columnSet).GetAwaiter().GetResult();

    public EntityCollection RetrieveMultiple(QueryBase query)
        => RetrieveMultipleAsync((QueryExpression)query).GetAwaiter().GetResult();

    public Task<EntityCollection> RetrieveMultipleAsync(QueryBase query, CancellationToken cancellationToken = default)
        => RetrieveMultipleAsync((QueryExpression)query, cancellationToken);

    // Not implemented - not needed for testing
    public Guid Create(Entity entity) => throw new NotImplementedException();
    public Task<Guid> CreateAsync(Entity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<Guid> CreateAsync(Entity entity) => throw new NotImplementedException();
    public Task<Entity> CreateAndReturnAsync(Entity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public void Update(Entity entity) => throw new NotImplementedException();
    public Task UpdateAsync(Entity entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task UpdateAsync(Entity entity) => throw new NotImplementedException();
    public void Delete(string entityName, Guid id) => throw new NotImplementedException();
    public Task DeleteAsync(string entityName, Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DeleteAsync(string entityName, Guid id) => throw new NotImplementedException();
    public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task AssociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task DisassociateAsync(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    public OrganizationResponse Execute(OrganizationRequest request) => throw new NotImplementedException();
    public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public Task<OrganizationResponse> ExecuteAsync(OrganizationRequest request) => throw new NotImplementedException();

    // IOrganizationServiceAsync methods without CancellationToken
    Task<Entity> IOrganizationServiceAsync.RetrieveAsync(string entityName, Guid id, ColumnSet columnSet)
        => RetrieveAsync(entityName, id, columnSet, CancellationToken.None);

    Task<EntityCollection> IOrganizationServiceAsync.RetrieveMultipleAsync(QueryBase query)
        => RetrieveMultipleAsync(query, CancellationToken.None);
}
