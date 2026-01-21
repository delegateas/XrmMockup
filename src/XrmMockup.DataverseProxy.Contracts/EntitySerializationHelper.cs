#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
#nullable enable
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmMockup.DataverseProxy.Contracts
{
    /// <summary>
    /// Helper class for serializing and deserializing CRM SDK types
    /// using DataContractSerializer.
    /// </summary>
    internal static class EntitySerializationHelper
    {
        private static readonly DataContractSerializer EntitySerializer = new DataContractSerializer(
            typeof(Entity),
            GetKnownTypes());

        private static readonly DataContractSerializer EntityCollectionSerializer = new DataContractSerializer(
            typeof(EntityCollection),
            GetKnownTypes());

        private static readonly DataContractSerializer QueryExpressionSerializer = new DataContractSerializer(
            typeof(QueryExpression),
            GetKnownTypes());

        private static IEnumerable<Type> GetKnownTypes()
        {
            return new[]
            {
                typeof(Entity),
                typeof(EntityCollection),
                typeof(EntityReference),
                typeof(OptionSetValue),
                typeof(Money),
                typeof(AliasedValue),
                typeof(QueryExpression),
                typeof(ColumnSet),
                typeof(ConditionExpression),
                typeof(FilterExpression),
                typeof(LinkEntity),
                typeof(OrderExpression),
                typeof(PagingInfo)
            };
        }

        /// <summary>
        /// Serializes an Entity to a byte array.
        /// Converts derived types (early-bound entities) to base Entity to ensure serialization works.
        /// </summary>
        public static byte[] SerializeEntity(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Convert to base Entity type to ensure serialization works for early-bound types
            var baseEntity = ToBaseEntity(entity);

            using (var ms = new MemoryStream())
            {
                EntitySerializer.WriteObject(ms, baseEntity);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Converts an entity (potentially early-bound) to base Entity type.<br/>
        /// We need to do this because DataContractSerializer has issues with derived types.
        /// </summary>
        private static Entity ToBaseEntity(Entity entity)
        {
            if (entity.GetType() == typeof(Entity))
                return entity;

            var baseEntity = new Entity(entity.LogicalName, entity.Id);

            if (entity.Attributes != null && entity.Attributes.Count > 0)
                baseEntity.Attributes.AddRange(entity.Attributes);

            if (entity.FormattedValues != null && entity.FormattedValues.Count > 0)
                baseEntity.FormattedValues.AddRange(entity.FormattedValues);

            if (entity.RelatedEntities != null && entity.RelatedEntities.Count > 0)
                baseEntity.RelatedEntities.AddRange(entity.RelatedEntities);

            if (entity.KeyAttributes != null && entity.KeyAttributes.Count > 0)
                baseEntity.KeyAttributes.AddRange(entity.KeyAttributes);

            baseEntity.RowVersion = entity.RowVersion;

            return baseEntity;
        }

        /// <summary>
        /// Deserializes an Entity from a byte array.
        /// </summary>
        public static Entity DeserializeEntity(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var ms = new MemoryStream(data))
            {
                var result = EntitySerializer.ReadObject(ms);
                return result as Entity
                    ?? throw new InvalidOperationException("Deserialization returned null or unexpected type");
            }
        }

        /// <summary>
        /// Serializes an EntityCollection to a byte array.
        /// Converts derived types (early-bound entities) to base Entity to ensure serialization works.
        /// </summary>
        public static byte[] SerializeEntityCollection(EntityCollection collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            // Convert all entities to base Entity type
            var baseCollection = new EntityCollection
            {
                EntityName = collection.EntityName,
                MoreRecords = collection.MoreRecords,
                PagingCookie = collection.PagingCookie,
                TotalRecordCount = collection.TotalRecordCount,
                TotalRecordCountLimitExceeded = collection.TotalRecordCountLimitExceeded
            };

            foreach (var entity in collection.Entities)
            {
                baseCollection.Entities.Add(ToBaseEntity(entity));
            }

            using (var ms = new MemoryStream())
            {
                EntityCollectionSerializer.WriteObject(ms, baseCollection);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes an EntityCollection from a byte array.
        /// </summary>
        public static EntityCollection DeserializeEntityCollection(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var ms = new MemoryStream(data))
            {
                var result = EntityCollectionSerializer.ReadObject(ms);
                return result as EntityCollection
                    ?? throw new InvalidOperationException("Deserialization returned null or unexpected type");
            }
        }

        /// <summary>
        /// Serializes a QueryExpression to a byte array.
        /// </summary>
        public static byte[] SerializeQueryExpression(QueryExpression query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            using (var ms = new MemoryStream())
            {
                QueryExpressionSerializer.WriteObject(ms, query);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a QueryExpression from a byte array.
        /// </summary>
        public static QueryExpression DeserializeQueryExpression(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Data cannot be null or empty", nameof(data));

            using (var ms = new MemoryStream(data))
            {
                var result = QueryExpressionSerializer.ReadObject(ms);
                return result as QueryExpression
                    ?? throw new InvalidOperationException("Deserialization returned null or unexpected type");
            }
        }
    }
}
