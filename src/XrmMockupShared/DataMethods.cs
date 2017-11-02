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

        private Dictionary<string, Money> CalcAndRollupTrees;
        internal EntityReference baseCurrency;
        private int baseCurrencyPrecision;
        private Dictionary<string, EntityMetadata> EntityMetadata;
        private Dictionary<Guid, SecurityRole> SecurityRoles;
        private Dictionary<Guid, Guid> SecurityRoleMapping;

        public EntityReference AdminUserRef;
        public EntityReference RootBusinessUnitRef;

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
                Utility.RemoveAttribute(entity, "createdby", "modifiedby", "organizationid", "modifiedonbehalfby", "createdonbehalfby");
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

        internal void InitializeSecurityRoles() {
            AddRolesForBusinessUnit(RootBusinessUnitRef);
            SetSecurityRole(AdminUserRef,
                SecurityRoles
                .Where(s => s.Value.RoleTemplateId == new Guid("627090ff-40a3-4053-8790-584edc5be201")) // System administrator role template ID
                .Select(s => s.Value.RoleId)
                .ToArray());
        }

        internal EntityMetadata GetMetadata(string logicalName) {
            return EntityMetadata.GetMetadata(logicalName);
        }


        internal void Delete(string logicalName, Guid id, EntityReference userRef) {
            var entity = Core.GetDbEntityWithRelatedEntities(new EntityReference(logicalName, id), EntityRole.Referenced, userRef);
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
                                var req = new DeleteRequest {
                                    Target = new EntityReference(relatedEntity.LogicalName, relatedEntity.Id)
                                };
                                Core.Execute(req, userRef, null);
                            }
                            break;
                        case CascadeType.RemoveLink:
                            var disassocHandler = Core.RequestHandlers.Find(x => x is DisassociateRequestHandler);
                            disassocHandler.Execute(new DisassociateRequest {
                                RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()),
                                Relationship = relatedEntities.Key,
                                Target = entity.ToEntityReference()
                            }, userRef);
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

        private Entity RetrieveDefaultNull(EntityReference entRef, ColumnSet columnSet) {
            var dbRow = db.GetDbRowOrNull(entRef);
            if (dbRow == null) return null;
            return Core.GetStronglyTypedEntity(dbRow.ToEntity(), dbRow.Metadata, columnSet);
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


        internal void CloseOpportunity(OpportunityState state, OptionSetValue status, Entity opportunityClose, EntityReference userRef) {
            var setStateHandler = Core.RequestHandlers.Find(x => x is SetStateRequestHandler);
            var req = new SetStateRequest() {
                EntityMoniker = opportunityClose.GetAttributeValue<EntityReference>("opportunityid"),
                State = new OptionSetValue((int)state),
                Status = status
            };
            setStateHandler.Execute(req, userRef);

            var create = new CreateRequest {
                Target = opportunityClose
            };
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
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
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
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
            CheckSharingAccess(entity, principalAccess, userRef);

            if (Shares.ContainsKey(target) && Shares[target].ContainsKey(principalAccess.Principal)) {
                Shares[target][principalAccess.Principal] = principalAccess.AccessMask;
            }

        }
        internal void RevokeAccess(EntityReference target, EntityReference revokee, EntityReference userRef) {
            var entity = Core.GetDbEntityWithRelatedEntities(target, EntityRole.Referenced, userRef);
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
                    var assocHandler = Core.RequestHandlers.Find(x => x is AssociateRequestHandler);
                    assocHandler.Execute(new AssociateRequest {
                        RelatedEntities = new EntityReferenceCollection(relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList()),
                        Relationship = relatedEntities.Key,
                        Target = entity.ToEntityReference()
                    }, userRef);
                }
            }

            return clonedEntity.Id;
        }



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
            Utility.CheckStatusTransitions(row.Metadata, updEntity, xrmEntity);
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
            Utility.Touch(xrmEntity, EntityMetadata[xrmEntity.LogicalName], Core.TimeOffset, userRef);

            db.Update(xrmEntity);
        }


#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal void CascadeOwnerUpdate(Entity dbEntity, EntityReference userRef, EntityReference ownerRef) {
            // Cascade like Assign, but with UpdateRequests
            foreach (var relatedEntities in Core.GetDbEntityWithRelatedEntities(dbEntity.ToEntityReference(), EntityRole.Referenced, userRef).RelatedEntities) {
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

        internal void CheckAssignPermission(Entity entity, EntityReference assignee, EntityReference userRef) {
            if (!HasPermission(entity, AccessRights.AssignAccess, userRef)) {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                    $", but calling user with id '{userRef.Id}' does not have assign access for that entity");
            }

            if (!HasPermission(entity, AccessRights.WriteAccess, userRef)) {
                throw new FaultException($"Trying to assign '{assignee.Id}' to entity '{entity.LogicalName}'" +
                     $", but calling user with id '{userRef.Id}' does not have write access for that entity");
            }
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

        internal void Merge(EntityReference target, Guid subordinateId, Entity updateContent,
            bool performParentingChecks, EntityReference userRef) {
            var mainEntity = db.GetEntity(target);
            var subordinateReference = new EntityReference { LogicalName = target.LogicalName, Id = subordinateId };
            var subordinateEntity = Core.GetDbEntityWithRelatedEntities(subordinateReference, EntityRole.Referencing, userRef);

            foreach (var attr in updateContent.Attributes) {
                if (attr.Value != null) {
                    mainEntity[attr.Key] = attr.Value;
                }
            }

            foreach (var relatedEntities in subordinateEntity.RelatedEntities) {
                var relationshipMeta = EntityMetadata[subordinateEntity.LogicalName].ManyToOneRelationships.First(r => r.SchemaName == relatedEntities.Key.SchemaName);
                if (relationshipMeta.CascadeConfiguration.Merge == CascadeType.Cascade) {
                    var entitiesToSwap = relatedEntities.Value.Entities.Select(e => e.ToEntityReference()).ToList();

                    var disassocHandler = Core.RequestHandlers.Find(x => x is DisassociateRequestHandler);
                    disassocHandler.Execute(new DisassociateRequest {
                        RelatedEntities = new EntityReferenceCollection(entitiesToSwap),
                        Relationship = relatedEntities.Key,
                        Target = subordinateEntity.ToEntityReference()
                    }, userRef);

                    var assocHandler = Core.RequestHandlers.Find(x => x is AssociateRequestHandler);
                    assocHandler.Execute(new AssociateRequest {
                        RelatedEntities = new EntityReferenceCollection(entitiesToSwap),
                        Relationship = relatedEntities.Key,
                        Target = mainEntity.ToEntityReference()
                    }, userRef);
                }
            }

            subordinateEntity["merged"] = true;
            db.Update(subordinateEntity);
            db.Update(mainEntity);
            var setStateHandler = Core.RequestHandlers.Find(x => x is SetStateRequestHandler);
            var req = new SetStateRequest() {
                EntityMoniker = subordinateReference,
                State = new OptionSetValue(1),
                Status = new OptionSetValue(2)
            };
            setStateHandler.Execute(req, userRef);
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
                    var factory = Core.ServiceFactory;
                    var resultTree = tree.Execute(dbEntity, Core.TimeOffset, Core.GetWorkflowService(), factory, factory.GetService(typeof(ITracingService)) as ITracingService);
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
        
        internal void ResetEnvironment(XrmDb db) {
            Initialize(db);
            InitializeSecurityRoles();
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

            var assocHandler = Core.RequestHandlers.Find(x => x is AssociateRequestHandler);
            assocHandler.Execute(new AssociateRequest {
                RelatedEntities = new EntityReferenceCollection(roles.ToList()),
                Relationship = relationship,
                Target = entRef
            }, AdminUserRef);
        }


        internal HashSet<SecurityRole> GetSecurityRoles(EntityReference caller) {
            var securityRoles = new HashSet<SecurityRole>();
            var callerEntity = Core.GetDbEntityWithRelatedEntities(caller, EntityRole.Referenced, AdminUserRef);
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