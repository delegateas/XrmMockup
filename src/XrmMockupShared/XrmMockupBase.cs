using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace DG.Tools {

    /// <summary>
    /// A Mockup of a CRM instance
    /// </summary>
    public abstract class XrmMockupBase {
        /// <summary>
        /// AdminUser for the Mockup instance
        /// </summary>
        public EntityReference AdminUser { get; private set; }

        /// <summary>
        /// Organization id for the Mockup instance
        /// </summary>
        public Guid OrganizationId { get; private set; }

        /// <summary>
        /// Organization name for the Mockup instance
        /// </summary>
        public string OrganizationName { get; private set; }
        /// <summary>
        /// Root businessunit for the Mockup instance
        /// </summary>
        public EntityReference RootBusinessUnit { get; private set; }
        
        private static bool HasProxyTypes = false;
        private RequestHandler RequestHandler;
        private Guid AdminuserId;
        private MockupServiceProviderAndFactory ServiceFactory;

        internal TimeSpan TimeOffset;

        /// <summary>
        /// Create a new XrmMockup
        /// </summary>
        /// <param name="Settings"></param>
        /// <param name="Metadata"></param>
        /// <param name="Workflows"></param>
        /// <param name="SecurityRoles"></param>
        protected XrmMockupBase(XrmMockupSettings Settings, MetadataSkeleton Metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) {

            this.OrganizationId = Guid.NewGuid();
            this.OrganizationName = "MockupOrganization";
            this.TimeOffset = new TimeSpan();
            this.RequestHandler = new RequestHandler(this, Settings, Metadata, Workflows, SecurityRoles);
            this.ServiceFactory = new MockupServiceProviderAndFactory(this);
            if (Settings.EnableProxyTypes == true) {
                EnableProxyTypes();
            }


            // Create default business unit
            var rootBu = Metadata.RootBusinessUnit;
            RootBusinessUnit = Metadata.RootBusinessUnit.ToEntityReference();

            // Create admin user
            AdminuserId = Guid.NewGuid();
            var admin = new Entity(LogicalNames.SystemUser) {
                Id = AdminuserId
            };
            this.AdminUser = admin.ToEntityReference();

            admin["firstname"] = "CRM";
            admin["lastname"] = "System";
            admin["businessunitid"] = rootBu.ToEntityReference();
            RequestHandler.Initialize(admin);
            RequestHandler.SetSecurityRoles(new EntityReference(LogicalNames.SystemUser, AdminUser.Id),
                SecurityRoles
                .Where(s => s.RoleTemplateId == new Guid("627090ff-40a3-4053-8790-584edc5be201")) // System administrator role template ID
                .Select(s => s.RoleId)
                .ToArray());

        }

        /// <summary>
        /// Settings for XrmMockup instance
        /// </summary>
        public struct XrmMockupSettings {
            /// <summary>
            /// List of base-types which all your plugins extend.
            /// This is used to locate the assemblies required.
            /// </summary>
            public IEnumerable<Type> BasePluginTypes;
            /// <summary>
            /// List of at least one instance of a CodeActivity in each of your projects that contain CodeActivities. 
            /// This is used to locate the assemblies required to find all CodeActivity.
            /// </summary>
            public IEnumerable<Type> CodeActivityInstanceTypes;
            /// <summary>
            /// Enable early-bound proxy types
            /// </summary>
            public bool? EnableProxyTypes;
            /// <summary>
            /// Sets whether all workflow definitions should be included on startup. Default is true.
            /// </summary>
            public bool? IncludeAllWorkflows;
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
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                .Where(asm => !exclude.Contains(asm.ManifestModule.Name) && !regex.IsMatch(asm.ManifestModule.Name));

            var assembly = assemblies.FirstOrDefault();
            if (assembly != null) {
                RequestHandler.EnableProxyTypes(assembly);
                HasProxyTypes = true;
            }
        }

        /// <summary>
        /// Resets the local test data
        /// </summary>
        public void ResetEnvironment() {
            this.TimeOffset = new TimeSpan();
            RequestHandler.ResetEnvironment();
        }


        internal OrganizationResponse Execute(OrganizationRequest request, EntityReference userRef, PluginContext pluginContext) {
            return RequestHandler.Execute(request, userRef, pluginContext);
        }

        /// <summary>
        /// Increase the local time that mockup uses by an offset of the current time
        /// </summary>
        /// <param name="offset"></param>
        public void AddTime(TimeSpan offset) {
            this.TimeOffset = this.TimeOffset.Add(offset);
            RequestHandler.TriggerWaitingWorkflows();
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
            AddTime(new TimeSpan(days,0,0,0));
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
            RequestHandler.AddWorkflow(workflow);
        }

        /// <summary>
        /// Returns true if an entity exists in the database with the same id, and has at least the attributes of the parameter.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool ContainsEntity(Entity entity) {
            return RequestHandler.ContainsEntity(entity);
        }

        /// <summary>
        /// Adds entities directly into the database, without modifying them
        /// </summary>
        /// <param name="entities"></param>
        public void PopulateWith(params Entity[] entities) {
            RequestHandler.PopulateWith(entities);
        }

        /// <summary>
        /// Gets a system administrator organization service
        /// </summary>
        /// <returns></returns>
        public IOrganizationService GetAdminService() {
            return ServiceFactory.CreateOrganizationService(AdminUser.Id);
        }

        /// <summary>
        /// Gets a system administrator organization service, with the given settings
        /// </summary>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationService GetConfigurableAdminService(MockupServiceSettings Settings) {
            return ServiceFactory.CreateConfigurableOrganizationService(AdminUser.Id, Settings);
        }

        /// <summary>
        /// Create an organization service for the systemuser with the given id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid id) {
            return ServiceFactory.CreateOrganizationService(id);
        }

        /// <summary>
        /// Create an organization service, with the given settings, for the systemuser with the given id
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Settings"></param>
        /// <returns></returns>
        public IOrganizationService CreateOrganizationService(Guid Id, MockupServiceSettings Settings) {
            return ServiceFactory.CreateConfigurableOrganizationService(Id, Settings);
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
            user.Attributes["businessunitid"] = businessUnit;
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
            RequestHandler.SetSecurityRoles(new EntityReference(LogicalNames.SystemUser, user.Id), securityRoles);
            return service.Retrieve(LogicalNames.SystemUser, user.Id, new ColumnSet(true));
        }

        /// <summary>
        /// Create a new team with a specific businessunit
        /// </summary>
        /// <param name="service"></param>
        /// <param name="businessUnit"></param>
        /// <param name="securityRoles"></param>
        /// <returns></returns>
        public Entity CreateTeam(IOrganizationService service, EntityReference businessUnit, params Guid[] securityRoles) {
            var team = new Entity("team");
            team.Attributes["businessunitid"] = businessUnit;
            return CreateTeam(service, team, securityRoles);
        }

        /// <summary>
        /// Create a new team from an entity. Remember to provide an existing businessunit in the entity.
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
            team.Id = service.Create(team);
            RequestHandler.SetSecurityRoles(new EntityReference(LogicalNames.Team, team.Id), securityRoles);
            return service.Retrieve(LogicalNames.Team, team.Id, new ColumnSet(true));
        }

    }

}
