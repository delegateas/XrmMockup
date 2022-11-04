using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.IO;

namespace DG.Tools.XrmMockup
{
    using Privileges = Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>>;

    /// <summary>
    /// A Mockup of a CRM instance
    /// </summary>
    public abstract partial class XrmMockupBase {

        /// <summary>
        /// AdminUser for the Mockup instance
        /// </summary>
        public EntityReference AdminUser {
            get {
                return Core.AdminUserRef;
            }
        }

        /// <summary>
        /// AdminUser for the Mockup instance
        /// </summary>
        public EntityReference RootBusinessUnit {
            get {
                return Core.RootBusinessUnitRef;
            }
        }

        /// <summary>
        /// Base currency for the Mockup instance
        /// </summary>
        public EntityReference BaseCurrency {
            get {
                return Core.baseCurrency;
            }
        }


        private bool HasProxyTypes = false;
        private Core Core;
        private MockupServiceProviderAndFactory ServiceFactory;


        /// <summary>
        /// Create a new XrmMockup instance
        /// </summary>
        /// <param name="settings"></param>
        protected XrmMockupBase(XrmMockupSettings settings) {

            var metadataDirectory = "../../Metadata/";
            if (settings.MetadataDirectoryPath != null)
                metadataDirectory = settings.MetadataDirectoryPath;
            MetadataSkeleton metadata = Utility.GetMetadata(metadataDirectory);

            List<Entity> workflows = Utility.GetWorkflows(metadataDirectory);
            List<SecurityRole> securityRoles = Utility.GetSecurityRoles(metadataDirectory);

            this.Core = new Core(settings, metadata, workflows, securityRoles);
            this.ServiceFactory = new MockupServiceProviderAndFactory(this.Core);
            if (settings.EnableProxyTypes == true) {
                EnableProxyTypes();
            }
        }


        /// <summary>
        /// Enable early-bound types from the given context
        /// </summary>
        private void EnableProxyTypes() {
            if (HasProxyTypes) return;
            List<string> exclude = new List<string> {
                "Microsoft.Xrm.Sdk.dll",
                "Microsoft.Crm.Sdk.Proxy.dll"
            };

            var regex = new Regex("^XrmMockup.*\\.dll$");
            var assemblies = new List<Assembly>();
            var addedAssemblies = new HashSet<string>();

            var exeAsm = AppDomain.CurrentDomain.GetAssemblies();
            assemblies.AddRange(exeAsm);
            foreach (var name in exeAsm.Select(x => x.FullName))
            {
                addedAssemblies.Add(name);
            }

            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                var asm = Assembly.LoadFile(dll);
                if (addedAssemblies.Contains(asm.FullName)) continue;

                assemblies.Add(asm);
                addedAssemblies.Add(asm.FullName);
            }

            var useableAssemblies =
                assemblies
                .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                .Where(asm => !exclude.Contains(asm.ManifestModule.Name) && !regex.IsMatch(asm.ManifestModule.Name))
                .ToList();

            if (useableAssemblies?.Any() == true) {
                foreach (var asm in useableAssemblies)
                {
                    Core.EnableProxyTypes(asm);
                }
                HasProxyTypes = true;
            }
        }

        /// <summary>
        /// Resets the local test data
        /// </summary>
        public void ResetEnvironment() {
            Core.ResetEnvironment();
        }


        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, PluginContext pluginContext) {
            return Core.Execute(request, userRef, pluginContext);
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="offset"></param>
        public void AddTime(TimeSpan offset) {
            Core.AddTime(offset);
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        /// <param name="seconds"></param>
        public void AddTime(int days, int hours, int minutes, int seconds) {
            AddTime(new TimeSpan(days, hours, minutes, seconds));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        /// <param name="minutes"></param>
        public void AddTime(int days, int hours, int minutes) {
            AddTime(new TimeSpan(days, hours, minutes, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        /// <param name="hours"></param>
        public void AddTime(int days, int hours) {
            AddTime(new TimeSpan(days, hours, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="days"></param>
        public void AddTime(int days) {
            AddTime(new TimeSpan(days, 0, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of days
        /// </summary>
        /// <param name="days"></param>
        public void AddDays(int days) {
            AddTime(new TimeSpan(days, 0, 0, 0));
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of hours
        /// </summary>
        /// <param name="hours"></param>
        public void AddHours(int hours) {
            AddTime(new TimeSpan(hours, 0, 0));
        }

        /// <summary>
        /// Add a workflow from a serialized workflow entity
        /// </summary>
        /// <param name="path"></param>
        public void AddWorkflow(string path) {
            var workflow = Utility.GetWorkflow(path);
            Core.AddWorkflow(workflow);
        }

        /// <summary>
        /// Returns true if an entity exists in the database with the same id, and has at least the attributes of the parameter.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool ContainsEntity(Entity entity) {
            return Core.ContainsEntity(entity);
        }

        /// <summary>
        /// Adds entities directly into the database, without modifying them
        /// </summary>
        /// <param name="entities"></param>
        public void PopulateWith(params Entity[] entities) {
            Core.PopulateWith(entities);
        }

        /// <summary>
        /// Gets a system administrator organization service
        /// </summary>
        /// <returns></returns>
        public IOrganizationService GetAdminService() {
            return ServiceFactory.CreateAdminOrganizationService();
        }

        /// <summary>
        /// Gets a system administrator organization service, with the given settings
        /// </summary>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationService GetAdminService(MockupServiceSettings Settings) {
            return ServiceFactory.CreateAdminOrganizationService(Settings);
        }

        /// <summary>
        /// Create an organization service for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid userId) {
            return ServiceFactory.CreateOrganizationService(userId);
        }

        /// <summary>
        /// Create an organization service, with the given settings, for the systemuser with the given id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid userId, MockupServiceSettings settings) {
            return ServiceFactory.CreateOrganizationService(userId, settings);
        }

        /// <summary>
        /// Create a new user with a specific businessunit
        /// </summary>
        /// <param name="service"></param>
        /// <param name="businessUnit"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateUser(IOrganizationService service, EntityReference businessUnit, params Guid[] securityRoles) {
            var user = new Entity("systemuser");
            user["businessunitid"] = businessUnit;
            return CreateUser(service, user, securityRoles);
        }


        /// <summary>
        /// Create a new user from an entity. Remember to provide an existing businessunit in the entity.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="user"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateUser(IOrganizationService service, Entity user, params Guid[] securityRoles) {
            if (user.LogicalName != "systemuser") {
                throw new MockupException($"This method is only for creating users. You tried to create a '{user.LogicalName}'");
            }
            if (user.GetAttributeValue<EntityReference>("businessunitid") == null) {
                throw new MockupException("You tried to create a user with security roles, but did not specify a businessunit in the user's attributes");
            }
            user.Id = service.Create(user);
            Core.SetSecurityRoles(new EntityReference(LogicalNames.SystemUser, user.Id), securityRoles);
            return service.Retrieve(LogicalNames.SystemUser, user.Id, new ColumnSet(true));
        }

        /// <summary>
        /// Create a new owner team with a specific businessunit
        /// </summary>
        /// <param name="service"></param>
        /// <param name="businessUnit"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateTeam(IOrganizationService service, EntityReference businessUnit, params Guid[] securityRoles) {
            var team = new Entity(LogicalNames.Team);
            team["businessunitid"] = businessUnit;
            return CreateTeam(service, team, securityRoles);
        }

        /// <summary>
        /// Create a new owner team from an entity. Remember to provide an existing businessunit in the entity.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="team"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateTeam(IOrganizationService service, Entity team, params Guid[] securityRoles) {
            if (team.LogicalName != "team") {
                throw new MockupException($"This method is only for creating teams. You tried to create a '{team.LogicalName}'");
            }
            if (team.GetAttributeValue<EntityReference>("businessunitid") == null) {
                throw new MockupException("You tried to create a team with security roles, but did not specify a businessunit in the team's attributes");
            }
            team["teamtype"] = new OptionSetValue(0);
                team.Id = service.Create(team);
            Core.SetSecurityRoles(new EntityReference(LogicalNames.Team, team.Id), securityRoles);

            return service.Retrieve(LogicalNames.Team, team.Id, new ColumnSet(true));
        }

        /// <summary>
        /// Adds security roles to a given user or team
        /// </summary>
        /// <param name="priniple">User or Team</param>
        /// <param name="securityRoles">List of security role guids</param>
        public void AddSecurityRolesToPrinciple(EntityReference priniple, params Guid[] securityRoles)
        {
            Core.SetSecurityRoles(priniple, securityRoles);
        }

        /// <summary>
        /// Add users to a team
        /// </summary>
        /// <param name="team"></param>
        /// <param name="users"></param>
        public void AddUsersToTeam(EntityReference team, params EntityReference[] users)
        {
            var req = new AddMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = users.Select(x => x.Id).ToArray()
            };
            Core.Execute(req, AdminUser);
        }

        /// <summary>
        /// Remove users from a team
        /// </summary>
        /// <param name="team"></param>
        /// <param name="users"></param>
        public void RemoveUsersFromTeam(EntityReference team, params EntityReference[] users)
        {
            var req = new RemoveMembersTeamRequest
            {
                TeamId = team.Id,
                MemberIds = users.Select(x => x.Id).ToArray()
            };
            Core.Execute(req, AdminUser);
        }

        /// <summary>
        /// Takes a snapshot of the XrmMockup database
        /// </summary>
        /// <param name="snapshotName"></param>
        public void TakeSnapshot(string snapshotName)
        {
            Core.TakeSnapshot(snapshotName);
        }

        /// <summary>
        /// Retore the XrmMockup database from a snapshot
        /// </summary>
        /// <param name="snapshotName"></param>
        public void RestoreToSnapshot(string snapshotName)
        {
            Core.RestoreToSnapshot(snapshotName);
        }

        /// <summary>
        /// Takes a snapshot of the XrmMockup database data and saves it zip compressed
        /// </summary>
        /// <param name="filename">Filename used for the ZIP archive. Extenstion will be replaced with .zip</param>
        public void TakeZipSnapshot(string filename)
        {
            var json = Core.TakeJsonSnapshot();
            Utility.ZipCompressString(filename, json);
        }

        /// <summary>
        /// Retore the XrmMockup database data from json data saved in a zip archive
        /// </summary>
        /// <param name="filename">Filename of the ZIP archive to be restored</param>
        public void RestoreZipSnapshot(string filename)
        {
            var json = Utility.ZipUncompressString(filename);
            Core.RestoreJsonSnapshot(json);
        }

        /// <summary>
        /// Delete a stored snapshot
        /// </summary>
        /// <param name="snapshotName"></param>
        public void DeleteSnapshot(string snapshotName)
        {
            Core.DeleteSnapshot(snapshotName);
        }

        /// <summary>
        /// Disables triggering of registered plugins. Does not include temporarily plugins. Is set to false when <see cref="ResetEnvironment"/> is called.
        /// </summary>
        /// <param name="include"></param>
        public void DisableRegisteredPlugins(bool include)
        {
            Core.DisabelRegisteredPlugins(include);
        }

        /// <summary>
        /// Register additional plugins to be triggered in addition to the existing plugins in the current environment.
        /// Plugins registered temporarily will be deleted when <see cref="ResetEnvironment"/> is called.
        /// </summary>
        /// <param name="scope">The scope of the plugin registration</param>
        /// <param name="basePluginTypes"></param>
        public void RegisterAdditionalPlugins(PluginRegistrationScope scope, params Type[] basePluginTypes)
        {
            Core.RegisterAdditionalPlugins(basePluginTypes, scope);
        }

        /// <summary>
        /// Returns the privilege of the given principle
        /// </summary>
        /// <param name="principleId">Guid of user or team</param>
        /// <returns>A dictionary of entities where each entity contains a dictionary over access rights and privilege depth for the given principle</returns>
        public Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> GetPrivilege(Guid principleId)
        {
            return Core.GetPrivilege(principleId);
        }
        
        /// <summary>
        /// Checks if a principle has the given access right to an entity
        /// </summary>
        /// <param name="entityRef">Entity to check against</param>
        /// <param name="access">Access to check with</param>
        /// <param name="principleRef">User or team</param>
        /// <returns>If the given principle has permission to 'access' the entity</returns>
        public bool HasPermission(EntityReference entityRef, AccessRights access, EntityReference principleRef)
        {
            return Core.HasPermission(entityRef, access, principleRef);
        }

        /// <summary>
        /// Add entity privileges to the given principle ontop of any existing privileges and security roles
        /// </summary>
        /// <param name="principleRef">EntityReference of a user or team</param>
        /// <param name="privileges">A dictionary of entities where each entity contains a dictionary over access rights and privilege depth</param>
        internal void AddPrivileges(EntityReference principleRef, Dictionary<string, Dictionary<AccessRights, PrivilegeDepth>> privileges)
        {
            Core.AddPrivileges(principleRef, privileges);
        }

        public SecurityRole GetSecurityRole(string roleName)
        {
            return Core.GetSecurityRole(roleName);
        }
        public SecurityRole CloneSecurityRole(string roleName)
        {
            return Core.GetSecurityRole(roleName).Clone();
        }
        public void AddSecurityRole(SecurityRole role)
        {
            Core.AddSecurityRole(role);
        }

        public void ResetTable(string tableName)
        {
            Core.ResetTable(tableName);
        }
    }
}
