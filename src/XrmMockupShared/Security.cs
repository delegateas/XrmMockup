using DG.Tools.XrmMockup.Database;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace DG.Tools.XrmMockup {
    internal class Security {
        private static Guid SYSTEMADMIN_ROLE_TEMPLATE = new Guid("627090ff-40a3-4053-8790-584edc5be201");
        private Core Core;
        private MetadataSkeleton Metadata;

        private Dictionary<Guid, SecurityRole> SecurityRoles;
        private Dictionary<Guid, Guid> SecurityRoleMapping;
        private Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>> Shares;

        internal Security(Core core, MetadataSkeleton metadata, List<SecurityRole> SecurityRoles) {
            this.Core = core;
            this.Metadata = metadata;
            this.SecurityRoles = SecurityRoles.ToDictionary(s => s.RoleId, s => s);

            Initialize();
        }

        private void Initialize() {
            this.SecurityRoleMapping = new Dictionary<Guid, Guid>();
            this.Shares = new Dictionary<EntityReference, Dictionary<EntityReference, AccessRights>>();
        }

        internal void AddRolesForBusinessUnit(XrmDb db, EntityReference businessUnit) {
            foreach (var sr in SecurityRoles.Values) {
                var roleMeta = Metadata.EntityMetadata.GetMetadata("role");
                var role = new Entity("role") {
                    Id = Guid.NewGuid()
                };
                role[roleMeta.PrimaryIdAttribute] = role.Id;
                role["businessunitid"] = businessUnit;
                role["name"] = sr.Name;
                role["roletemplateid"] = sr.RoleTemplateId;
                role["createdby"] = Core.AdminUserRef;
                role["createdon"] = DateTime.Now.Add(Core.TimeOffset);
                role["modifiedby"] = Core.AdminUserRef;
                role["modifiedon"] = DateTime.Now.Add(Core.TimeOffset);
                db.Add(role);
                SecurityRoleMapping.Add(role.Id, sr.RoleId);
            }
        }

        internal void InitializeSecurityRoles(XrmDb db) {
            AddRolesForBusinessUnit(db, Core.RootBusinessUnitRef);
            SetSecurityRole(Core.AdminUserRef,
                SecurityRoles
                .Where(s => s.Value.RoleTemplateId == SYSTEMADMIN_ROLE_TEMPLATE)
                .Select(s => s.Value.RoleId)
                .ToArray());
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
        internal AccessRights GetAccessRights(EntityReference target, EntityReference userRef)
        {
            var entity = Core.GetDbRow(target);
            return GetAccessRights(entity.ToEntity(), userRef);
        }

        internal AccessRights GetAccessRights(Entity target, EntityReference userRef)
        {
            var ret = AccessRights.None;
            ret |= HasPermission(target, AccessRights.CreateAccess, userRef) ? AccessRights.CreateAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.ReadAccess, userRef) ? AccessRights.ReadAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.WriteAccess, userRef) ? AccessRights.WriteAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.DeleteAccess, userRef) ? AccessRights.DeleteAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AppendAccess, userRef) ? AccessRights.AppendAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AppendToAccess, userRef) ? AccessRights.AppendToAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.AssignAccess, userRef) ? AccessRights.AssignAccess : AccessRights.None;
            ret |= HasPermission(target, AccessRights.ShareAccess, userRef) ? AccessRights.ShareAccess : AccessRights.None;
            return ret;
        }

#if !(XRM_MOCKUP_2011 || XRM_MOCKUP_2013 || XRM_MOCKUP_2015)
        internal void CascadeOwnerUpdate(Entity dbEntity, EntityReference userRef, EntityReference ownerRef) {
            // Cascade like Assign, but with UpdateRequests
            foreach (var relatedEntities in Core.GetDbEntityWithRelatedEntities(dbEntity.ToEntityReference(), EntityRole.Referenced, userRef).RelatedEntities) {
                var relationshipMeta = Metadata.EntityMetadata.GetMetadata(dbEntity.LogicalName).OneToManyRelationships.FirstOrDefault(r => r.SchemaName == relatedEntities.Key.SchemaName);
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
                            if (Core.GetDbRow(relatedEntity.ToEntityReference()).ToEntity().GetAttributeValue<OptionSetValue>("statecode")?.Value == 0) {
                                req.Target = new Entity(relatedEntity.LogicalName, relatedEntity.Id);
                                req.Target.Attributes["ownerid"] = ownerRef;
                                Core.Execute(req, userRef, null);
                            }
                        }
                        break;

                    case CascadeType.UserOwned:
                        foreach (var relatedEntity in relatedEntities.Value.Entities) {
                            var currentOwner = dbEntity.Attributes["ownerid"] as EntityReference;
                            if (Core.GetDbRow(relatedEntity.ToEntityReference()).ToEntity().GetAttributeValue<EntityReference>("ownerid")?.Id == currentOwner.Id) {
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
        
        internal void ResetEnvironment(XrmDb db) {
            Initialize();
            InitializeSecurityRoles(db);
        }

        internal void SetSecurityRole(EntityReference entRef, Guid[] securityRoles) {
            if (securityRoles.Any(s => !SecurityRoles.ContainsKey(s))) {
                throw new MockupException($"Unknown security role");
            }
            var user = Core.GetDbRow(entRef).ToEntity();
            var relationship = entRef.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = securityRoles
                .Select(sr => SecurityRoleMapping.Where(srm => srm.Value == sr).Select(srm => srm.Key))
                .Select(roleGuids =>
                    Core.GetDbTable("role")
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
            }, Core.AdminUserRef);
        }

        internal HashSet<SecurityRole> GetSecurityRoles(EntityReference caller) {
            var securityRoles = new HashSet<SecurityRole>();
            var callerEntity = Core.GetDbEntityWithRelatedEntities(caller, EntityRole.Referenced, Core.AdminUserRef);
            if (callerEntity == null) return securityRoles;
            var relationship = caller.LogicalName == LogicalNames.SystemUser ? new Relationship("systemuserroles_association") : new Relationship("teamroles_association");
            var roles = callerEntity.RelatedEntities.ContainsKey(relationship) ? callerEntity.RelatedEntities[relationship] : new EntityCollection();
            foreach (var role in roles.Entities) {
                securityRoles.Add(SecurityRoles[SecurityRoleMapping[role.Id]]);
            }
            return securityRoles;
        }

        private bool HasOwnerTeamAccess(EntityReference owner, EntityReference caller) {
            return Core.GetDbTable(LogicalNames.TeamMembership)
                .Select(x => x.ToEntity())
                .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == caller.Id && tm.GetAttributeValue<Guid>("teamid") == owner.Id)
                .Any();
        }

        private bool IsInBusinessUnit(EntityReference owner, Guid businessunitId, EntityReference caller) {
            var usersInBusinessUnit = Core.GetDbTable(LogicalNames.SystemUser).Select(x => x.ToEntity()).Where(u => u.GetAttributeValue<EntityReference>("businessunitid")?.Id == businessunitId);
            return usersInBusinessUnit.Any(u => owner.Id == u.Id);
        }

        private bool IsInBusinessUnitTree(EntityReference owner, Guid businessunitId, EntityReference caller) {
            if (IsInBusinessUnit(owner, businessunitId, caller)) return true;

            var childBusinessUnits = Core.GetDbTable(LogicalNames.BusinessUnit).Select(x => x.ToEntity()).Where(b => b.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id == businessunitId);
            return childBusinessUnits.Any(b => IsInBusinessUnitTree(owner, b.Id, caller));
        }

        internal bool HasTeamMemberPermission(Entity entity, AccessRights access, EntityReference caller)
        {
            if (!entity.Attributes.ContainsKey("ownerid") || caller.LogicalName != LogicalNames.SystemUser)
                return false;
            
            var owner = entity.GetAttributeValue<EntityReference>("ownerid");

            // Check owner access rights
            if (owner.LogicalName == LogicalNames.Team)
            {
                return HasOwnerTeamAccess(owner, caller);
            }

            // Check if teams have access
            var teamIds = Core.GetDbTable(LogicalNames.TeamMembership)
            .Select(x => x.ToEntity())
            .Where(tm => tm.GetAttributeValue<Guid>("systemuserid") == caller.Id)
            .Select(tm => tm.GetAttributeValue<Guid>("teamid"))
            .ToList();

            return teamIds.Any(teamId => HasPermission(entity, access, new EntityReference(LogicalNames.Team, teamId)));
        }

        internal bool HasPermission(string logicalName, AccessRights access, EntityReference caller)
        {
            if (!SecurityRoles.Any(s => s.Value.Privileges.Any(p => p.Key == logicalName)))
            {
                // system has no security roles for this entity. Is a case with linkentities which have no security roles
                return true;
            }
            if (caller.Id == Core.AdminUserRef.Id) return true;

            var callerRoles = GetSecurityRoles(caller)?.Where(r =>
                r.Privileges.ContainsKey(logicalName) &&
                r.Privileges[logicalName].ContainsKey(access));
            return callerRoles != null && callerRoles.Count() > 0;
        }

        internal bool HasPermission(EntityReference entityReference, AccessRights access, EntityReference caller)
        {
            return HasPermission(Core.GetDbRow(entityReference).ToEntity(), access, caller);
        }

        internal bool HasPermission(Entity entity, AccessRights access, EntityReference caller) {
            if (!SecurityRoles.Any(s => s.Value.Privileges.Any(p => p.Key == entity.LogicalName))) {
                // system has no security roles for this entity. Is a case with linkentities which have no security roles
                return true;
            }
            if (caller.Id == Core.AdminUserRef.Id) return true;

            var callerRoles = GetSecurityRoles(caller)?.Where(r =>
                r.Privileges.ContainsKey(entity.LogicalName) &&
                r.Privileges[entity.LogicalName].ContainsKey(access));
            if (callerRoles == null || callerRoles.Count() == 0) {
                return false;
            }
            var maxRole = callerRoles.Max(r => r.Privileges[entity.LogicalName][access].PrivilegeDepth);
            if (maxRole == PrivilegeDepth.Global) return true;

            if (access == AccessRights.CreateAccess) {
                if (!entity.Attributes.ContainsKey("ownerid")) {
                    entity["ownerid"] = caller;
                }
            }

            // Owner Check
            if (entity.Attributes.ContainsKey("ownerid")) {
                var owner = entity.GetAttributeValue<EntityReference>("ownerid");
                if (owner.Id == caller.Id) {
                    return true;
                }

                var callerRow = Core.GetDbRow(caller);

                if (maxRole == PrivilegeDepth.Local) {

                    if (IsInBusinessUnit(owner, callerRow.GetColumn<DbRow>("businessunitid").Id, caller))
                        return true; 
                }
                if (maxRole == PrivilegeDepth.Deep) {
                    if (callerRow.GetColumn<DbRow>("parentbusinessunitid") != null && IsInBusinessUnitTree(owner, callerRow.GetColumn<DbRow>("parentbusinessunitid").Id, caller))
                        return true;
                    else if (IsInBusinessUnitTree(owner, callerRow.GetColumn<DbRow>("businessunitid").Id, caller))
                        return true;
                }
            }

            // check if any of the team that the user is a member of has access
            if(caller.LogicalName == LogicalNames.SystemUser && HasTeamMemberPermission(entity, access, caller))
            {
                return true;
            }

            var entityRef = entity.ToEntityReference();
            if (Shares.ContainsKey(entityRef) &&
                Shares[entityRef].ContainsKey(caller) &&
                Shares[entityRef][caller].HasFlag(access)) {
                return true;
            }

            var parentChangeRelationships = Metadata.EntityMetadata.GetMetadata(entity.LogicalName).ManyToOneRelationships
                .Where(r => r.CascadeConfiguration.Reparent == CascadeType.Cascade || r.CascadeConfiguration.Reparent == CascadeType.Active)
                .Where(r => entity.Attributes.ContainsKey(r.ReferencingAttribute));

            if (parentChangeRelationships.Any(r =>
                Core.GetDbRowOrNull(new EntityReference(r.ReferencedEntity, Utility.GetGuidFromReference(entity[r.ReferencingAttribute])))
                    ?.GetColumn<DbRow>("ownerid").Id == caller.Id)) {
                return true;
            }

            return false;
        }

        public Security Clone()
        {
            var security = new Security(this.Core, this.Metadata, this.SecurityRoles.Values.ToList());
            security.SecurityRoleMapping = this.SecurityRoleMapping.ToDictionary(x => x.Key, x => x.Value);
            security.Shares = this.Shares.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));
            return security;
        }
    }
}