using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WorkflowExecuter;

namespace DG.Tools.XrmMockup {
    internal class DataMethods {
        private Dictionary<string, Dictionary<Guid, Entity>> db = new Dictionary<string, Dictionary<Guid, Entity>>();
        private Dictionary<string, Type> entityTypeMap = new Dictionary<string, Type>();
        private Dictionary<string, Money> CalcAndRollupTrees = new Dictionary<string, Money>();
        private Guid defaultBusinessUnit;
        private Guid defaultAdminUser;
        private EntityReference baseCurrency;
        private int baseCurrencyPrecision;
        private FullNameConventionCode fullnameFormat;
        private Dictionary<string, EntityMetadata> Metadata;
        private Dictionary<Guid, SecurityRole> SecurityRoles;
        private Dictionary<Guid, Guid> SecurityRoleMapping;
        private Entity RootBusinessUnit;
        private ITracingService trace;
        private IOrganizationServiceFactory serviceFactory;
        private IOrganizationService service;
        private Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>> Shares;
        private RequestHandler RequestHandler;


        internal DataMethods(RequestHandler requestHandler, MetadataSkeleton metadata, List<SecurityRole> SecurityRoles) {
            this.RequestHandler = requestHandler;
            this.db = new Dictionary<string, Dictionary<Guid, Entity>>();
            this.Metadata = metadata.Metadata;
            this.SecurityRoles = SecurityRoles.ToDictionary(s => s.RoleId, s => s);
            this.SecurityRoleMapping = new Dictionary<Guid, Guid>();
            this.Shares = new Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>>();

            var currencies = new Dictionary<Guid, Entity>();
            foreach (var entity in metadata.Currencies) {
                RemoveAttribute(entity, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby", "createdonbehalfby");
                currencies.Add(entity.Id, entity);
            }

            db.Add("transactioncurrency", currencies);
            RootBusinessUnit = metadata.RootBusinessUnit;
            RootBusinessUnit["name"] = "RootBusinessUnit";
            RootBusinessUnit.Attributes.Remove("organizationid");
            this.defaultBusinessUnit = RootBusinessUnit.Id;
            this.fullnameFormat = (FullNameConventionCode)metadata.BaseOrganization.GetAttributeValue<OptionSetValue>("fullnameconventioncode").Value;

            AddRolesForBusinessUnit(RootBusinessUnit.ToEntityReference());
            db[LogicalNames.BusinessUnit] = new Dictionary<Guid, Entity>();
            db[LogicalNames.BusinessUnit].Add(RootBusinessUnit.Id, RootBusinessUnit);

            baseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
            baseCurrencyPrecision = (int)metadata.BaseOrganization.Attributes["pricingdecimalprecision"];
        }

        private void AddRolesForBusinessUnit(EntityReference businessUnit) {
            foreach (var sr in SecurityRoles.Values) {
                var role = new Entity("role");
                role["businessunitid"] = businessUnit;
                role["name"] = sr.Name;
                role["roletemplateid"] = sr.RoleTemplateId;
                role.Id = Guid.NewGuid();
                Create(role, new EntityReference(LogicalNames.SystemUser, defaultAdminUser));
                SecurityRoleMapping.Add(role.Id, sr.RoleId);
            }
        }

        internal void SetAdminUser(EntityReference adminUser) {
            this.defaultAdminUser = adminUser.Id;
        }

        internal EntityMetadata GetMetadata(string logicalName) {
            return Metadata.GetMetadata(logicalName);
        }

        internal EntityReference GetBusinessUnit(EntityReference owner) {
            var user = GetDbEntityDefaultNull(owner);
            if (user == null) return null;
            var buRef = user.GetAttributeValue<EntityReference>("businessunitid");
            var bu = GetDbEntityDefaultNull(buRef);
            if (bu == null) return null;
            buRef.Name = bu.GetAttributeValue<string>("name");
            return buRef;
        }

        private bool HasType(string entityType) {
            return entityTypeMap.ContainsKey(entityType);
        }

        private Entity GetEntity(string entityType) {
            if (HasType(entityType)) {
                return (Entity)Activator.CreateInstance(entityTypeMap[entityType]);
            }
            return null;
        }

        private Entity GetEntityWithAttributes(Entity entity, ColumnSet colsToKeep) {
            if (HasType(entity.LogicalName)) {
                var typedEntity = GetEntity(entity.LogicalName);
                typedEntity.SetAttributes(entity.Attributes, Metadata.GetMetadata(entity.LogicalName), colsToKeep);

                PopulateEntityReferenceNames(typedEntity);
                typedEntity.Id = entity.Id;
                typedEntity.EntityState = entity.EntityState;
                return typedEntity;
            } else {
                return entity.CloneEntity(Metadata.GetMetadata(entity.LogicalName), colsToKeep);
            }
        }

        internal void SetWorkflowServices(ITracingService tracingService, IOrganizationServiceFactory factory, IOrganizationService service) {
            this.trace = tracingService;
            this.serviceFactory = factory;
            this.service = service;
        }

        private void PopulateEntityReferenceNames(Entity entity) {
            foreach (var attr in entity.Attributes) {
                if (attr.Value is EntityReference eRef) {
                    var nameAttr = Metadata.GetMetadata(eRef.LogicalName).PrimaryNameAttribute;
                    eRef.Name = GetDbEntityDefaultNull(eRef)?.GetAttributeValue<string>(nameAttr);
                }
            }
        }

        private RelationshipMetadataBase GetRelatedEntityMetadata(string entityType, string relationshipName) {
            if (!Metadata.ContainsKey(entityType)) {
                return null;
            }
            var oneToMany = GetMetadata(entityType).OneToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (oneToMany != null) {
                return oneToMany;
            }

            var manyToOne = GetMetadata(entityType).ManyToOneRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToOne != null) {
                return manyToOne;
            }

            var manyToMany = GetMetadata(entityType).ManyToManyRelationships
                .FirstOrDefault(r => r.SchemaName == relationshipName);
            if (manyToMany != null) {
                return manyToMany;
            }

            return null;
        }


        internal void Delete(string logicalName, Guid id, EntityReference userRef) {
            var entity = GetDbEntityWithRelatedEntities(new EntityReference(logicalName, id), EntityRole.Referenced, userRef);
            if (entity == null) {
                throw new FaultException($"{logicalName} with Id '{id}' does not exist");
            }

            if (!HasPermission(entity, AccessRights.DeleteAccess, userRef)) {
                throw new FaultException($"You do not have permission to access entity '{logicalName}' for delete");
            }

            if (entity != null) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    var relationshipMeta = Metadata[entity.LogicalName].OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                    switch (relationshipMeta.CascadeConfiguration.Assign) {
                        case CascadeType.Cascade:
                            foreach (var relatedEntity in relatedEntities.Value.Entities) {
                                var req = new DeleteRequest();
                                req.Target = new EntityReference(relatedEntity.LogicalName, relatedEntity.Id);
                                RequestHandler.Execute(req, userRef, null);
                            }
                            break;
                        case CascadeType.RemoveLink:
                            Disassociate(entity.ToEntityReference(), relatedEntities.Key,
                                new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()), userRef);
                            break;
                        case CascadeType.Restrict:
                            return;
                    }
                }

                db[logicalName].Remove(id);
            }
        }

        internal bool ContainsEntity(Entity entity) {
            var dbentity = GetDbEntityDefaultNull(entity.ToEntityReference());
            if (dbentity == null) return false;
            return entity.Attributes.All(a => dbentity.Attributes.ContainsKey(a.Key) && dbentity.Attributes[a.Key].Equals(a.Value));
        }

        internal void PopulateWith(Entity[] entities) {
            foreach (var entity in entities) {
                if (!db.ContainsKey(entity.LogicalName)) {
                    db[entity.LogicalName] = new Dictionary<Guid, Entity>();
                }
                if (entity.Id == Guid.Empty) {
                    var id = Guid.NewGuid();
                    entity.Id = id;
                    entity[entity.LogicalName + "id"] = id;
                }
                db[entity.LogicalName].Add(entity.Id, entity);
            }
        }

        private Entity RetrieveDefaultNull(EntityReference entRef, ColumnSet columnSet) {
            var dbEntity = GetDbEntityDefaultNull(entRef);
            if (dbEntity == null) return null;
            return GetEntityWithAttributes(dbEntity, columnSet);
        }

        public Entity Retrieve(EntityReference entRef, ColumnSet columnSet,
            RelationshipQueryCollection relatedEntityQuery, bool setUnsettableFields, EntityReference userRef) {
            Entity entity;
            if (entRef.LogicalName == null) {
                throw new FaultException("You must provide a LogicalName");
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (columnSet == null && entRef.KeyAttributes.Count == 0) {
                throw new FaultException("The columnset parameter must not be null when no KeyAttributes are provided");
            }
#else
            if (columnSet == null) {
                throw new FaultException("The columnset parameter must not be null");
            }
#endif

            entity = GetDbEntityDefaultNull(entRef);

            if (entity == null)
                throw new FaultException($"The record of type '{entRef.LogicalName}' with id '{entRef.Id}' and name '{entRef.Name}' " +
                    "does not exist. If you use hard-coded records from CRM, then make sure you create those records before retrieving them.");

            if (!HasPermission(entity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{entity.LogicalName}'");
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            var attributes = GetMetadata(entity.LogicalName).Attributes.Where(
                m => m.SourceType == 1 && !(m is MoneyAttributeMetadata && m.LogicalName.EndsWith("_base")));

            foreach (var attr in attributes) {
                string definition = (attr as BooleanAttributeMetadata)?.FormulaDefinition;
                if (attr is BooleanAttributeMetadata) definition = (attr as BooleanAttributeMetadata).FormulaDefinition;
                else if (attr is DateTimeAttributeMetadata) definition = (attr as DateTimeAttributeMetadata).FormulaDefinition;
                else if (attr is DecimalAttributeMetadata) definition = (attr as DecimalAttributeMetadata).FormulaDefinition;
                else if (attr is IntegerAttributeMetadata) definition = (attr as IntegerAttributeMetadata).FormulaDefinition;
                else if (attr is MoneyAttributeMetadata) definition = (attr as MoneyAttributeMetadata).FormulaDefinition;
                else if (attr is PicklistAttributeMetadata) definition = (attr as PicklistAttributeMetadata).FormulaDefinition;
                else if (attr is StringAttributeMetadata) definition = (attr as StringAttributeMetadata).FormulaDefinition;

                if (definition == null) {
                    throw new NotImplementedException("Unknown type when parsing calculated attr");
                }
                var tree = WorkflowConstructor.ParseCalculated(definition);
                tree.Execute(entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true)), RequestHandler.GetCurrentOffset(), service, serviceFactory, trace);
            }
#endif
            entity = GetEntityWithAttributes(entity, columnSet);

            if (!setUnsettableFields) {
                RemoveUnsettableAttributes("Retrieve", entity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            HandlePrecision(entity);
#endif
            if (relatedEntityQuery != null) {
                AddRelatedEntities(entity, relatedEntityQuery, userRef);
            }

            return entity;
        }

        internal Guid? GetEntityId(EntityReference reference) {
            var dbEntity = GetDbEntityDefaultNull(reference);
            return dbEntity == null ? (Guid?)null : dbEntity.Id;
        }

        private void AddRelatedEntities(Entity entity, RelationshipQueryCollection relatedEntityQuery, EntityReference userRef) {
            foreach (var relQuery in relatedEntityQuery) {

                var relationship = relQuery.Key;
                var queryExpr = relQuery.Value as QueryExpression;
                if (queryExpr == null) {
                    queryExpr = XmlHandling.FetchXmlToQueryExpression(((FetchExpression)relQuery.Value).Query);
                }
                var relationshipMetadata = GetRelatedEntityMetadata(queryExpr.EntityName, relationship.SchemaName);


                var oneToMany = relationshipMetadata as OneToManyRelationshipMetadata;
                var manyToMany = relationshipMetadata as ManyToManyRelationshipMetadata;

                if (oneToMany != null) {
                    if (relationship.PrimaryEntityRole == EntityRole.Referenced) {
                        var entityAttributes = GetDbEntityDefaultNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) && entityAttributes[oneToMany.ReferencingAttribute] != null) {
                            var referencingGuid = GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal, referencingGuid));
                        }
                    } else {
                        queryExpr.Criteria.AddCondition(
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal, entity.Id));
                    }
                }

                if (manyToMany != null) {
                    if (db.ContainsKey(manyToMany.IntersectEntityName)) {
                        var conditions = new FilterExpression(Microsoft.Xrm.Sdk.Query.LogicalOperator.Or);
                        if (entity.LogicalName == manyToMany.Entity1LogicalName) {
                            queryExpr.EntityName = manyToMany.Entity2LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(e => (Guid)e.Value.Attributes[manyToMany.Entity1IntersectAttribute] == entity.Id)
                                .Select(e => (Guid)e.Value.Attributes[manyToMany.Entity2IntersectAttribute]);
                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        } else {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(e => (Guid)e.Value.Attributes[manyToMany.Entity2IntersectAttribute] == entity.Id)
                                .Select(e => (Guid)e.Value.Attributes[manyToMany.Entity1IntersectAttribute]);
                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        }
                        queryExpr.Criteria = conditions;
                    }
                }
                var entities = new EntityCollection();

                if ((oneToMany != null || manyToMany != null) && queryExpr.Criteria.Conditions.Count > 0) {
                    entities = RetrieveMultiple(queryExpr, userRef);
                }

                if (entities.Entities.Count() > 0) {
                    entity.RelatedEntities.Add(relationship, entities);
                }
            }
        }

        private Entity GetDbEntityWithRelatedEntities(EntityReference reference, EntityRole primaryEntityRole, EntityReference userRef) {
            var entity = GetDbEntityDefaultNull(reference);
            if (entity == null) {
                return null;
            }
            if (entity.RelatedEntities.Count() > 0) {
                var clone = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
                SetDBEntity(clone, reference);
                entity = clone;
            }
            var relationQuery = new RelationshipQueryCollection();
            var metadata =
                primaryEntityRole == EntityRole.Referenced ? Metadata[entity.LogicalName].OneToManyRelationships : Metadata[entity.LogicalName].ManyToOneRelationships;
            foreach (var relationshipMeta in metadata) {
                var query = new QueryExpression(relationshipMeta.ReferencingEntity);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName, PrimaryEntityRole = primaryEntityRole }, query);
            }

            foreach (var relationshipMeta in Metadata[entity.LogicalName].ManyToManyRelationships) {
                var query = new QueryExpression(relationshipMeta.IntersectEntityName);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName }, query);
            }
            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }

        internal void CloseOpportunity(OpportunityState state, OptionSetValue status, Entity opportunityClose, EntityReference userRef) {
            SetState(opportunityClose["opportunityid"] as EntityReference, new OptionSetValue((int)state), status, userRef);
            var create = new CreateRequest();
            create.Target = opportunityClose;
            RequestHandler.Execute(create as OrganizationRequest, userRef);
        }

        internal Entity GetDbEntityDefaultNull(EntityReference reference) {
            if (!db.ContainsKey(reference.LogicalName) || !db[reference.LogicalName].ContainsKey(reference.Id)) {
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                if (db.ContainsKey(reference.LogicalName) && reference.KeyAttributes.Count > 0) {

                    var records = db[reference.LogicalName].Where(x => reference.KeyAttributes.All(y => x.Value.Attributes[y.Key] == y.Value));

                    if (records.AsEnumerable().Count() == 0) {
                        return null;
                    }
                    return records.First().Value;
                }
#endif
                return null;
            }
            return db[reference.LogicalName][reference.Id];
        }

        private Entity GetDbEntity(EntityReference reference) {
            var entity = GetDbEntityDefaultNull(reference);
            if (entity == null)
                throw new FaultException($"The record of type '{reference.LogicalName}' with id '{reference.Id}' does not exist");
            return entity;
        }

        private void SetDBEntity(Entity entity, EntityReference reference) {
            db[reference.LogicalName][reference.Id] = entity;
        }


        private void CheckSharingAccess(Entity entity, PrincipalAccess principalAccess, EntityReference userRef) {
            if (!HasPermission(entity, AccessRights.ShareAccess, userRef)) {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have share access for that entity");
            }
            if (principalAccess.AccessMask.GetAccessRights().Any(r => !HasPermission(entity, r, userRef))) {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have the privileges it tries to share");
            }
        }

        internal void GrantAccess(EntityReference target, PrincipalAccess principalAccess, EntityReference userRef) {
            var entity = GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckSharingAccess(entity, principalAccess, userRef);

            if (!Shares.ContainsKey(target)) {
                Shares.Add(target, new Dictionary<EntityReference, AccessRights>());
            }

            if (Shares[target].ContainsKey(principalAccess.Principal)) {
                throw new FaultException($"Trying to share record with logicalname '{target.LogicalName}' and id '{target.Id}' with " +
                    $"'{principalAccess.Principal.LogicalName}' with id '{principalAccess.Principal.Id}'" +
                    $", but the record was already shared with the {principalAccess.Principal.LogicalName}");
            }

            Shares[target].Add(principalAccess.Principal, principalAccess.AccessMask);

        }

        internal void ModifyAccess(EntityReference target, PrincipalAccess principalAccess, EntityReference userRef) {
            var entity = GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckSharingAccess(entity, principalAccess, userRef);

            if (Shares.ContainsKey(target) && Shares[target].ContainsKey(principalAccess.Principal)) {
                Shares[target][principalAccess.Principal] = principalAccess.AccessMask;
            }

        }
        internal void RevokeAccess(EntityReference target, EntityReference revokee, EntityReference userRef) {
            var entity = GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            if (!HasPermission(entity, AccessRights.ShareAccess, userRef)) {
                throw new FaultException($"Trying to share entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have share access for that entity");
            }
            if (Shares.ContainsKey(target) && Shares[target].ContainsKey(revokee)) {
                Shares[target].Remove(revokee);
            }
        }


        private void HandleBaseCurrencies(Entity entity) {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr)) {
                return;
            }
            var currency = db[LogicalNames.TransactionCurrency].First(c => c.Key.Equals(entity.GetAttributeValue<EntityReference>(transAttr).Id)).Value;
            var attributesMetadata = Metadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            if (!currency.GetAttributeValue<decimal?>("exchangerate").HasValue) {
                throw new FaultException($"No exchangerate specified for transactioncurrency '{entity.GetAttributeValue<EntityReference>(transAttr)}'");
            }
            foreach (var attr in entity.Attributes.ToList()) {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && !attr.Key.EndsWith("_base")) {
                    if (entity.GetAttributeValue<EntityReference>(transAttr) == baseCurrency) {
                        entity.Attributes[attr.Key + "_base"] = attr.Value;
                    } else {
                        var value = ((Money)attr.Value).Value / currency.GetAttributeValue<decimal?>("exchangerate").Value;
                        entity.Attributes[attr.Key + "_base"] = new Money(value);
                    }
                }
            }
        }

        private void HandlePrecision(Entity entity) {
            if (entity.LogicalName == LogicalNames.TransactionCurrency) return;
            var transAttr = "transactioncurrencyid";
            if (!entity.Attributes.ContainsKey(transAttr)) {
                return;
            }
            var currency = db[LogicalNames.TransactionCurrency].First(c => c.Key.Equals(entity.GetAttributeValue<EntityReference>(transAttr).Id)).Value;
            var attributesMetadata = Metadata.GetMetadata(entity.LogicalName).Attributes.Where(a => a is MoneyAttributeMetadata);
            foreach (var attr in entity.Attributes.ToList()) {
                if (attributesMetadata.Any(a => a.LogicalName == attr.Key) && attr.Value != null) {
                    var metadata = attributesMetadata.First(m => m.LogicalName == attr.Key) as MoneyAttributeMetadata;
                    int? precision = null;
                    switch (metadata.PrecisionSource) {
                        case 0:
                            precision = metadata.Precision;
                            break;
                        case 1:
                            precision = baseCurrencyPrecision;
                            break;
                        case 2:
                            precision = currency.GetAttributeValue<int?>("currencyprecision");
                            break;
                    }

                    if (!precision.HasValue) {
                        switch (metadata.PrecisionSource) {
                            case 0:
                                throw new MockupException($"No precision set for field '{attr.Key}' on entity '{entity.LogicalName}'");
                            case 1:
                                throw new MockupException($"No precision set for organization. Please check you have the correct metadata");
                            case 2:
                                throw new MockupException($"No precision set for currency. Make sure you set the precision for your own currencies");
                        }
                    }

                    var rounded = Math.Round(((Money)attr.Value).Value, precision.Value);
                    if (rounded < (decimal)metadata.MinValue.Value || rounded > (decimal)metadata.MaxValue.Value) {
                        throw new FaultException($"'{attr.Key}' was outside the ranges '{metadata.MinValue}','{metadata.MaxValue}' with value '{rounded}' ");
                    }
                    entity.Attributes[attr.Key] = new Money(rounded);
                }
            }
        }

        private void HandleCurrencies(Entity entity) {
            HandleBaseCurrencies(entity);
            HandlePrecision(entity);
        }

        internal Guid Create<T>(T entity, EntityReference userRef) where T : Entity {
            return Create(entity, userRef, MockupServiceSettings.Role.SDK);
        }

        internal Guid Create<T>(T entity, EntityReference userRef, MockupServiceSettings.Role serviceRole) where T : Entity {
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");
            if (!db.ContainsKey(entity.LogicalName)) {
                db.Add(entity.LogicalName, new Dictionary<Guid, Entity>());
            }
            if (db[entity.LogicalName].ContainsKey(entity.Id)) {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}' and id '{entity.Id}',"
                    + " but entity already exists with those values");
            }
            if (entity.Id == Guid.Empty) {
                throw new MockupException($"Trying to create entity '{entity.LogicalName}', but id is empty guid, please make sure id is initialized");
            }

            var dbEntity = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
            var validAttributes = dbEntity.Attributes.Where(x => x.Value != null);
            dbEntity.Attributes = new AttributeCollection();
            dbEntity.Attributes.AddRange(validAttributes);


            if (userRef != null && userRef.Id != Guid.Empty && !HasPermission(dbEntity, AccessRights.CreateAccess, userRef)) {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have create access for that entity");
            }

            if (HasCircularReference(dbEntity)) {
                throw new FaultException($"Trying to create entity '{dbEntity.LogicalName}', but the attributes had a circular reference");
            }

            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";
            if (!dbEntity.Attributes.ContainsKey(transactioncurrencyId) && 
                Utility.IsSettableAttribute(transactioncurrencyId, Metadata[dbEntity.LogicalName]) &&
                Metadata[dbEntity.LogicalName].Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (serviceRole == MockupServiceSettings.Role.UI ||
                (serviceRole == MockupServiceSettings.Role.SDK && dbEntity.Attributes.Any(
                    attr => Metadata[dbEntity.LogicalName].Attributes.Where(a => a is MoneyAttributeMetadata).Any(m => m.LogicalName == attr.Key))))) {
                var user = GetDbEntity(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                    dbEntity.Attributes[transactioncurrencyId] = user.Attributes[transactioncurrencyId];
                } else {
                    dbEntity.Attributes[transactioncurrencyId] = baseCurrency;
                }
            }

            if (!dbEntity.Attributes.ContainsKey(exchangerate) && 
                Utility.IsSettableAttribute(exchangerate, Metadata[dbEntity.LogicalName]) &&
                dbEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                var currencyId = dbEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db[LogicalNames.TransactionCurrency][currencyId.Id];
                dbEntity.Attributes[exchangerate] = currency["exchangerate"];
                HandleCurrencies(dbEntity);
            }

            var attributes = GetMetadata(dbEntity.LogicalName).Attributes;
            if (!dbEntity.Attributes.ContainsKey("statecode") && 
                Utility.IsSettableAttribute("statecode", Metadata[dbEntity.LogicalName])) {
                var stateMeta = attributes.First(a => a.LogicalName == "statecode") as StateAttributeMetadata;
                dbEntity["statecode"] = stateMeta.DefaultFormValue.HasValue ? new OptionSetValue(stateMeta.DefaultFormValue.Value) : new OptionSetValue(0);
            }
            if (!dbEntity.Attributes.ContainsKey("statuscode") &&
                Utility.IsSettableAttribute("statuscode", Metadata[dbEntity.LogicalName])) {
                var statusMeta = attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata; 
                dbEntity["statuscode"] = statusMeta.DefaultFormValue.HasValue ? new OptionSetValue(statusMeta.DefaultFormValue.Value) : new OptionSetValue(1);
            }

            if (Utility.IsSettableAttribute("createdon", Metadata[dbEntity.LogicalName])) {
                dbEntity.Attributes["createdon"] = DateTime.Now.Add(RequestHandler.GetCurrentOffset());
            }
            if (Utility.IsSettableAttribute("createdby", Metadata[dbEntity.LogicalName])) {
                dbEntity.Attributes["createdby"] = userRef;
            }

            if (Utility.IsSettableAttribute("modifiedon", Metadata[dbEntity.LogicalName]) && 
                Utility.IsSettableAttribute("modifiedby", Metadata[dbEntity.LogicalName])) {
                dbEntity.Attributes["modifiedon"] = dbEntity.Attributes["createdon"];
                dbEntity.Attributes["modifiedby"] = dbEntity.Attributes["createdby"];
            }

            var owner = userRef;
            if (dbEntity.Attributes.ContainsKey("ownerid")) {
                owner = dbEntity.GetAttributeValue<EntityReference>("ownerid");
            }
            SetOwner(dbEntity, owner);

            if (!dbEntity.Attributes.ContainsKey("businessunitid") &&
                dbEntity.LogicalName == "systemuser" || dbEntity.LogicalName == "team") {
                dbEntity.Attributes["businessunitid"] =
                    new EntityReference { Id = defaultBusinessUnit, LogicalName = "businessunit" };
            }

            if (dbEntity.LogicalName == LogicalNames.BusinessUnit && !dbEntity.Attributes.ContainsKey("parentbusinessunitid")) {
                dbEntity.Attributes["parentbusinessunitid"] = new EntityReference(LogicalNames.BusinessUnit, defaultBusinessUnit);
            }
            if (serviceRole == MockupServiceSettings.Role.UI) {
                foreach (var attr in Metadata[dbEntity.LogicalName].Attributes.Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList()) {
                    if (!dbEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        dbEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                    }
                }

                foreach (var attr in Metadata[dbEntity.LogicalName].Attributes.Where(a =>
                    (a as PicklistAttributeMetadata)?.DefaultFormValue != null && (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList()) {
                    if (!dbEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        dbEntity[attr.LogicalName] = new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                    }
                }
            }

            if (dbEntity.LogicalName == LogicalNames.Contact || dbEntity.LogicalName == LogicalNames.Lead || dbEntity.LogicalName == LogicalNames.SystemUser) {
                SetFullName(dbEntity);
            }

            if (dbEntity.LogicalName == LogicalNames.BusinessUnit) {
                AddRolesForBusinessUnit(dbEntity.ToEntityReference());
            }

            db[entity.LogicalName].Add(dbEntity.Id, dbEntity);

            if (entity.RelatedEntities.Count > 0) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    if (GetRelationshipMetadataDefaultNull(relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null) {
                        throw new FaultException($"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }
                    foreach (var relatedEntity in relatedEntities.Value.Entities) {
                        var req = new CreateRequest();
                        req.Target = relatedEntity;
                        RequestHandler.Execute(req, userRef);
                    }
                    Associate(entity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()), userRef);
                }
            }

            return dbEntity.Id;
        }

        private void SetFullName(Entity dbEntity) {
            var first = dbEntity.GetAttributeValue<string>("firstname");
            if (first == null) first = "";
            var middle = dbEntity.GetAttributeValue<string>("middlename");
            if (middle == null) middle = "";
            var last = dbEntity.GetAttributeValue<string>("lastname");
            if (last == null) last = "";
            switch (fullnameFormat) {
                case FullNameConventionCode.FirstLast:
                    dbEntity["fullname"] = first != "" ? first + " " + last : last;
                    break;
                case FullNameConventionCode.LastFirst:
                    dbEntity["fullname"] = first != "" ? last + ", " + first : last;
                    break;
                case FullNameConventionCode.LastNoSpaceFirst:
                    dbEntity["fullname"] = first != "" ? last + first : last;
                    break;
                case FullNameConventionCode.LastSpaceFirst:
                    dbEntity["fullname"] = first != "" ? last + " " + first : last;
                    break;
                case FullNameConventionCode.FirstMiddleLast:
                    dbEntity["fullname"] = first;
                    if (middle != "") dbEntity["fullname"] += " " + middle;
                    dbEntity["fullname"] += (string)dbEntity["fullname"] != "" ? " " + last : last;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.FirstMiddleInitialLast:
                    dbEntity["fullname"] = first;
                    if (middle != "") dbEntity["fullname"] += " " + middle[0] + ".";
                    dbEntity["fullname"] += (string)dbEntity["fullname"] != "" ? " " + last : last;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.LastFirstMiddle:
                    dbEntity["fullname"] = last;
                    if (first != "") dbEntity["fullname"] += ", " + first;
                    if (middle != "") dbEntity["fullname"] += (string)dbEntity["fullname"] == last ? ", " + middle : " " + middle;
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;
                case FullNameConventionCode.LastFirstMiddleInitial:
                    dbEntity["fullname"] = last;
                    if (first != "") dbEntity["fullname"] += ", " + first;
                    if (middle != "") dbEntity["fullname"] +=
                            (string)dbEntity["fullname"] == last ? ", " + middle[0] + "." : " " + middle[0] + ".";
                    if (dbEntity.GetAttributeValue<string>("fullname") == "") dbEntity["fullname"] = null;
                    break;

            }
            if (dbEntity["fullname"] != null) {
                (dbEntity["fullname"] as string).TrimStart().TrimEnd();
            }
        }

        private bool HasCircularReference(Entity entity) {
            if (!Metadata.ContainsKey(entity.LogicalName)) return false;
            var metadata = GetMetadata(entity.LogicalName).Attributes;
            var references = entity.Attributes.Where(a => metadata.FirstOrDefault(m => m.LogicalName == a.Key) is LookupAttributeMetadata).Select(a => a.Value);
            foreach (var r in references) {
                if (r == null) continue;
                Guid guid;
                if (r is EntityReference) {
                    guid = (r as EntityReference).Id;
                } else if (r is Guid) {
                    guid = (Guid)r;
                } else if (r is EntityCollection) {
                    continue;
                } else {
                    throw new NotImplementedException($"{r.GetType()} not implemented in HasCircularReference");
                }
                if (guid == entity.Id) return true;
            }
            return false;
        }



        internal EntityCollection RetrieveMultiple(QueryBase query, EntityReference userRef) {
            var queryExpr = query as QueryExpression;
            var fetchExpr = query as FetchExpression;
            if (queryExpr == null) {
                queryExpr = XmlHandling.FetchXmlToQueryExpression(fetchExpr.Query);
            }

            var collection = new EntityCollection();
            if (db.ContainsKey(queryExpr.EntityName)) {
                foreach (var entity in db[queryExpr.EntityName].Values) {
                    if (!Utility.MatchesCriteria(entity, queryExpr.Criteria)) continue;
                    var toAdd = GetEntityWithAttributes(entity, null);

                    if (queryExpr.LinkEntities.Count > 0) {
                        foreach (var linkEntity in queryExpr.LinkEntities) {
                            collection.Entities.AddRange(
                                GetAliasedValuesFromLinkentity(linkEntity, entity, toAdd, db));
                        }
                    } else {
                        collection.Entities.Add(toAdd);
                    }
                }
            }

            if (!collection.Entities.All(e => HasPermission(e, AccessRights.ReadAccess, userRef))) {
                var entitiesWithoutAccess = collection.Entities.Where(e => !HasPermission(e, AccessRights.ReadAccess, userRef)).Select(e => e.LogicalName).ToList();
                throw new FaultException($"You do not have permission to access the entities " +
                    $"'{string.Join(",", entitiesWithoutAccess)}' for read");
            }

            var orders = queryExpr.Orders;
            var orderedCollection = new EntityCollection();
            // TODO: Check the order that the orders are executed in is correct
            if (orders.Count > 2) {
                throw new MockupException("Number of orders are greater than 2, unsupported in crm");
            } else if (orders.Count == 1) {
                if (orders.First().OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection.Entities.OrderBy(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName])));
                else
                    orderedCollection.Entities.AddRange(collection.Entities.OrderByDescending(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName])));
            } else if (orders.Count == 2) {
                if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection.Entities
                        .OrderBy(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Ascending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(collection.Entities
                        .OrderBy(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Ascending)
                    orderedCollection.Entities.AddRange(collection.Entities
                        .OrderByDescending(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenBy(x => GetComparableAttribute(x.Attributes[orders[1].AttributeName])));

                else if (orders[0].OrderType == OrderType.Descending && orders[1].OrderType == OrderType.Descending)
                    orderedCollection.Entities.AddRange(collection.Entities
                        .OrderByDescending(x => GetComparableAttribute(x.Attributes[orders[0].AttributeName]))
                        .ThenByDescending(x => GetComparableAttribute(x.Attributes[orders[1].AttributeName])));
            }

            if (orderedCollection.Entities.Count != 0) {
                foreach (var entity in orderedCollection.Entities) {
                    KeepAttributesAndAliasAttributes(entity, queryExpr.ColumnSet);
                }
                return orderedCollection;
            }

            foreach (var entity in collection.Entities) {
                KeepAttributesAndAliasAttributes(entity, queryExpr.ColumnSet);
            }
            return collection;
        }

        private void KeepAttributesAndAliasAttributes(Entity entity, ColumnSet toKeep) {
            var clone = entity.CloneEntity(GetMetadata(entity.LogicalName), toKeep);
            if(toKeep != null && !toKeep.AllColumns)
                clone.Attributes.AddRange(entity.Attributes.Where(x => x.Key.Contains(".")));
            entity.Attributes.Clear();
            entity.Attributes.AddRange(clone.Attributes);
        }


        private object GetComparableAttribute(object attribute) {
            if (attribute is Money) {
                return (attribute as Money).Value;
            }
            if (attribute is EntityReference) {
                return (attribute as EntityReference).Name;
            }
            if (attribute is OptionSetValue) {
                return (attribute as OptionSetValue).Value;
            }
            return attribute;
        }

        private List<Entity> GetAliasedValuesFromLinkentity(LinkEntity linkEntity, Entity parent, Entity toAdd,
                Dictionary<string, Dictionary<Guid, Entity>> db) {
            var collection = new List<Entity>();

            if (!db.TryGetValue(linkEntity.LinkToEntityName, out Dictionary<Guid, Entity> linkedRecords)) {
                linkedRecords = new Dictionary<Guid, Entity>();
            }

            foreach (var linkedRecord in linkedRecords.Values) {

                if (!Utility.MatchesCriteria(linkedRecord, linkEntity.LinkCriteria)) continue;

                if (linkedRecord.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName)) {
                    var linkedAttr = Utility.ConvertToComparableObject(
                        linkedRecord.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = Utility.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr)) {

                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                linkedRecord.Attributes, linkEntity.Columns);

                        if (linkEntity.LinkEntities.Count > 0) {
                            var subEntities = new List<Entity>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities) {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                subEntities.AddRange(
                                    GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedRecord, aliasedEntity, db));
                            }
                            collection.AddRange(subEntities);
                        } else {
                            collection.Add(aliasedEntity);
                        }

                    }
                }
            }
            if (linkEntity.JoinOperator == JoinOperator.LeftOuter && collection.Count == 0) {
                collection.Add(toAdd);
            }
            return collection;
        }

        private Entity GetEntityWithAliasAttributes(string alias, Entity toAdd, AttributeCollection attributes,
                ColumnSet columns) {
            var parentClone = GetEntityWithAttributes(toAdd, null);
            foreach (var attr in columns.Columns) {
                parentClone.Attributes.Add(alias + "." + attr, new AliasedValue(alias, attr, attributes[attr]));
            }
            return parentClone;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        private void CheckStatusTransitions(Entity newEntity, Entity prevEntity) {
            if (newEntity == null || prevEntity == null) return;
            if (!newEntity.Attributes.ContainsKey("statuscode") || !prevEntity.Attributes.ContainsKey("statuscode")) return;
            if (newEntity.LogicalName != prevEntity.LogicalName || newEntity.Id != prevEntity.Id) return;

            var newValue = newEntity["statuscode"] as OptionSetValue;
            var prevValue = prevEntity["statuscode"] as OptionSetValue;

            var metadata = GetMetadata(newEntity.LogicalName);
            if (metadata.EnforceStateTransitions != true) return;

            var optionsMeta = (metadata.Attributes
                .FirstOrDefault(a => a is StatusAttributeMetadata) as StatusAttributeMetadata)
                .OptionSet.Options;
            if (!optionsMeta.Any(o => o.Value == newValue.Value)) return;

            var prevValueOptionMeta = optionsMeta.FirstOrDefault(o => o.Value == prevValue.Value) as StatusOptionMetadata;
            if (prevValueOptionMeta == null) return;

            var transitions = prevValueOptionMeta.TransitionData;
            if (transitions != null && transitions != "") {
                var ns = XNamespace.Get("http://schemas.microsoft.com/crm/2009/WebServices");
                var doc = XDocument.Parse(transitions).Element(ns + "allowedtransitions");
                if (doc.Descendants(ns + "allowedtransition")
                    .Where(x => x.Attribute("tostatusid").Value == newValue.Value.ToString())
                    .Any()) {
                    return;
                }
            }
            throw new FaultException($"Trying to switch {newEntity.LogicalName} from status {prevValue.Value} to {newValue.Value}");
        }
#endif

        public void Update(Entity entity, EntityReference userRef, MockupServiceSettings.Role serviceRole) {
            var entRef = entity.ToEntityReference();
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            entRef.KeyAttributes = entity.KeyAttributes;
#endif
            var dbEntity = GetDbEntity(entRef);

            if (serviceRole == MockupServiceSettings.Role.UI &&
                dbEntity.LogicalName != LogicalNames.Opportunity &&
                dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) {
                throw new MockupException($"Trying to update inactive '{dbEntity.LogicalName}', which is impossible in UI");
            }

            if (serviceRole == MockupServiceSettings.Role.UI &&
                dbEntity.LogicalName == LogicalNames.Opportunity &&
                dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value != 0) {
                throw new MockupException($"Trying to update closed opportunity '{dbEntity.Id}', which is impossible in UI");
            }

            // modify for all activites
            //if (entity.LogicalName == "activity" && dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) return;


            if (!HasPermission(dbEntity, AccessRights.WriteAccess, userRef)) {
                throw new FaultException($"Trying to update entity '{dbEntity.LogicalName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }

            var ownerRef = entity.GetAttributeValue<EntityReference>("ownerid");
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (ownerRef != null) {
                CheckAssignPermission(dbEntity, ownerRef, userRef);
            }
#endif

            var updEntity = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            CheckStatusTransitions(updEntity, dbEntity);
#endif


            if (HasCircularReference(updEntity)) {
                throw new FaultException($"Trying to create entity '{dbEntity.LogicalName}', but the attributes had a circular reference");
            }

            if (updEntity.LogicalName == LogicalNames.Contact || updEntity.LogicalName == LogicalNames.Lead || updEntity.LogicalName == LogicalNames.SystemUser) {
                SetFullName(updEntity);
            }

            dbEntity.SetAttributes(updEntity.Attributes, Metadata[updEntity.LogicalName]);

            var transactioncurrencyId = "transactioncurrencyid";
            if (updEntity.LogicalName != LogicalNames.TransactionCurrency &&
                (updEntity.Attributes.ContainsKey(transactioncurrencyId) ||
                updEntity.Attributes.Any(a => Metadata[dbEntity.LogicalName].Attributes.Any(m => m.LogicalName == a.Key && m is MoneyAttributeMetadata)))) {
                if (!dbEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                    var user = GetDbEntity(userRef);
                    if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                        dbEntity.Attributes[transactioncurrencyId] = user.Attributes[transactioncurrencyId];
                    } else {
                        dbEntity.Attributes[transactioncurrencyId] = baseCurrency;
                    }
                }
                var currencyId = dbEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db[LogicalNames.TransactionCurrency][currencyId.Id];
                dbEntity.Attributes["exchangerate"] = currency.GetAttributeValue<decimal?>("exchangerate");
                HandleCurrencies(dbEntity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (updEntity.Attributes.ContainsKey("statecode") || updEntity.Attributes.ContainsKey("statuscode")) {
                HandleCurrencies(dbEntity);
            }
#endif

            if (ownerRef != null) {
                SetOwner(dbEntity, ownerRef);
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                CascadeOwnerUpdate(dbEntity, userRef, ownerRef);
#endif
            }
            Touch(dbEntity, userRef);
        }


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal void CascadeOwnerUpdate(Entity dbEntity, EntityReference userRef, EntityReference ownerRef) {
            // Cascade like Assign, but with UpdateRequests
            foreach (var relatedEntities in GetDbEntityWithRelatedEntities(dbEntity.ToEntityReference(), EntityRole.Referenced, userRef).RelatedEntities) {
                var relationshipMeta = Metadata[dbEntity.LogicalName].OneToManyRelationships.FirstOrDefault(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta == null) continue;

                var req = new UpdateRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign) {
                    case CascadeType.Cascade:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                            req.Target.Attributes["ownerid"] = ownerRef;
                            RequestHandler.Execute(req, userRef, null);
                        }
                        break;

                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            if ((GetDbEntity(relatedEntity.ToEntityReference()).Attributes["statecode"] as OptionSetValue)?.Value == 0) {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                RequestHandler.Execute(req, userRef, null);
                            }
                        }
                        break;

                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if ((GetDbEntity(relatedEntity.ToEntityReference()).Attributes["ownerid"] as EntityReference)?.Id == currentOwner.Id) {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                RequestHandler.Execute(req, userRef, null);
                            }
                        }
                        break;
                }
            }
        }
#endif

        internal string RetrieveVersion() {
            Assembly sdk = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.ManifestModule.Name == "Microsoft.Xrm.Sdk.dll").First();
            return FileVersionInfo.GetVersionInfo(sdk.Location).FileVersion;
        }


        internal void SetState(EntityReference entityRef, OptionSetValue state, OptionSetValue status, EntityReference userRef) {
            var dbEntity = GetDbEntity(entityRef);

            if (Utility.IsSettableAttribute("statecode", Metadata[dbEntity.LogicalName]) && 
                Utility.IsSettableAttribute("statuscode", Metadata[dbEntity.LogicalName])) {
                var prevEntity = dbEntity.CloneEntity();
                dbEntity["statecode"] = state;
                dbEntity["statuscode"] = status;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
                CheckStatusTransitions(dbEntity, prevEntity);
#endif
                HandleCurrencies(dbEntity);
                Touch(dbEntity, userRef);
            }
        }

        private void CheckAssignPermission(Entity entity, EntityReference assignee, EntityReference userRef) {
            if (!HasPermission(entity, AccessRights.AssignAccess, userRef)) {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                    $", but calling user with id '{userRef.Id}' does not have assign access for that entity");
            }

            if (!HasPermission(entity, AccessRights.WriteAccess, userRef)) {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }
        }

        internal void Assign(EntityReference target, EntityReference assignee, EntityReference userRef) {
            var dbEntity = GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckAssignPermission(dbEntity, assignee, userRef);

            // Cascade
            foreach (var relatedEntities in dbEntity.RelatedEntities) {
                var relationshipMeta = Metadata[dbEntity.LogicalName].OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                var req = new AssignRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign) {
                    case CascadeType.Cascade:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            req.Target = relatedEntity.ToEntityReference();
                            req.Assignee = assignee;
                            RequestHandler.Execute(req, userRef, null);
                        }
                        break;
                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            if ((GetDbEntity(relatedEntity.ToEntityReference()).Attributes["statecode"] as OptionSetValue)?.Value == 0) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = assignee;
                                RequestHandler.Execute(req, userRef, null);
                            }
                        }
                        break;
                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if ((GetDbEntity(relatedEntity.ToEntityReference()).Attributes["ownerid"] as EntityReference)?.Id == currentOwner.Id) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = assignee;
                                RequestHandler.Execute(req, userRef, null);
                            }
                        }
                        break;
                }
            }
            SetOwner(dbEntity, assignee);
            Touch(dbEntity, userRef);
        }

        internal RelationshipMetadataBase GetRelationshipMetadataDefaultNull(string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                return null;
            }
            RelationshipMetadataBase relationshipBase;
            foreach (var meta in Metadata) {
                relationshipBase = meta.Value.ManyToManyRelationships.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                relationshipBase = meta.Value.ManyToManyRelationships.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                var oneToManyBases = meta.Value.ManyToOneRelationships.Concat(meta.Value.OneToManyRelationships);
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.MetadataId == metadataId);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
                relationshipBase = oneToManyBases.FirstOrDefault(rel => rel.SchemaName == name);
                if (relationshipBase != null) {
                    return relationshipBase;
                }
            }
            return null;
        }


        internal RelationshipMetadataBase GetRelationshipMetadata(string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                throw new FaultException("Relationship name is required when MetadataId is not specified");
            }
            var metadata = GetRelationshipMetadataDefaultNull(name, metadataId, userRef);
            if (metadata == null) {
                throw new FaultException("Could not find relationship");
            }
            return metadata;
        }


        internal EntityMetadata GetEntityMetadata(string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                throw new FaultException("Entity logical name is required when MetadataId is not specified");
            }
            if (name != null && Metadata.ContainsKey(name)) return Metadata[name];
            if (metadataId != Guid.Empty) return Metadata.FirstOrDefault(x => x.Value.MetadataId == metadataId).Value;
            throw new FaultException("Could not find matching entity");
        }


        internal void Associate(EntityReference target, Relationship relationship, EntityReferenceCollection relatedEntities, EntityReference userRef) {
            var relatedLogicalName = relatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as ManyToManyRelationshipMetadata;
            if (!HasPermission(GetDbEntity(target), AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!HasPermission(GetDbEntity(target), AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(GetDbEntity(r), AccessRights.ReadAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(GetDbEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to append entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(GetDbEntity(r), AccessRights.AppendAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(GetDbEntity(r), AccessRights.AppendAccess, userRef));
                throw new FaultException($"Trying to append entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have append access for that entity");
            }

            if (manyToMany != null) {
                foreach (var relatedEntity in relatedEntities) {
                    var linker = new Entity(manyToMany.IntersectEntityName);
                    linker.Id = Guid.NewGuid();
                    if (target.LogicalName == manyToMany.Entity1LogicalName) {
                        linker.Attributes[manyToMany.Entity1IntersectAttribute] = target.Id;
                        linker.Attributes[manyToMany.Entity2IntersectAttribute] = relatedEntity.Id;
                    } else {
                        linker.Attributes[manyToMany.Entity1IntersectAttribute] = relatedEntity.Id;
                        linker.Attributes[manyToMany.Entity2IntersectAttribute] = target.Id;
                    }
                    Create(linker, userRef, MockupServiceSettings.Role.SDK);
                }
            } else {
                if (oneToMany.ReferencedEntity == target.LogicalName) {
                    foreach (var relatedEntity in relatedEntities) {
                        var dbEntity = GetDbEntity(relatedEntity);
                        dbEntity[oneToMany.ReferencingAttribute] = target;
                        Touch(dbEntity, userRef);
                    }
                } else {
                    if (relatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (relatedEntities.Count == 1) {
                        var related = relatedEntities.First();
                        var targetEntity = GetDbEntity(target);
                        targetEntity[oneToMany.ReferencingAttribute] = related;
                        Touch(targetEntity, userRef);
                    }

                }
            }
        }

        internal void Disassociate(EntityReference target, Relationship relationship, EntityReferenceCollection relatedEntities, EntityReference userRef) {
            var relatedLogicalName = relatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as ManyToManyRelationshipMetadata;

            if (!HasPermission(GetDbEntity(target), AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!HasPermission(GetDbEntity(target), AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(GetDbEntity(r), AccessRights.ReadAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(GetDbEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(GetDbEntity(r), AccessRights.AppendAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(GetDbEntity(r), AccessRights.AppendAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have append access for that entity");
            }

            if (manyToMany != null) {
                foreach (var relatedEntity in relatedEntities) {
                    if (target.LogicalName == manyToMany.Entity1LogicalName) {
                        var link = db[manyToMany.IntersectEntityName].First(e =>
                            (Guid)e.Value.Attributes[manyToMany.Entity1IntersectAttribute] == target.Id &&
                            (Guid)e.Value.Attributes[manyToMany.Entity2IntersectAttribute] == relatedEntity.Id);
                        db[manyToMany.IntersectEntityName].Remove(link.Key);
                    } else {
                        var link = db[manyToMany.IntersectEntityName].First(e =>
                            (Guid)e.Value.Attributes[manyToMany.Entity1IntersectAttribute] == relatedEntity.Id &&
                            (Guid)e.Value.Attributes[manyToMany.Entity2IntersectAttribute] == target.Id);
                        db[manyToMany.IntersectEntityName].Remove(link.Key);
                    }
                }
            } else {
                if (oneToMany.ReferencedEntity == target.LogicalName) {
                    foreach (var relatedEntity in relatedEntities) {
                        var dbEntity = GetDbEntity(relatedEntity);
                        RemoveAttribute(dbEntity, oneToMany.ReferencingAttribute);
                        Touch(dbEntity, userRef);
                    }
                } else {
                    if (relatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (relatedEntities.Count == 1) {
                        var related = relatedEntities.First();
                        var targetEntity = GetDbEntity(target);
                        RemoveAttribute(targetEntity, oneToMany.ReferencingAttribute);
                        Touch(targetEntity, userRef);
                    }
                }
            }
        }

        internal void Merge(EntityReference target, Guid subordinateId, Entity updateContent,
            bool performParentingChecks, EntityReference userRef) {
            var mainEntity = GetDbEntity(target);
            var subordinateReference = new EntityReference { LogicalName = target.LogicalName, Id = subordinateId };
            var subordinateEntity = GetDbEntityWithRelatedEntities(subordinateReference, EntityRole.Referencing, userRef);

            foreach (var attr in updateContent.Attributes) {
                if (attr.Value != null) {
                    mainEntity.Attributes[attr.Key] = attr.Value;
                }
            }

            foreach (var relatedEntities in subordinateEntity.RelatedEntities) {
                var relationshipMeta = Metadata[subordinateEntity.LogicalName].ManyToOneRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta.CascadeConfiguration.Merge == CascadeType.Cascade) {
                    var entitiesToSwap = relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList();
                    Disassociate(subordinateEntity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(entitiesToSwap), userRef);
                    Associate(mainEntity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(entitiesToSwap), userRef);
                }
            }

            subordinateEntity.Attributes["merged"] = true;
            SetState(subordinateReference, new OptionSetValue(1), new OptionSetValue(2), userRef);
        }

        private void SetOwner(Entity entity, EntityReference owner) {
            var ownershipType = Metadata.GetMetadata(entity.LogicalName).OwnershipType;

            if (!ownershipType.HasValue) {
                throw new MockupException($"No ownership type set for '{entity.LogicalName}'");
            }

            if (ownershipType.Value.HasFlag(OwnershipTypes.UserOwned) || ownershipType.Value.HasFlag(OwnershipTypes.TeamOwned)) {
                if (GetDbEntityDefaultNull(owner) == null) {
                    throw new FaultException($"Owner referenced with id '{owner.Id}' does not exist");
                }

                var prevOwner = entity.Attributes.ContainsKey("ownerid") ? entity["ownerid"] : null;
                entity.Attributes["ownerid"] = owner;

                if (!HasPermission(entity, AccessRights.ReadAccess, owner)) {
                    entity["ownerid"] = prevOwner;
                    throw new FaultException($"Trying to assign '{entity.LogicalName}' with id '{entity.Id}'" +
                        $" to '{owner.LogicalName}' with id '{owner.Id}', but owner does not have read access for that entity");
                }

                entity.Attributes["owningbusinessunit"] = null;
                entity.Attributes["owninguser"] = null;
                entity.Attributes["owningteam"] = null;


                if (entity.LogicalName != "systemuser" && entity.LogicalName != "team") {
                    if (owner.LogicalName == "systemuser" && ownershipType.Value.HasFlag(OwnershipTypes.UserOwned)) {
                        entity.Attributes["owninguser"] = owner;
                    } else if (owner.LogicalName == "team") {
                        entity.Attributes["owningteam"] = owner;
                    } else {
                        throw new MockupException($"Trying to give owner to {owner.LogicalName} but ownershiptype is {ownershipType.ToString()}");
                    }
                    entity.Attributes["owningbusinessunit"] = GetBusinessUnit(owner);
                }
            }
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        internal Entity CalculateRollUpField(EntityReference entRef, string fieldName, EntityReference userRef) {
            var dbEntity = GetDbEntity(entRef);
            var metadata = GetMetadata(entRef.LogicalName);
            var field = metadata.Attributes.FirstOrDefault(a => a.LogicalName == fieldName);
            if (field == null) {
                throw new FaultException($"Couldn't find the field '{fieldName}' on the entity '{entRef.LogicalName}'");
            } else {
                string definition = (field as BooleanAttributeMetadata)?.FormulaDefinition;
                if ((field as BooleanAttributeMetadata)?.SourceType == 2) definition = (field as BooleanAttributeMetadata).FormulaDefinition;
                else if ((field as DateTimeAttributeMetadata)?.SourceType == 2) definition = (field as DateTimeAttributeMetadata).FormulaDefinition;
                else if ((field as DecimalAttributeMetadata)?.SourceType == 2) definition = (field as DecimalAttributeMetadata).FormulaDefinition;
                else if ((field as IntegerAttributeMetadata)?.SourceType == 2) definition = (field as IntegerAttributeMetadata).FormulaDefinition;
                else if ((field as MoneyAttributeMetadata)?.SourceType == 2) definition = (field as MoneyAttributeMetadata).FormulaDefinition;
                else if ((field as PicklistAttributeMetadata)?.SourceType == 2) definition = (field as PicklistAttributeMetadata).FormulaDefinition;
                else if ((field as StringAttributeMetadata)?.SourceType == 2) definition = (field as StringAttributeMetadata).FormulaDefinition;

                if (definition == null) {
                    throw new FaultException($"Field '{fieldName}' on entity '{entRef.LogicalName}' is not a rollup field");
                } else {
                    var tree = WorkflowConstructor.ParseRollUp(definition);
                    var resultTree = tree.Execute(dbEntity, RequestHandler.GetCurrentOffset(), service, serviceFactory, trace);
                    var resultLocaltion = ((resultTree.StartActivity as RollUp).Aggregation[1] as Aggregate).VariableName;
                    var result = resultTree.Variables[resultLocaltion];
                    if (result != null) {
                        dbEntity.Attributes[fieldName] = result;
                    }
                }
            }
            HandleCurrencies(dbEntity);
            return dbEntity;
        }
#endif

        private void Touch(Entity dbEntity, EntityReference user) {
            if (Utility.IsSettableAttribute("modifiedon", Metadata[dbEntity.LogicalName]) && Utility.IsSettableAttribute("modifiedby", Metadata[dbEntity.LogicalName])) {
                dbEntity.Attributes["modifiedon"] = DateTime.Now.Add(RequestHandler.GetCurrentOffset());
                dbEntity.Attributes["modifiedby"] = user;
            }
        }

        //private bool CanSetAttribute(string attributeName, EntityMetadata metadata) {
        //    return metadata.Attributes.Any(a => a.LogicalName == attributeName);
        //}

        internal void RemoveUnsettableAttributes(string actionType, Entity entity) {
            if (entity == null) return;

            RemoveAttribute(entity,
                "modifiedon",
                "modifiedby",
                "createdby",
                "createdon");

            var metadata = GetMetadata(entity.LogicalName);
            switch (actionType) {
                case "Create":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForCreate == false)).Select(a => a.Key).ToArray());
                    break;
                case "Retrieve":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForRead == false)).Select(a => a.Key).ToArray());
                    break;
                case "Update":
                    RemoveAttribute(entity,
                        entity.Attributes.Where(a => metadata.Attributes.Any(m => m.LogicalName == a.Key && m.IsValidForUpdate == false)).Select(a => a.Key).ToArray());
                    break;
            }
            entity[metadata.PrimaryIdAttribute] = entity.Id;
        }

        private void RemoveAttribute(Entity entity, params string[] attrNames) {
            foreach (var attrName in attrNames) {
                if (entity.Attributes.ContainsKey(attrName)) entity.Attributes.Remove(attrName);
            }
        }

        internal void ResetEnvironment() {
            var toKeep = new Dictionary<string, Dictionary<Guid, Entity>>();
            toKeep[LogicalNames.TransactionCurrency] = db[LogicalNames.TransactionCurrency];
            var adminUser = db[LogicalNames.SystemUser][defaultAdminUser];
            adminUser.Attributes["transactioncurrencyid"] = baseCurrency;
            toKeep[LogicalNames.SystemUser] = new Dictionary<Guid, Entity>();
            toKeep[LogicalNames.SystemUser].Add(defaultAdminUser, adminUser);

            this.db = toKeep;
            db.Add(LogicalNames.BusinessUnit, new Dictionary<Guid, Entity>());
            db[LogicalNames.BusinessUnit].Add(RootBusinessUnit.Id, RootBusinessUnit);
            AddRolesForBusinessUnit(RootBusinessUnit.ToEntityReference());
        }


        internal void EnableProxyTypes(Assembly proxyTypeAssembly) {
            foreach (var type in proxyTypeAssembly.GetLoadableTypes()) {
                var logicalName =
                            type.CustomAttributes
                            .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                            ?.ConstructorArguments
                            ?.FirstOrDefault()
                            .Value as string;

                if (logicalName != null) {
                    entityTypeMap.Add(logicalName, type);
                }
            }
        }

        internal void SetSecurityRole(EntityReference entRef, Guid[] securityRoles) {
            if (securityRoles.Any(s => !SecurityRoles.ContainsKey(s))) {
                throw new MockupException($"Unknown security role");
            }
            var user = GetDbEntityDefaultNull(entRef);
            if (user == null) {
                throw new MockupException($"{entRef.LogicalName} with id '{entRef.Id}' does not exist");
            }
            var relationship = entRef.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = securityRoles
                .Select(sr => SecurityRoleMapping.Where(srm => srm.Value == sr).Select(srm => srm.Key))
                .Select(roleGuids => db["role"].Where(r => roleGuids.Contains(r.Key)).Select(r => r.Value))
                .Select(roleEntities =>
                    roleEntities.First(e => (e.Attributes["businessunitid"] as EntityReference).Id == (user.Attributes["businessunitid"] as EntityReference).Id))
                .Select(r => r.ToEntityReference());
            Associate(entRef, relationship, new EntityReferenceCollection(roles.ToList()), new EntityReference(LogicalNames.SystemUser, defaultAdminUser));
        }


        internal HashSet<SecurityRole> GetSecurityRoles(EntityReference caller) {
            var securityRoles = new HashSet<SecurityRole>();
            var callerEntity = GetDbEntityWithRelatedEntities(caller, EntityRole.Referenced, new EntityReference(LogicalNames.SystemUser, defaultAdminUser));
            if (callerEntity == null) return securityRoles;
            var relationship = caller.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = callerEntity.RelatedEntities.ContainsKey(relationship) ? callerEntity.RelatedEntities[relationship] : new EntityCollection();
            foreach (var role in roles.Entities) {
                securityRoles.Add(SecurityRoles[SecurityRoleMapping[role.Id]]);
            }
            return securityRoles;
        }

        private bool IsInBusinessUnit(Guid ownerId, Guid businessunitId) {
            var usersInBusinessUnit = db[LogicalNames.SystemUser].Where(u => u.Value.GetAttributeValue<EntityReference>("businessunitid")?.Id == businessunitId);
            return usersInBusinessUnit.Any(u => ownerId == u.Key);
        }

        private bool IsInBusinessUnitTree(Guid ownerId, Guid businessunitId) {
            if (IsInBusinessUnit(ownerId, businessunitId)) return true;

            var childBusinessUnits = db[LogicalNames.BusinessUnit].Where(b => b.Value.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id == businessunitId);
            return childBusinessUnits.Any(b => IsInBusinessUnitTree(ownerId, b.Key));

        }

        private Guid GetGuidFromReference(object reference) {
            return reference is EntityReference ? (reference as EntityReference).Id : (Guid)reference;
        }


        private bool HasPermission(Entity entity, AccessRights access, EntityReference caller) {
            if (!SecurityRoles.Any(s => s.Value.Privileges.Any(p => p.Key == entity.LogicalName))) {
                // system has no security roles for this entity. Is a case with linkentities which have no security roles
                return true;
            }
            if (caller.Id == defaultAdminUser) return true;

            var userRoles = GetSecurityRoles(caller)?.Where(r =>
                r.Privileges.ContainsKey(entity.LogicalName) &&
                r.Privileges[entity.LogicalName].ContainsKey(access));
            if (userRoles == null || userRoles.Count() == 0) {
                return false;
            }
            var maxRole = userRoles.Max(r => r.Privileges[entity.LogicalName][access].PrivilegeDepth);
            if (maxRole == PrivilegeDepth.Global) return true;

            if (access == AccessRights.CreateAccess) {
                if (!entity.Attributes.ContainsKey("ownerid")) {
                    entity.Attributes["ownerid"] = caller;
                }
            }

            if (entity.Attributes.ContainsKey("ownerid")) {
                var owner = entity.Attributes["ownerid"] as EntityReference;
                if (owner.Id == caller.Id) {
                    return true;
                }

                var callerEntity = GetDbEntity(caller);
                if (maxRole == PrivilegeDepth.Local) {
                    return IsInBusinessUnit(owner.Id, callerEntity.GetAttributeValue<EntityReference>("businessunitid").Id);
                }
                if (maxRole == PrivilegeDepth.Deep) {
                    if (callerEntity.GetAttributeValue<EntityReference>("parentbusinessunitid") != null) {
                        return IsInBusinessUnitTree(owner.Id, callerEntity.GetAttributeValue<EntityReference>("parentbusinessunitid").Id);
                    }
                    return IsInBusinessUnitTree(owner.Id, callerEntity.GetAttributeValue<EntityReference>("businessunitid").Id);
                }
            }

            if (Shares.ContainsKey(entity.ToEntityReference()) &&
                Shares[entity.ToEntityReference()].ContainsKey(caller) &&
                Shares[entity.ToEntityReference()][caller].HasFlag(access)) {
                return true;
            }

            var parentChangeRelationships = GetMetadata(entity.LogicalName).ManyToOneRelationships
                .Where(r => r.CascadeConfiguration.Reparent == CascadeType.Cascade || r.CascadeConfiguration.Reparent == CascadeType.Active)
                .Where(r => entity.Attributes.ContainsKey(r.ReferencingAttribute));
            if (parentChangeRelationships.Any(r =>
                db.ContainsKey(r.ReferencedEntity) &&
                db[r.ReferencedEntity].ContainsKey(GetGuidFromReference(entity.Attributes[r.ReferencingAttribute])) &&
                db[r.ReferencedEntity][GetGuidFromReference(entity.Attributes[r.ReferencingAttribute])].GetAttributeValue<EntityReference>("ownerid").Id == caller.Id)) {
                return true;
            }



            return false;
        }
    }
}
