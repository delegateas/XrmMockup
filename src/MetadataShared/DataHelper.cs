using DG.Tools;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.ServiceModel;

namespace DG.Tools.XrmMockup.Metadata
{
    internal class DataHelper {
        internal const string PRIVILEGE = "privilege";
        internal const string ROLEPRIVILEGES = "roleprivileges";
        internal const string ROLE = "role";
        internal const string PRIVILEGE_OTC = "privilegeobjecttypecodes";
        internal const string PRIVILEGE_ROLEPRIVILEGE_ALIAS = "privrp";
        internal const string ROLEPRIVILEGE_ROLE_ALIAS = "rpr";
        internal const string PRIVILEGE_POTCH = "privpotch";
        
        private IOrganizationService service;
        private HashSet<string> EntityLogicalNames;
        private string[] SolutionNames;

        public DataHelper(IOrganizationService service, string entitiesString, string solutionsString, bool fetchFromAssemblies) {
            this.service = service;

            this.EntityLogicalNames = new HashSet<string>();

            // Add entites from assemblies
            if (fetchFromAssemblies)
                this.EntityLogicalNames = GetLogicalNames(AssemblyGetter.GetAssembliesInBuildPath());

            // Add default entities
            var defaultEntities = new string[] { "businessunit", "systemuser", "transactioncurrency", "role", "systemuserroles", "teamroles" };
            foreach (var logicalName in defaultEntities) {
                this.EntityLogicalNames.Add(logicalName);
            }

            // Add entites from solution
            this.SolutionNames = solutionsString.Split(',').Select(x => x.Trim()).ToArray();
            var solutionEntityLogicalNames = SolutionNames.SelectMany(GetEntityListFromSolution);
            foreach(var logicalName in solutionEntityLogicalNames)
            {
                this.EntityLogicalNames.Add(logicalName);
            }

            // Add specified entities
            if (!String.IsNullOrWhiteSpace(entitiesString)) {
                foreach (var logicalName in entitiesString.Split(',')) {
                    this.EntityLogicalNames.Add(logicalName.Trim());
                }
            }
        }

        public MetadataSkeleton GetMetadata(string path) {
            var skeleton = new MetadataSkeleton();
            
            Console.WriteLine($"Getting metadata for: {String.Join(", ", this.EntityLogicalNames.ToArray())}");
            skeleton.EntityMetadata = GetEntityMetadataBulk(this.EntityLogicalNames).ToDictionary(m => m.LogicalName, m => m);

            Console.WriteLine("Getting currencies");
            skeleton.Currencies = GetCurrencies();

            Console.WriteLine("Getting plugins");
            skeleton.Plugins = GetPlugins(this.SolutionNames);

            Console.WriteLine("Getting organization");
            skeleton.BaseOrganization = GetBaseOrganization();

            Console.WriteLine("Getting root businessunit");
            var rootBU = GetBusinessUnits().First(bu => !bu.Attributes.ContainsKey("parentbusinessunitid"));
            skeleton.RootBusinessUnit = rootBU.ToEntity<Entity>();

            Console.WriteLine("Getting All Optionsets");
            skeleton.OptionSets = RetrieveAllOptionSets();

            Console.WriteLine("Getting Default state and status");
            skeleton.DefaultStateStatus = GetDefaultStateAndStatus();

            return skeleton;
        }

        public OptionSetMetadataBase[] RetrieveAllOptionSets() {
            var optionSetRequest = new RetrieveAllOptionSetsRequest {
                RetrieveAsIfPublished = true
            };
            var results = service.Execute(optionSetRequest) as RetrieveAllOptionSetsResponse;
            return results.OptionSetMetadata;
        }

        private List<MetaPlugin> GetPlugins(string[] solutions) {
            if (solutions == null || solutions.Length == 0) {
                return new List<MetaPlugin>();
            }

            var pluginQuery = new QueryExpression("sdkmessageprocessingstep") {
                ColumnSet = new ColumnSet("eventhandler", "stage", "mode", "rank", "sdkmessageid", "filteringattributes", "name"),
                Criteria = new FilterExpression()
            };
            pluginQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var sdkMessageFilterQuery = new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.Inner) {
                Columns = new ColumnSet("primaryobjecttypecode"),
                EntityAlias = "sdkmessagefilter",
                LinkCriteria = new FilterExpression()
            };
            pluginQuery.LinkEntities.Add(sdkMessageFilterQuery);

            var solutionComponentQuery = new LinkEntity("sdkmessageprocessingstep", "solutioncomponent", "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner) {
                Columns = new ColumnSet(),
                LinkCriteria = new FilterExpression()
            };
            pluginQuery.LinkEntities.Add(solutionComponentQuery);

            var solutionQuery = new LinkEntity("solutioncomponent", "solution", "solutionid", "solutionid", JoinOperator.Inner) {
                Columns = new ColumnSet(),
                LinkCriteria = new FilterExpression()
            };
            solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions);
            solutionComponentQuery.LinkEntities.Add(solutionQuery);

            var plugins = new List<MetaPlugin>();

            foreach (var plugin in service.RetrieveMultiple(pluginQuery).Entities) {
                var metaPlugin = new MetaPlugin() {
                    Name = plugin.GetAttributeValue<string>("name"),
                    Rank = plugin.GetAttributeValue<int>("rank"),
                    FilteredAttributes = plugin.GetAttributeValue<string>("filteringattributes"),
                    Mode = plugin.GetAttributeValue<OptionSetValue>("mode").Value,
                    Stage = plugin.GetAttributeValue<OptionSetValue>("stage").Value,
                    MessageName = plugin.GetAttributeValue<EntityReference>("sdkmessageid").Name,
                    AssemblyName = plugin.GetAttributeValue<EntityReference>("eventhandler").Name,
                    PrimaryEntity = plugin.GetAttributeValue<AliasedValue>("sdkmessagefilter.primaryobjecttypecode").Value as string
                };
                plugins.Add(metaPlugin);
            }

            return plugins;
        }

        private List<Entity> GetCurrencies() {
            var query = new QueryExpression("transactioncurrency") {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>()).ToList();
        }

        private Entity GetBaseOrganization() {
            var baseOrganizationId = (service.Execute(new WhoAmIRequest()) as WhoAmIResponse).OrganizationId;
            var query = new QueryExpression("organization") {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>())
                .First(e => e.Id.Equals(baseOrganizationId));
        }


        internal IEnumerable<Entity> GetBusinessUnits() {
            var query = new QueryExpression("businessunit") {
                ColumnSet = new ColumnSet(true)
            };
            return service.RetrieveMultiple(query).Entities;
        }

        private IEnumerable<string> GetEntityListFromSolution(string solutionName)
        {
            var solutionFilter = new Dictionary<string, object>() { { "uniquename", solutionName } };
            var solutionColumns = new string[] { "solutionid", "uniquename" };
            var solutions = GetEntities("solution", solutionColumns, solutionFilter);
            var entitieLogicalNames = solutions.SelectMany(solution =>
                {
                    var filter = new Dictionary<string, object>() {
                            { "solutionid", solution.Id },
                            { "componenttype", 1 }
                        };
                    var columns = new string[] { "solutionid", "objectid", "componenttype", "" };
                    return GetEntities("solutioncomponent", columns, filter);
                }
            ).Select(x => GetEntityLogicalNameFromId((Guid)x["objectid"]));
            return entitieLogicalNames;
        }

        private string GetEntityLogicalNameFromId(Guid id)
        {
            var request = new RetrieveEntityRequest() {
                MetadataId = id,
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            };
            var resp = (RetrieveEntityResponse)service.Execute(request);
            return resp.EntityMetadata.LogicalName;
        }

        private IEnumerable<Entity> GetEntities(string logicalName, string[] columns, Dictionary<string, object> filter)
        {
            var f = new FilterExpression();
            foreach(var item in filter)
                f.AddCondition(item.Key, ConditionOperator.Equal, item.Value);
            var query = new QueryExpression(logicalName);
            if (columns.Count() == 0)
                query.ColumnSet = new ColumnSet(true);
            else
                query.ColumnSet = new ColumnSet(columns);
            query.Criteria = f;

            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>());
        }

        private HashSet<string> GetLogicalNames(AssemblyName[] assemblies) {
            var exclude = new string[] { "Microsoft.Xrm.Sdk.dll", "Microsoft.Crm.Sdk.Proxy.dll" };
            var correctAssemblies = assemblies
                .Select(dll => Assembly.Load(dll))
                .Where(asm => asm.CustomAttributes.Any(attr => attr.AttributeType.Name.Equals("ProxyTypesAssemblyAttribute")))
                .Where(asm => !exclude.Contains(asm.ManifestModule.Name));

            var logicalNames = new HashSet<string>();
            foreach (var asm in correctAssemblies) {
                try {
                    foreach (var type in asm.GetTypes()) {
                        var logicalNameAttribute = 
                            type.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute");
                        var logicalName = logicalNameAttribute?.ConstructorArguments?.FirstOrDefault().Value as string;

                        if (logicalName != null) {
                            logicalNames.Add(logicalName);
                        }
                    }
                } catch (Exception) {
                }
            }
            return logicalNames;
        }

        internal IEnumerable<Entity> GetWorkflows() {
            var query = new QueryExpression("workflow") {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);

            var category = new FilterExpression(LogicalOperator.Or);
            category.AddCondition("category", ConditionOperator.Equal, 0);
            category.AddCondition("category", ConditionOperator.Equal, 3);

            query.Criteria.AddFilter(category);
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 2);

            return service.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>());
        }

        internal Dictionary<Guid, SecurityRole> GetSecurityRoles(Guid rootBUId) {
            var privPotc = new LinkEntity(PRIVILEGE, PRIVILEGE_OTC, "privilegeid", "privilegeid", JoinOperator.LeftOuter) {
                Columns = new ColumnSet("objecttypecode"),
                EntityAlias = PRIVILEGE_POTCH
            };
            var rpR = new LinkEntity(ROLEPRIVILEGES, ROLE, "roleid", "parentrootroleid", JoinOperator.LeftOuter) {
                Columns = new ColumnSet("name", "roleid", "roletemplateid"),
                LinkCriteria = new FilterExpression()
            };
            rpR.LinkCriteria.AddCondition("businessunitid", ConditionOperator.Equal, rootBUId);
            rpR.EntityAlias = ROLEPRIVILEGE_ROLE_ALIAS;

            var privRp = new LinkEntity(PRIVILEGE, ROLEPRIVILEGES, "privilegeid", "privilegeid", JoinOperator.LeftOuter) {
                Columns = new ColumnSet("privilegedepthmask")
            };
            privRp.LinkEntities.Add(rpR);
            privRp.LinkEntities.Add(privPotc);
            privRp.EntityAlias = PRIVILEGE_ROLEPRIVILEGE_ALIAS;

            var query = new QueryExpression(PRIVILEGE) {
                ColumnSet = new ColumnSet("accessright", "canbeglobal", "canbedeep", "canbelocal", "canbebasic")
            };
            query.LinkEntities.Add(privRp);

            var entities = new List<Entity>();
            query.PageInfo.PageNumber = 1;

            var resp = service.RetrieveMultiple(query);
            entities.AddRange(resp.Entities);
            while (resp.MoreRecords) {
                query.PageInfo.PageNumber = query.PageInfo.PageNumber + 1;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = service.RetrieveMultiple(query);
                entities.AddRange(resp.Entities);
            }

            var roles = new Dictionary<Guid, SecurityRole>();
            foreach (var e in entities.Where(e => e.Attributes.ContainsKey(ROLEPRIVILEGE_ROLE_ALIAS + ".roleid"))) {
                var entityName = e.GetAttributeValue<AliasedValue>(PRIVILEGE_POTCH + ".objecttypecode").Value as string;
                if (entityName == "none") continue;

                var rp = ToRolePrivilege(e);
                if (rp.AccessRight == AccessRights.None) continue;
                var roleId = (Guid)e.GetAttributeValue<AliasedValue>(ROLEPRIVILEGE_ROLE_ALIAS + ".roleid").Value;
                if (!roles.ContainsKey(roleId)) {
                    roles[roleId] = new SecurityRole() {
                        Name = e.GetAttributeValue<AliasedValue>(ROLEPRIVILEGE_ROLE_ALIAS + ".name").Value as string,
                        RoleId = roleId
                    };

                    if (e.Attributes.ContainsKey(ROLEPRIVILEGE_ROLE_ALIAS + ".roletemplateid")) {
                        roles[roleId].RoleTemplateId = (e.GetAttributeValue<AliasedValue>(ROLEPRIVILEGE_ROLE_ALIAS + ".roletemplateid").Value as EntityReference).Id;
                    }

                    roles[roleId].Privileges = new Dictionary<string, Dictionary<AccessRights, RolePrivilege>>();
                }

                if (!roles[roleId].Privileges.ContainsKey(entityName)) {
                    roles[roleId].Privileges.Add(entityName, new Dictionary<AccessRights, RolePrivilege>());
                }

                roles[roleId].Privileges[entityName].Add(rp.AccessRight, rp);
            }
            return roles;
        }

        private PrivilegeDepth PrivilegeDepthToEnum(int privilegeDepth) {
            switch (privilegeDepth) {
                case 1: return PrivilegeDepth.Basic;
                case 2: return PrivilegeDepth.Local;
                case 4: return PrivilegeDepth.Deep;
                case 8: return PrivilegeDepth.Global;
                default:
                    throw new NotImplementedException($"Unknown privilege depth mask: {privilegeDepth}");
            }
        }

        private RolePrivilege ToRolePrivilege(Entity e) {
            var rp = new RolePrivilege() {
                CanBeGlobal = e.GetAttributeValue<bool>("canbeglobal"),
                CanBeDeep = e.GetAttributeValue<bool>("canbedeep"),
                CanBeLocal = e.GetAttributeValue<bool>("canbelocal"),
                CanBeBasic = e.GetAttributeValue<bool>("canbebasic"),
                AccessRight = e.GetAttributeValue<AccessRights>("accessright"),
                PrivilegeDepth = PrivilegeDepthToEnum((int)e.GetAttributeValue<AliasedValue>(PRIVILEGE_ROLEPRIVILEGE_ALIAS + ".privilegedepthmask").Value)
            };
            return rp;

        }
        private RetrieveEntityRequest GetEntityMetadataRequest(string logicalName) {
            var request = new RetrieveEntityRequest() {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.All,
                RetrieveAsIfPublished = true
            };
            return request;
        }

        private EntityMetadata[] GetEntityMetadataBulk(HashSet<string> logicalNames)
        {
            var request = new ExecuteMultipleRequest {
                Requests = new OrganizationRequestCollection()
            };
            request.Requests.AddRange(logicalNames.Select(lname => GetEntityMetadataRequest(lname)));
            request.Settings = new ExecuteMultipleSettings() {
                ContinueOnError = false,
                ReturnResponses = true
            };

            var bulkResp = (service.Execute(request) as ExecuteMultipleResponse).Responses;
            if (bulkResp.Any(resp => resp.Fault != null)) {
                var resp = bulkResp.First(r => r.Fault != null);
                var faultedReq = (RetrieveEntityRequest)request.Requests[resp.RequestIndex];
                throw new FaultException($"Error while retrieving entity metadata for entity {faultedReq.LogicalName}: {resp.Fault.Message}");
            }

            return bulkResp
                .Select(resp => (resp.Response as RetrieveEntityResponse).EntityMetadata)
                .ToArray();
        }

        private Dictionary<string, Dictionary<int,int>> GetDefaultStateAndStatus() {
            var dict = new Dictionary<string, Dictionary<int,int>>();

            var query = new QueryExpression("statusmap") {
                ColumnSet = new ColumnSet(true)
            };
            query.PageInfo.PageNumber = 1;

            var statusmaps = new List<Entity>();
            var resp = service.RetrieveMultiple(query);
            statusmaps.AddRange(resp.Entities);
            while (resp.MoreRecords) {
                query.PageInfo.PageNumber = query.PageInfo.PageNumber + 1;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = service.RetrieveMultiple(query);
                statusmaps.AddRange(resp.Entities);
            }

            foreach (var e in statusmaps.Where(x => x.GetAttributeValue<bool>("isdefault"))) {
                var logicalName = e.GetAttributeValue<string>("objecttypecode");
                if (!dict.ContainsKey(logicalName)) {
                    dict.Add(logicalName, new Dictionary<int, int>());
                }
                dict[logicalName].Add(e.GetAttributeValue<int>("state"),e.GetAttributeValue<int>("status"));
            }

            return dict;
        }

    }
}
