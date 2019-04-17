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

namespace DG.Tools.XrmMockup {

    /// <summary>
    /// A Mockup of a CRM instance
    /// </summary>
    public abstract class XrmMockupBase {

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
        /// add a code activity trigger from a known type
        /// </summary>
        /// <param name="type"></param>
        public void AddCodeActivityTrigger(Type type)
        {
            Core.AddCodeActivityTrigger(type);
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
        public void RegisterAdditionalPlugins(PluginRegistrationScope scope, StepDerivationType stepDerivationType, params Type[] basePluginTypes)
        {
            Core.RegisterAdditionalPlugins(basePluginTypes, scope, stepDerivationType);
        }

        /// <summary>
        /// allows you to register additional workflow activities from a list of compiled assemblies, present at a given location
        /// </summary>
        /// <param name="workFlowAssembliesPath"></param>
        /// <param name="matchingTypes">base types to match against eg. CodeActivity / AccountWorkflowActivity</param>
        /// <param name="filesToIgnore"></param>
        /// <param name="matchOnTypeFullName">If yes, a type equality test is ignored in favour of checking the FullName properrty for equality instead. Useful if types are being loaded from ilmerged assemblies</param>
        public void RegisterWorkflowCodeActivitiesFromExternalAssemblies(string workFlowAssembliesPath, List<Type> matchingTypes, List<string> filesToIgnore, bool matchOnTypeFullName = false)
        {
            if (!string.IsNullOrEmpty(workFlowAssembliesPath))
            {
                var workflowTypes = GetLoadableTypesFromAssembliesInPath(workFlowAssembliesPath, matchingTypes, filesToIgnore, matchOnTypeFullName);

                foreach (Type type in workflowTypes)
                {
                    AddCodeActivityTrigger(type);
                }
            }
        }

        /// <summary>
        /// allows you to register additional plugin steps from a list of compiled assemblies, present at a given location
        /// </summary>
        /// <param name="pluginAssembliesPath"></param>
        /// <param name="matchingTypes">base types to match against eg. Plugin</param>
        /// <param name="filesToIgnore"></param>
        /// <param name="stepDerivationType">where to get the plugin step registrations from. either as part of the plugin hardcoded, or as part of the live crm metadata</param>
        /// <param name="matchOnTypeFullName">If yes, a type equality test is ignored in favour of checking the FullName properrty for equality instead. Useful if types are being loaded from ilmerged assemblies</param>
        public void RegisterPluginsFromExternalAssemblies(string pluginAssembliesPath, List<Type> matchingTypes, List<string> filesToIgnore, StepDerivationType stepDerivationType, bool matchOnTypeFullName = false)
        {
            if (!string.IsNullOrEmpty(pluginAssembliesPath))
            {
                var pluginTypes = GetLoadableTypesFromAssembliesInPath(pluginAssembliesPath, matchingTypes,filesToIgnore, matchOnTypeFullName);

                foreach (Type type in pluginTypes)
                {
                    RegisterAdditionalPlugins(PluginRegistrationScope.Temporary, stepDerivationType, type );
                }
            }
        }

        private IEnumerable<Type> GetLoadableTypesFromAssembliesInPath(string path, List<Type> baseTypesAndInterfacesToLoad, List<string> filesToIgnore, bool matchOnTypeFullName = false)
        {
            List<Type> types = new List<Type>();

            List<Type> baseTypesToLoad = baseTypesAndInterfacesToLoad.Where(t => !t.IsInterface).ToList();
            List<Type> interfacesToLoad = baseTypesAndInterfacesToLoad.Where(t => t.IsInterface).ToList();


            try
            {
                DirectoryInfo d = new DirectoryInfo(path);
                if (Directory.Exists(path))
                {
                    FileInfo[] Files = d.GetFiles("*.dll"); //get libraries

                    foreach (FileInfo file in Files)
                    {
                        var isRestrictedFile = (filesToIgnore != null && filesToIgnore.Count > 0) &&
                                               filesToIgnore.Any(f => file.Name.IndexOf(f, StringComparison.Ordinal) > -1);

                        if (!isRestrictedFile)
                        {
                            var assembly = Assembly.LoadFrom(file.FullName);
                            var allTypes = assembly.GetTypes();
                            foreach (Type type in allTypes)
                            {
                                if (baseTypesToLoad.Any(ttl => ttl.FullName == type.FullName))
                                {
                                    //this type matches a base type so ignore it
                                    continue;
                                }

                                if (matchOnTypeFullName)
                                {
                                    if (type.BaseType != null && baseTypesToLoad.Any(p => p.FullName == type.BaseType.FullName))
                                    {
                                        //does it have an execute method?
                                        var executeMethod = type.BaseType.GetMethod("Execute");
                                        if (executeMethod != null)
                                        {
                                            types.Add(type);
                                        }
                                    }
                                    else if (type.GetInterfaces().Any(i => interfacesToLoad.Any(itl => itl.FullName == i.FullName))) //match against interfaces
                                    {
                                        //does it have an execute method?

                                        var executeMethod = type.GetMethod("Execute");
                                        if (executeMethod != null)
                                        {
                                            types.Add(type);
                                        }
                                    }
                                }
                                else
                                {
                                    if (type.BaseType!=null && baseTypesToLoad.Any(p => p == type.BaseType))
                                    {
                                        var executeMethod = type.BaseType.GetMethod("Execute");
                                        if (executeMethod != null)
                                        {
                                            types.Add(type);
                                        }
                                    }
                                    else if (type.GetInterfaces().Any(i => interfacesToLoad.Any(itl => itl == i))) //match against interfaces
                                    {
                                        var executeMethod = type.GetMethod("Execute");
                                        if (executeMethod != null)
                                        {
                                            types.Add(type);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


                return types;
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }

}
