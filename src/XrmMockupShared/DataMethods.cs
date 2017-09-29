using DG.Tools.XrmMockup.Database;
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
        private XrmDb db;

        private Dictionary<string, Type> entityTypeMap = new Dictionary<string, Type>();
        private Dictionary<string, Money> CalcAndRollupTrees;
        internal EntityReference baseCurrency;
        private int baseCurrencyPrecision;
        private Dictionary<string, EntityMetadata> EntityMetadata;
        private Dictionary<Guid, SecurityRole> SecurityRoles;
        private Dictionary<Guid, Guid> SecurityRoleMapping;

        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;



        private ITracingService trace;
        private IOrganizationServiceFactory serviceFactory;
        private IOrganizationService service;
        private Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>> Shares;
        private Core Core;
        private MetadataSkeleton Metadata;


        /// <summary>
        /// Organization id for the Mockup instance
        /// </summary>
        public Guid OrganizationId { get; private set; }

        /// <summary>
        /// Organization name for the Mockup instance
        /// </summary>
        public string OrganizationName { get; private set; }

        internal DataMethods(Core core, XrmDb db, MetadataSkeleton metadata, List<SecurityRole> SecurityRoles) {
            this.Core = core;
            this.EntityMetadata = metadata.EntityMetadata;
            this.Metadata = metadata;
            this.SecurityRoles = SecurityRoles.ToDictionary(s => s.RoleId, s => s);


            baseCurrency = metadata.BaseOrganization.GetAttributeValue<EntityReference>("basecurrencyid");
            baseCurrencyPrecision = metadata.BaseOrganization.GetAttributeValue<int>("pricingdecimalprecision");

            Initialize(db);
        }

        private void Initialize(XrmDb db) {
            this.OrganizationId = Guid.NewGuid();
            this.OrganizationName = "MockupOrganization";
            this.SecurityRoleMapping = new Dictionary<Guid, Guid>();
            this.Shares = new Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>>();
            this.CalcAndRollupTrees = new Dictionary<string, Money>();

            this.db = db;

            // Setup currencies
            var currencies = new List<Entity>();
            foreach (var entity in Metadata.Currencies) {
                RemoveAttribute(entity, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby", "createdonbehalfby");
                currencies.Add(entity);
            }
            this.db.AddRange(currencies);

            // Setup root business unit
            var rootBu = Metadata.RootBusinessUnit;
            rootBu["name"] = "RootBusinessUnit";
            rootBu.Attributes.Remove("organizationid");
            this.db.Add(rootBu, false);
            this.RootBusinessUnitRef = rootBu.ToEntityReference();

            // Setup admin user
            var admin = new Entity(LogicalNames.SystemUser) {
                Id = Guid.NewGuid()
            };
            this.AdminUserRef = admin.ToEntityReference();

            admin["firstname"] = "";
            admin["lastname"] = "SYSTEM";
            admin["businessunitid"] = RootBusinessUnitRef;
            this.db.Add(admin);
            
            AddRolesForBusinessUnit(RootBusinessUnitRef);

            SetSecurityRole(AdminUserRef,
                SecurityRoles
                .Where(s => s.Value.RoleTemplateId == new Guid("627090ff-40a3-4053-8790-584edc5be201")) // System administrator role template ID
                .Select(s => s.Value.RoleId)
                .ToArray());
        }

        internal void AddRolesForBusinessUnit(EntityReference businessUnit) {
            foreach (var sr in SecurityRoles.Values) {
                var roleMeta = EntityMetadata.GetMetadata("role");
                var role = new Entity("role");
                role.Id = Guid.NewGuid();
                role[roleMeta.PrimaryIdAttribute] = role.Id;
                role["businessunitid"] = businessUnit;
                role["name"] = sr.Name;
                role["roletemplateid"] = sr.RoleTemplateId;
                role["createdby"] = AdminUserRef;
                role["createdon"] = DateTime.Now.Add(Core.TimeOffset);
                role["modifiedby"] = AdminUserRef;
                role["modifiedon"] = DateTime.Now.Add(Core.TimeOffset);
                db.Add(role);
                SecurityRoleMapping.Add(role.Id, sr.RoleId);
            }
        }

        internal EntityMetadata GetMetadata(string logicalName) {
            return EntityMetadata.GetMetadata(logicalName);
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

        private Entity GetStronglyTypedEntity(Entity entity, ColumnSet colsToKeep) {
            if (HasType(entity.LogicalName)) {
                var typedEntity = GetEntity(entity.LogicalName);
                typedEntity.SetAttributes(entity.Attributes, EntityMetadata.GetMetadata(entity.LogicalName), colsToKeep);

                PopulateEntityReferenceNames(typedEntity);
                typedEntity.Id = entity.Id;
                typedEntity.EntityState = entity.EntityState;
                return typedEntity;
            } else {
                return entity.CloneEntity(EntityMetadata.GetMetadata(entity.LogicalName), colsToKeep);
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
                    var nameAttr = EntityMetadata.GetMetadata(eRef.LogicalName).PrimaryNameAttribute;
                    eRef.Name = db.GetEntityOrNull(eRef)?.GetAttributeValue<string>(nameAttr);
                }
            }
        }

        private RelationshipMetadataBase GetRelatedEntityMetadata(string entityType, string relationshipName) {
            if (!EntityMetadata.ContainsKey(entityType)) {
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
                    var relationshipMeta = EntityMetadata[entity.LogicalName].OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                    switch (relationshipMeta.CascadeConfiguration.Assign) {
                        case CascadeType.Cascade:
                            foreach (var relatedEntity in relatedEntities.Value.Entities) {
                                var req = new DeleteRequest();
                                req.Target = new EntityReference(relatedEntity.LogicalName, relatedEntity.Id);
                                Core.Execute(req, userRef, null);
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
            var dbentity = db.GetEntityOrNull(entity.ToEntityReference());
            if (dbentity == null) return false;
            return entity.Attributes.All(a => dbentity.Attributes.ContainsKey(a.Key) && dbentity.Attributes[a.Key].Equals(a.Value));
        }

        internal void PopulateWith(Entity[] entities) {
            foreach (var entity in entities) {
                if (entity.Id == Guid.Empty) {
                    var id = Guid.NewGuid();
                    entity.Id = id;
                    entity[entity.LogicalName + "id"] = id;
                }
                db.Add(entity);
            }
        }

        private void AddFormattedValues(Entity entity) {

        }

        private Boolean IsValidForFormattedValues(AttributeMetadata attributeMetadata) {
            return
                attributeMetadata is PicklistAttributeMetadata ||
                attributeMetadata is BooleanAttributeMetadata ||
                attributeMetadata is MoneyAttributeMetadata ||
                attributeMetadata is LookupAttributeMetadata ||
                attributeMetadata is IntegerAttributeMetadata ||
                attributeMetadata is DateTimeAttributeMetadata ||
                attributeMetadata is MemoAttributeMetadata ||
                attributeMetadata is DoubleAttributeMetadata ||
                attributeMetadata is DecimalAttributeMetadata;
        }

        private string GetFormattedValueLabel(AttributeMetadata metadataAtt, object value, Entity entity) {
            if (metadataAtt is PicklistAttributeMetadata) {
                var optionset = (metadataAtt as PicklistAttributeMetadata).OptionSet.Options
                    .Where(opt => opt.Value == (value as OptionSetValue).Value).FirstOrDefault();
                return optionset.Label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is BooleanAttributeMetadata) {
                var booleanOptions = (metadataAtt as BooleanAttributeMetadata).OptionSet;
                var label = (bool)value ? booleanOptions.TrueOption.Label : booleanOptions.FalseOption.Label;
                return label.UserLocalizedLabel.Label;
            }

            if (metadataAtt is MoneyAttributeMetadata) {
                var currency = db.GetEntity(entity.GetAttributeValue<EntityReference>("transactioncurrencyid"));
                var currencysymbol = currency.GetAttributeValue<string>("currencysymbol");
                return currencysymbol + (value as Money).Value.ToString();
            }

            if (metadataAtt is LookupAttributeMetadata) {
                try {
                    return (value as EntityReference).Name;
                } catch (NullReferenceException e) {
                    Console.WriteLine("No lookup entity exists: " + e.Message);
                }
            }

            if (metadataAtt is IntegerAttributeMetadata ||
                metadataAtt is DateTimeAttributeMetadata ||
                metadataAtt is MemoAttributeMetadata ||
                metadataAtt is DoubleAttributeMetadata ||
                metadataAtt is DecimalAttributeMetadata) {
                return value.ToString();
            }

            return null;
        }

        private void SetFormmattedValues(Entity entity) {
            var metadata = GetMetadata(entity.LogicalName).Attributes
                .Where(a => IsValidForFormattedValues(a));

            var formattedValues = new List<KeyValuePair<string, string>>();
            foreach (var a in entity.Attributes) {
                if (a.Value == null) continue;
                var metadataAtt = metadata.Where(m => m.LogicalName == a.Key).FirstOrDefault();
                var formattedValuePair = new KeyValuePair<string, string>(a.Key, GetFormattedValueLabel(metadataAtt, a.Value, entity));
                if (formattedValuePair.Value != null) {
                    formattedValues.Add(formattedValuePair);
                }
            }

            if (formattedValues.Count > 0) {
                entity.FormattedValues.AddRange(formattedValues);
            }
        }
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        private void ExecuteCalculatedFields(Entity entity) {
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
                tree.Execute(entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true)), Core.TimeOffset, service, serviceFactory, trace);
            }
        }
#endif

        private Entity RetrieveDefaultNull(EntityReference entRef, ColumnSet columnSet) {
            var dbEntity = db.GetEntityOrNull(entRef);
            if (dbEntity == null) return null;
            return GetStronglyTypedEntity(dbEntity, columnSet);
        }

        public Entity Retrieve(EntityReference entRef, ColumnSet columnSet,
            RelationshipQueryCollection relatedEntityQuery, bool setUnsettableFields, EntityReference userRef) {

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
            var entity = db.GetEntity(entRef);

            if (!HasPermission(entity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Calling user with id '{userRef.Id}' does not have permission to read entity '{entity.LogicalName}'");
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            ExecuteCalculatedFields(entity);
#endif
            entity = GetStronglyTypedEntity(db.GetEntity(entRef), columnSet);

            SetFormmattedValues(entity);

            if (!setUnsettableFields) {
                RemoveUnsettableAttributes("Retrieve", entity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            Utility.HandlePrecision(Metadata, db, entity);
#endif
            if (relatedEntityQuery != null) {
                AddRelatedEntities(entity, relatedEntityQuery, userRef);
            }

            return entity;
        }

        internal Entity GetEntityOrNull(EntityReference reference) {
            return db.GetEntityOrNull(reference);
        }

        internal Guid? GetEntityId(EntityReference reference) {
            if (reference?.Id != Guid.Empty) return reference.Id;
            var dbEntity = db.GetEntityOrNull(reference);
            return dbEntity?.Id;
        }

        internal OptionSetMetadataBase[] RetrieveAllOptionSets() {
            return Metadata.OptionSets;
        }

        internal OptionSetMetadataBase RetrieveOptionSet(string name) {
            return Metadata.OptionSets.Where(x => x.Name == name).FirstOrDefault();
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
                        var entityAttributes = db.GetEntityOrNull(entity.ToEntityReference()).Attributes;
                        if (entityAttributes.ContainsKey(oneToMany.ReferencingAttribute) && entityAttributes[oneToMany.ReferencingAttribute] != null) {
                            var referencingGuid = Utility.GetGuidFromReference(entityAttributes[oneToMany.ReferencingAttribute]);
                            queryExpr.Criteria.AddCondition(
                                new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencedAttribute, ConditionOperator.Equal, referencingGuid));
                        }
                    } else {
                        queryExpr.Criteria.AddCondition(
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression(oneToMany.ReferencingAttribute, ConditionOperator.Equal, entity.Id));
                    }
                }

                if (manyToMany != null) {
                    if (db[manyToMany.IntersectEntityName].Count() > 0) {
                        var conditions = new FilterExpression(LogicalOperator.Or);
                        if (entity.LogicalName == manyToMany.Entity1LogicalName) {
                            queryExpr.EntityName = manyToMany.Entity2LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute));

                            foreach (var id in relatedIds) {
                                conditions.AddCondition(
                                    new Microsoft.Xrm.Sdk.Query.ConditionExpression(null, ConditionOperator.Equal, id));
                            }
                        } else {
                            queryExpr.EntityName = manyToMany.Entity1LogicalName;
                            var relatedIds = db[manyToMany.IntersectEntityName]
                                .Where(row => row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == entity.Id)
                                .Select(row => row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute));

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
            var entity = db.GetEntityOrNull(reference);
            if (entity == null) {
                return null;
            }
            if (entity.RelatedEntities.Count() > 0) {
                var clone = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
                db.Update(clone);
                entity = clone;
            }
            var relationQuery = new RelationshipQueryCollection();
            var metadata =
                primaryEntityRole == EntityRole.Referenced ? EntityMetadata[entity.LogicalName].OneToManyRelationships : EntityMetadata[entity.LogicalName].ManyToOneRelationships;
            foreach (var relationshipMeta in metadata) {
                var query = new QueryExpression(relationshipMeta.ReferencingEntity);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName, PrimaryEntityRole = primaryEntityRole }, query);
            }

            foreach (var relationshipMeta in EntityMetadata[entity.LogicalName].ManyToManyRelationships) {
                var query = new QueryExpression(relationshipMeta.IntersectEntityName);
                query.ColumnSet = new ColumnSet(true);
                relationQuery.Add(new Relationship() { SchemaName = relationshipMeta.SchemaName }, query);
            }
            AddRelatedEntities(entity, relationQuery, userRef);
            return entity;
        }

        internal void CloseOpportunity(OpportunityState state, OptionSetValue status, Entity opportunityClose, EntityReference userRef) {
            SetState(opportunityClose.GetAttributeValue<EntityReference>("opportunityid"), new OptionSetValue((int)state), status, userRef);
            var create = new CreateRequest();
            create.Target = opportunityClose;
            Core.Execute(create as OrganizationRequest, userRef);
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

        internal void HandleInternalPreOperations(OrganizationRequest request, EntityReference userRef) {
            if (request.RequestName == "Create") {
                var entity = request["Target"] as Entity;
                if (entity.Id == Guid.Empty) {
                    entity.Id = Guid.NewGuid();
                }
                if (entity.GetAttributeValue<EntityReference>("ownerid") == null &&
                    Utility.IsSettableAttribute("ownerid", EntityMetadata.GetMetadata(entity.LogicalName))) {
                    entity["ownerid"] = userRef;
                }
            }
        }

        internal Guid Create<T>(T entity, EntityReference userRef) where T : Entity {
            return Create(entity, userRef, MockupServiceSettings.Role.SDK);
        }

        internal Guid Create<T>(T entity, EntityReference userRef, MockupServiceSettings.Role serviceRole) where T : Entity {
            if (entity.LogicalName == null) throw new MockupException("Entity needs a logical name");

            var clonedEntity = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
            var validAttributes = clonedEntity.Attributes.Where(x => x.Value != null);
            clonedEntity.Attributes = new AttributeCollection();
            clonedEntity.Attributes.AddRange(validAttributes);


            if (userRef != null && userRef.Id != Guid.Empty && !HasPermission(clonedEntity, AccessRights.CreateAccess, userRef)) {
                throw new FaultException($"Trying to create entity '{entity.LogicalName}'" +
                    $", but the calling user with id '{userRef.Id}' does not have create access for that entity");
            }

            if (Utility.HasCircularReference(Metadata.EntityMetadata, clonedEntity)) {
                throw new FaultException($"Trying to create entity '{clonedEntity.LogicalName}', but the attributes had a circular reference");
            }

            var transactioncurrencyId = "transactioncurrencyid";
            var exchangerate = "exchangerate";
            if (!clonedEntity.Attributes.ContainsKey(transactioncurrencyId) &&
                Utility.IsSettableAttribute(transactioncurrencyId, EntityMetadata[clonedEntity.LogicalName]) &&
                EntityMetadata[clonedEntity.LogicalName].Attributes.Any(m => m is MoneyAttributeMetadata) &&
                (serviceRole == MockupServiceSettings.Role.UI ||
                (serviceRole == MockupServiceSettings.Role.SDK && clonedEntity.Attributes.Any(
                    attr => EntityMetadata[clonedEntity.LogicalName].Attributes.Where(a => a is MoneyAttributeMetadata).Any(m => m.LogicalName == attr.Key))))) {
                var user = db.GetEntity(userRef);
                if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                    clonedEntity.Attributes[transactioncurrencyId] = user[transactioncurrencyId];
                } else {
                    clonedEntity.Attributes[transactioncurrencyId] = baseCurrency;
                }
            }

            if (!clonedEntity.Attributes.ContainsKey(exchangerate) &&
                Utility.IsSettableAttribute(exchangerate, EntityMetadata[clonedEntity.LogicalName]) &&
                clonedEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                var currencyId = clonedEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db[LogicalNames.TransactionCurrency][currencyId.Id];
                clonedEntity.Attributes[exchangerate] = currency["exchangerate"];
                Utility.HandleCurrencies(Metadata, db, clonedEntity);
            }

            var attributes = GetMetadata(clonedEntity.LogicalName).Attributes;
            if (!clonedEntity.Attributes.ContainsKey("statecode") &&
                Utility.IsSettableAttribute("statecode", EntityMetadata[clonedEntity.LogicalName])) {
                var stateMeta = attributes.First(a => a.LogicalName == "statecode") as StateAttributeMetadata;
                clonedEntity["statecode"] = stateMeta.DefaultFormValue.HasValue ? new OptionSetValue(stateMeta.DefaultFormValue.Value) : new OptionSetValue(0);
            }
            if (!clonedEntity.Attributes.ContainsKey("statuscode") &&
                Utility.IsSettableAttribute("statuscode", EntityMetadata[clonedEntity.LogicalName])) {
                var statusMeta = attributes.FirstOrDefault(a => a.LogicalName == "statuscode") as StatusAttributeMetadata;
                clonedEntity["statuscode"] = statusMeta.DefaultFormValue.HasValue ? new OptionSetValue(statusMeta.DefaultFormValue.Value) : new OptionSetValue(1);
            }

            if (Utility.IsSettableAttribute("createdon", EntityMetadata[clonedEntity.LogicalName])) {
                clonedEntity["createdon"] = DateTime.Now.Add(Core.TimeOffset);
            }
            if (Utility.IsSettableAttribute("createdby", EntityMetadata[clonedEntity.LogicalName])) {
                clonedEntity["createdby"] = userRef;
            }

            if (Utility.IsSettableAttribute("modifiedon", EntityMetadata[clonedEntity.LogicalName]) &&
                Utility.IsSettableAttribute("modifiedby", EntityMetadata[clonedEntity.LogicalName])) {
                clonedEntity["modifiedon"] = clonedEntity["createdon"];
                clonedEntity["modifiedby"] = clonedEntity["createdby"];
            }

            var owner = userRef;
            if (clonedEntity.Attributes.ContainsKey("ownerid")) {
                owner = clonedEntity.GetAttributeValue<EntityReference>("ownerid");
            }
            Utility.SetOwner(db, this, Metadata, clonedEntity, owner);

            if (!clonedEntity.Attributes.ContainsKey("businessunitid") &&
                clonedEntity.LogicalName == LogicalNames.SystemUser || clonedEntity.LogicalName == LogicalNames.Team) {
                clonedEntity["businessunitid"] = RootBusinessUnitRef;
            }

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit && !clonedEntity.Attributes.ContainsKey("parentbusinessunitid")) {
                clonedEntity["parentbusinessunitid"] = RootBusinessUnitRef;
            }
            if (serviceRole == MockupServiceSettings.Role.UI) {
                foreach (var attr in EntityMetadata[clonedEntity.LogicalName].Attributes.Where(a => (a as BooleanAttributeMetadata)?.DefaultValue != null).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = (attr as BooleanAttributeMetadata).DefaultValue;
                    }
                }

                foreach (var attr in EntityMetadata[clonedEntity.LogicalName].Attributes.Where(a =>
                    (a as PicklistAttributeMetadata)?.DefaultFormValue != null && (a as PicklistAttributeMetadata)?.DefaultFormValue.Value != -1).ToList()) {
                    if (!clonedEntity.Attributes.Any(a => a.Key == attr.LogicalName)) {
                        clonedEntity[attr.LogicalName] = new OptionSetValue((attr as PicklistAttributeMetadata).DefaultFormValue.Value);
                    }
                }
            }

            if (clonedEntity.LogicalName == LogicalNames.Contact || clonedEntity.LogicalName == LogicalNames.Lead || clonedEntity.LogicalName == LogicalNames.SystemUser) {
                Utility.SetFullName(Metadata, clonedEntity);
            }


            db.Add(clonedEntity);

            if (clonedEntity.LogicalName == LogicalNames.BusinessUnit) {
                AddRolesForBusinessUnit(clonedEntity.ToEntityReference());
            }

            if (entity.RelatedEntities.Count > 0) {
                foreach (var relatedEntities in entity.RelatedEntities) {
                    if (Utility.GetRelationshipMetadataDefaultNull(Metadata.EntityMetadata, relatedEntities.Key.SchemaName, Guid.Empty, userRef) == null) {
                        throw new FaultException($"Relationship with schemaname '{relatedEntities.Key.SchemaName}' does not exist in metadata");
                    }
                    foreach (var relatedEntity in relatedEntities.Value.Entities) {
                        var req = new CreateRequest() {
                            Target = relatedEntity
                        };
                        Core.Execute(req, userRef);
                    }
                    Associate(entity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()), userRef);
                }
            }

            return clonedEntity.Id;
        }

        internal EntityCollection RetrieveMultiple(QueryBase query, EntityReference userRef) {
            var queryExpr = query as QueryExpression;
            var fetchExpr = query as FetchExpression;
            if (queryExpr == null) {
                queryExpr = XmlHandling.FetchXmlToQueryExpression(fetchExpr.Query);
            }

            var collection = new EntityCollection();
            if (db[queryExpr.EntityName].Count() > 0) {
                foreach (var row in db[queryExpr.EntityName]) {
                    if (!Utility.MatchesCriteria(row, queryExpr.Criteria)) continue;
                    var entity = row.ToEntity();
                    var toAdd = GetStronglyTypedEntity(entity, null);

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
            if (toKeep != null && !toKeep.AllColumns)
                clone.Attributes.AddRange(entity.Attributes.Where(x => x.Key.Contains(".")));
            entity.Attributes.Clear();
            entity.Attributes.AddRange(clone.Attributes);
        }


        private object GetComparableAttribute(object attribute) {
            if (attribute is Money money) {
                return money.Value;
            }
            if (attribute is EntityReference eRef) {
                return eRef.Name;
            }
            if (attribute is OptionSetValue osv) {
                return osv.Value;
            }
            return attribute;
        }

        private List<Entity> GetAliasedValuesFromLinkentity(LinkEntity linkEntity, Entity parent, Entity toAdd, XrmDb db) {
            var collection = new List<Entity>();


            foreach (var linkedRow in db[linkEntity.LinkToEntityName]) {

                if (!Utility.MatchesCriteria(linkedRow, linkEntity.LinkCriteria)) continue;
                var linkedEntity = linkedRow.ToEntity();

                if (linkedEntity.Attributes.ContainsKey(linkEntity.LinkToAttributeName) &&
                    parent.Attributes.ContainsKey(linkEntity.LinkFromAttributeName)) {
                    var linkedAttr = Utility.ConvertToComparableObject(
                        linkedEntity.Attributes[linkEntity.LinkToAttributeName]);
                    var entAttr = Utility.ConvertToComparableObject(
                            parent.Attributes[linkEntity.LinkFromAttributeName]);

                    if (linkedAttr.Equals(entAttr)) {

                        var aliasedEntity = GetEntityWithAliasAttributes(linkEntity.EntityAlias, toAdd,
                                linkedEntity.Attributes, linkEntity.Columns);

                        if (linkEntity.LinkEntities.Count > 0) {
                            var subEntities = new List<Entity>();
                            foreach (var nestedLinkEntity in linkEntity.LinkEntities) {
                                nestedLinkEntity.LinkFromEntityName = linkEntity.LinkToEntityName;
                                subEntities.AddRange(
                                    GetAliasedValuesFromLinkentity(
                                        nestedLinkEntity, linkedEntity, aliasedEntity, db));
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
            var parentClone = GetStronglyTypedEntity(toAdd, null);
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
            var entRef = entity.ToEntityReferenceWithKeyAttributes();
            var row = db.GetDbRow(entRef);

            if (serviceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName != LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1) {
                throw new MockupException($"Trying to update inactive '{row.Table.TableName}', which is impossible in UI");
            }

            if (serviceRole == MockupServiceSettings.Role.UI &&
                row.Table.TableName == LogicalNames.Opportunity &&
                row.GetColumn<int?>("statecode") == 1) {
                throw new MockupException($"Trying to update closed opportunity '{row.Id}', which is impossible in UI");
            }

            // modify for all activites
            //if (entity.LogicalName == "activity" && dbEntity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 1) return;
            var xrmEntity = row.ToEntity();

            if (!HasPermission(row.ToEntity(), AccessRights.WriteAccess, userRef)) {
                throw new FaultException($"Trying to update entity '{row.Table.TableName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }

            var ownerRef = entity.GetAttributeValue<EntityReference>("ownerid");
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (ownerRef != null) {
                CheckAssignPermission(xrmEntity, ownerRef, userRef);
            }
#endif

            var updEntity = entity.CloneEntity(GetMetadata(entity.LogicalName), new ColumnSet(true));
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
            CheckStatusTransitions(updEntity, xrmEntity);
#endif


            if (Utility.HasCircularReference(Metadata.EntityMetadata, updEntity)) {
                throw new FaultException($"Trying to create entity '{xrmEntity.LogicalName}', but the attributes had a circular reference");
            }

            if (updEntity.LogicalName == LogicalNames.Contact || updEntity.LogicalName == LogicalNames.Lead || updEntity.LogicalName == LogicalNames.SystemUser) {
                Utility.SetFullName(Metadata, updEntity);
            }

            xrmEntity.SetAttributes(updEntity.Attributes, EntityMetadata[updEntity.LogicalName]);

            var transactioncurrencyId = "transactioncurrencyid";
            if (updEntity.LogicalName != LogicalNames.TransactionCurrency &&
                (updEntity.Attributes.ContainsKey(transactioncurrencyId) ||
                updEntity.Attributes.Any(a => EntityMetadata[xrmEntity.LogicalName].Attributes.Any(m => m.LogicalName == a.Key && m is MoneyAttributeMetadata)))) {
                if (!xrmEntity.Attributes.ContainsKey(transactioncurrencyId)) {
                    var user = db.GetEntity(userRef);
                    if (user.Attributes.ContainsKey(transactioncurrencyId)) {
                        xrmEntity[transactioncurrencyId] = user[transactioncurrencyId];
                    } else {
                        xrmEntity[transactioncurrencyId] = baseCurrency;
                    }
                }
                var currencyId = xrmEntity.GetAttributeValue<EntityReference>(transactioncurrencyId);
                var currency = db.GetEntity(LogicalNames.TransactionCurrency, currencyId.Id);
                xrmEntity["exchangerate"] = currency.GetAttributeValue<decimal?>("exchangerate");
                Utility.HandleCurrencies(Metadata, db, xrmEntity);
            }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
            if (updEntity.Attributes.ContainsKey("statecode") || updEntity.Attributes.ContainsKey("statuscode")) {
                Utility.HandleCurrencies(Metadata, db, xrmEntity);
            }
#endif

            if (ownerRef != null) {
                Utility.SetOwner(db, this, Metadata, xrmEntity, ownerRef);
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
                CascadeOwnerUpdate(xrmEntity, userRef, ownerRef);
#endif
            }
            Touch(xrmEntity, userRef);

            db.Update(xrmEntity);
        }


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal void CascadeOwnerUpdate(Entity dbEntity, EntityReference userRef, EntityReference ownerRef) {
            // Cascade like Assign, but with UpdateRequests
            foreach (var relatedEntities in GetDbEntityWithRelatedEntities(dbEntity.ToEntityReference(), EntityRole.Referenced, userRef).RelatedEntities) {
                var relationshipMeta = EntityMetadata[dbEntity.LogicalName].OneToManyRelationships.FirstOrDefault(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta == null) continue;

                var req = new UpdateRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign) {
                    case CascadeType.Cascade:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                            req.Target.Attributes["ownerid"] = ownerRef;
                            Core.Execute(req, userRef, null);
                        }
                        break;

                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<OptionSetValue>("statecode")?.Value == 0) {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;

                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<EntityReference>("ownerid")?.Id == currentOwner.Id) {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
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
            var record = db.GetEntity(entityRef);

            if (Utility.IsSettableAttribute("statecode", EntityMetadata[record.LogicalName]) &&
                Utility.IsSettableAttribute("statuscode", EntityMetadata[record.LogicalName])) {
                var prevEntity = record.CloneEntity();
                record["statecode"] = state;
                record["statuscode"] = status;
#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
                CheckStatusTransitions(record, prevEntity);
#endif
                Utility.HandleCurrencies(Metadata, db, record);
                Touch(record, userRef);

                db.Update(record);
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
                var relationshipMeta = EntityMetadata[dbEntity.LogicalName].OneToManyRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                var req = new AssignRequest();
                switch (relationshipMeta.CascadeConfiguration.Assign) {
                    case CascadeType.Cascade:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            req.Target = relatedEntity.ToEntityReference();
                            req.Assignee = assignee;
                            Core.Execute(req, userRef, null);
                        }
                        break;
                    case CascadeType.Active:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<OptionSetValue>("statecode")?.Value == 0) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = assignee;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;
                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if (db.GetEntity(relatedEntity.ToEntityReference()).GetAttributeValue<EntityReference>("ownerid")?.Id == currentOwner.Id) {
                                req.Target = relatedEntity.ToEntityReference();
                                req.Assignee = assignee;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;
                }
            }
            Utility.SetOwner(db, this, Metadata, dbEntity, assignee);
            Touch(dbEntity, userRef);
        }



        internal RelationshipMetadataBase GetRelationshipMetadata(string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                throw new FaultException("Relationship name is required when MetadataId is not specified");
            }
            var metadata = Utility.GetRelationshipMetadataDefaultNull(Metadata.EntityMetadata, name, metadataId, userRef);
            if (metadata == null) {
                throw new FaultException("Could not find relationship");
            }
            return metadata;
        }


        internal EntityMetadata GetEntityMetadata(string name, Guid metadataId, EntityReference userRef) {
            if (name == null && metadataId == Guid.Empty) {
                throw new FaultException("Entity logical name is required when MetadataId is not specified");
            }
            if (name != null && EntityMetadata.ContainsKey(name)) return EntityMetadata[name];
            if (metadataId != Guid.Empty) return EntityMetadata.FirstOrDefault(x => x.Value.MetadataId == metadataId).Value;
            throw new FaultException("Could not find matching entity");
        }


        internal void Associate(EntityReference target, Relationship relationship, EntityReferenceCollection relatedEntities, EntityReference userRef) {
            var relatedLogicalName = relatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as ManyToManyRelationshipMetadata;

            var targetEntity = db.GetEntity(target);
            if (!HasPermission(targetEntity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!HasPermission(targetEntity, AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to append to entity '{target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to append entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef));
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
                        var dbEntity = db.GetEntity(relatedEntity);
                        dbEntity[oneToMany.ReferencingAttribute] = target;
                        Touch(dbEntity, userRef);
                        db.Update(dbEntity);
                    }
                } else {
                    if (relatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (relatedEntities.Count == 1) {
                        var related = relatedEntities.First();
                        var dbEntity = db.GetEntity(target);
                        dbEntity[oneToMany.ReferencingAttribute] = related;
                        Touch(dbEntity, userRef);
                        db.Update(dbEntity);
                    }

                }
            }
        }

        internal void Disassociate(EntityReference target, Relationship relationship, EntityReferenceCollection relatedEntities, EntityReference userRef) {
            var relatedLogicalName = relatedEntities.FirstOrDefault()?.LogicalName;
            var oneToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as OneToManyRelationshipMetadata;
            var manyToMany = GetRelatedEntityMetadata(relatedLogicalName, relationship.SchemaName) as ManyToManyRelationshipMetadata;

            var targetEntity = db.GetEntity(target);
            if (!HasPermission(targetEntity, AccessRights.ReadAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{target.LogicalName}'" +
                    ", but the calling user does not have read access for that entity");
            }

            if (!HasPermission(targetEntity, AccessRights.AppendToAccess, userRef)) {
                throw new FaultException($"Trying to unappend to entity '{target.LogicalName}'" +
                    ", but the calling user does not have append to access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(db.GetEntity(r), AccessRights.ReadAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have read access for that entity");
            }

            if (relatedEntities.Any(r => !HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef))) {
                var firstError = relatedEntities.First(r => !HasPermission(db.GetEntity(r), AccessRights.AppendAccess, userRef));
                throw new FaultException($"Trying to unappend entity '{firstError.LogicalName}'" +
                    $" to '{target.LogicalName}', but the calling user does not have append access for that entity");
            }

            if (manyToMany != null) {
                foreach (var relatedEntity in relatedEntities) {
                    if (target.LogicalName == manyToMany.Entity1LogicalName) {
                        var link = db[manyToMany.IntersectEntityName]
                            .First(row =>
                                row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == target.Id &&
                                row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == relatedEntity.Id
                            );

                        db[manyToMany.IntersectEntityName].Remove(link.Id);
                    } else {
                        var link = db[manyToMany.IntersectEntityName]
                            .First(row =>
                                row.GetColumn<Guid>(manyToMany.Entity1IntersectAttribute) == relatedEntity.Id &&
                                row.GetColumn<Guid>(manyToMany.Entity2IntersectAttribute) == target.Id
                            );
                        db[manyToMany.IntersectEntityName].Remove(link.Id);
                    }
                }
            } else {
                if (oneToMany.ReferencedEntity == target.LogicalName) {
                    foreach (var relatedEntity in relatedEntities) {
                        var dbEntity = db.GetEntity(relatedEntity);
                        RemoveAttribute(dbEntity, oneToMany.ReferencingAttribute);
                        Touch(dbEntity, userRef);
                        db.Update(dbEntity);
                    }
                } else {
                    if (relatedEntities.Count > 1) {
                        throw new FaultException("Only one related entity is supported for Many to One relationship association.");
                    }
                    if (relatedEntities.Count == 1) {
                        var related = relatedEntities.First();
                        var dbEntity = db.GetEntity(target);
                        RemoveAttribute(dbEntity, oneToMany.ReferencingAttribute);
                        Touch(dbEntity, userRef);
                        db.Update(dbEntity);
                    }
                }
            }
        }

        internal void Merge(EntityReference target, Guid subordinateId, Entity updateContent,
            bool performParentingChecks, EntityReference userRef) {
            var mainEntity = db.GetEntity(target);
            var subordinateReference = new EntityReference { LogicalName = target.LogicalName, Id = subordinateId };
            var subordinateEntity = GetDbEntityWithRelatedEntities(subordinateReference, EntityRole.Referencing, userRef);

            foreach (var attr in updateContent.Attributes) {
                if (attr.Value != null) {
                    mainEntity[attr.Key] = attr.Value;
                }
            }

            foreach (var relatedEntities in subordinateEntity.RelatedEntities) {
                var relationshipMeta = EntityMetadata[subordinateEntity.LogicalName].ManyToOneRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta.CascadeConfiguration.Merge == CascadeType.Cascade) {
                    var entitiesToSwap = relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList();
                    Disassociate(subordinateEntity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(entitiesToSwap), userRef);
                    Associate(mainEntity.ToEntityReference(), relatedEntities.Key,
                        new EntityReferenceCollection(entitiesToSwap), userRef);
                }
            }

            subordinateEntity["merged"] = true;
            db.Update(subordinateEntity);
            db.Update(mainEntity);
            SetState(subordinateReference, new OptionSetValue(1), new OptionSetValue(2), userRef);
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013)
        internal Entity CalculateRollUpField(EntityReference entRef, string fieldName, EntityReference userRef) {
            var dbEntity = db.GetEntity(entRef);
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
                    var resultTree = tree.Execute(dbEntity, Core.TimeOffset, service, serviceFactory, trace);
                    var resultLocaltion = ((resultTree.StartActivity as RollUp).Aggregation[1] as Aggregate).VariableName;
                    var result = resultTree.Variables[resultLocaltion];
                    if (result != null) {
                        dbEntity[fieldName] = result;
                    }
                }
            }
            Utility.HandleCurrencies(Metadata, db, dbEntity);
            db.Update(dbEntity);
            return db.GetEntity(entRef);
        }
#endif

        private void Touch(Entity dbEntity, EntityReference user) {
            if (Utility.IsSettableAttribute("modifiedon", EntityMetadata[dbEntity.LogicalName]) && Utility.IsSettableAttribute("modifiedby", EntityMetadata[dbEntity.LogicalName])) {
                dbEntity["modifiedon"] = DateTime.Now.Add(Core.TimeOffset);
                dbEntity["modifiedby"] = user;
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

        internal void ResetEnvironment(XrmDb db) {
            Initialize(db);
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
            var user = db.GetEntity(entRef);
            var relationship = entRef.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = securityRoles
                .Select(sr => SecurityRoleMapping.Where(srm => srm.Value == sr).Select(srm => srm.Key))
                .Select(roleGuids =>
                    db["role"]
                    .Select(x => x.ToEntity())
                    .Where(r => roleGuids.Contains(r.Id))
                )
                .Select(roleEntities =>
                    roleEntities.First(e => e.GetAttributeValue<EntityReference>("businessunitid").Id == user.GetAttributeValue<EntityReference>("businessunitid").Id))
                .Select(r => r.ToEntityReference());
            Associate(entRef, relationship, new EntityReferenceCollection(roles.ToList()), AdminUserRef);
        }


        internal HashSet<SecurityRole> GetSecurityRoles(EntityReference caller) {
            var securityRoles = new HashSet<SecurityRole>();
            var callerEntity = GetDbEntityWithRelatedEntities(caller, EntityRole.Referenced, AdminUserRef);
            if (callerEntity == null) return securityRoles;
            var relationship = caller.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = callerEntity.RelatedEntities.ContainsKey(relationship) ? callerEntity.RelatedEntities[relationship] : new EntityCollection();
            foreach (var role in roles.Entities) {
                securityRoles.Add(SecurityRoles[SecurityRoleMapping[role.Id]]);
            }
            return securityRoles;
        }

        private bool IsInBusinessUnit(Guid ownerId, Guid businessunitId) {
            var usersInBusinessUnit = db[LogicalNames.SystemUser].Select(x => x.ToEntity()).Where(u => u.GetAttributeValue<EntityReference>("businessunitid")?.Id == businessunitId);
            return usersInBusinessUnit.Any(u => ownerId == u.Id);
        }

        private bool IsInBusinessUnitTree(Guid ownerId, Guid businessunitId) {
            if (IsInBusinessUnit(ownerId, businessunitId)) return true;

            var childBusinessUnits = db[LogicalNames.BusinessUnit].Select(x => x.ToEntity()).Where(b => b.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id == businessunitId);
            return childBusinessUnits.Any(b => IsInBusinessUnitTree(ownerId, b.Id));

        }


        internal bool HasPermission(Entity entity, AccessRights access, EntityReference caller) {
            if (!SecurityRoles.Any(s => s.Value.Privileges.Any(p => p.Key == entity.LogicalName))) {
                // system has no security roles for this entity. Is a case with linkentities which have no security roles
                return true;
            }
            if (caller.Id == AdminUserRef.Id) return true;

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
                    entity["ownerid"] = caller;
                }
            }

            if (entity.Attributes.ContainsKey("ownerid")) {
                var owner = entity.GetAttributeValue<EntityReference>("ownerid");
                if (owner.Id == caller.Id) {
                    return true;
                }

                var callerRow = db.GetDbRow(caller);
                if (maxRole == PrivilegeDepth.Local) {
                    return IsInBusinessUnit(owner.Id, callerRow.GetColumn<DbRow>("businessunitid").Id);
                }
                if (maxRole == PrivilegeDepth.Deep) {
                    if (callerRow.GetColumn<DbRow>("parentbusinessunitid") != null) {
                        return IsInBusinessUnitTree(owner.Id, callerRow.GetColumn<DbRow>("parentbusinessunitid").Id);
                    }
                    return IsInBusinessUnitTree(owner.Id, callerRow.GetColumn<DbRow>("businessunitid").Id);
                }
            }

            var entityRef = entity.ToEntityReference();
            if (Shares.ContainsKey(entityRef) &&
                Shares[entityRef].ContainsKey(caller) &&
                Shares[entityRef][caller].HasFlag(access)) {
                return true;
            }

            var parentChangeRelationships = GetMetadata(entity.LogicalName).ManyToOneRelationships
                .Where(r => r.CascadeConfiguration.Reparent == CascadeType.Cascade || r.CascadeConfiguration.Reparent == CascadeType.Active)
                .Where(r => entity.Attributes.ContainsKey(r.ReferencingAttribute));



            if (parentChangeRelationships.Any(r =>
                db.GetDbRowOrNull(r.ReferencedEntity, Utility.GetGuidFromReference(entity[r.ReferencingAttribute]))
                    ?.GetColumn<DbRow>("ownerid").Id == caller.Id)) {
                return true;
            }


            return false;
        }
    }
}