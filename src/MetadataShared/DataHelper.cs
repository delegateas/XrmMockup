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
        private IOrganizationService proxy;
        internal string priv = "privilege";
        internal string rp = "roleprivileges";
        internal string r = "role";
        internal string potc = "privilegeobjecttypecodes";
        internal string privRpAlias = "privrp";
        internal string rpRAlias = "rpr";
        internal string privPotcAlias = "privpotch";


        public DataHelper() {
            proxy = AuthHelper.Authenticate();
        }

        public MetadataSkeleton GetMetadata(string path) {
            var skeleton = new MetadataSkeleton();
            var logicalNames = GetLogicalNames(AssemblyGetter.GetAssembliesInBuildPath());
            var defaultEntities = new string[] { "businessunit", "systemuser", "transactioncurrency", "role", "systemuserroles", "teamroles" };
            var configEntities = ConfigurationManager.AppSettings["entities"];
            var solutions = ConfigurationManager.AppSettings["solutions"];

            if (configEntities != null && configEntities != "") {
                foreach (var logicalName in configEntities.Split(',')) {
                    logicalNames.Add(logicalName);
                }
            }

            foreach (var logicalName in defaultEntities) {
                logicalNames.Add(logicalName);
            }

            Console.WriteLine($"Getting metadata for: {String.Join(", ", logicalNames.ToArray())}");
            skeleton.Metadata = GetEntityMetadataBulk(logicalNames).ToDictionary(m => m.LogicalName, m => m);

            Console.WriteLine("Getting currencies");
            skeleton.Currencies = GetCurrencies();

            Console.WriteLine("Getting plugins");
            skeleton.Plugins = solutions == "" || solutions == null ? new List<MetaPlugin>() : GetPlugins(solutions.Split(','));

            Console.WriteLine("Getting organization");
            skeleton.BaseOrganization = GetBaseOrganization();

            Console.WriteLine("Getting root businessunit");
            var rootBU = GetBusinessUnits().First(bu => !bu.Attributes.ContainsKey("parentbusinessunitid"));
            skeleton.RootBusinessUnit = rootBU.ToEntity<Entity>();

            return skeleton;
        }

        private List<MetaPlugin> GetPlugins(string[] solutions) {
            var pluginQuery = new QueryExpression("sdkmessageprocessingstep");
            pluginQuery.ColumnSet = new ColumnSet("eventhandler", "stage", "mode", "rank", "sdkmessageid", "filteringattributes", "name");
            pluginQuery.Criteria = new FilterExpression();
            pluginQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

            var sdkMessageFilterQuery = new LinkEntity("sdkmessageprocessingstep", "sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.Inner);
            sdkMessageFilterQuery.Columns = new ColumnSet("primaryobjecttypecode");
            sdkMessageFilterQuery.EntityAlias = "sdkmessagefilter";
            sdkMessageFilterQuery.LinkCriteria = new FilterExpression();
            pluginQuery.LinkEntities.Add(sdkMessageFilterQuery);

            var solutionComponentQuery = new LinkEntity("sdkmessageprocessingstep", "solutioncomponent", "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner);
            solutionComponentQuery.Columns = new ColumnSet();
            solutionComponentQuery.LinkCriteria = new FilterExpression();
            pluginQuery.LinkEntities.Add(solutionComponentQuery);

            var solutionQuery = new LinkEntity("solutioncomponent", "solution", "solutionid", "solutionid", JoinOperator.Inner);
            solutionQuery.Columns = new ColumnSet();
            solutionQuery.LinkCriteria = new FilterExpression();
            solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions);
            solutionComponentQuery.LinkEntities.Add(solutionQuery);

            var plugins = new List<MetaPlugin>();

            foreach (var plugin in proxy.RetrieveMultiple(pluginQuery).Entities) {
                var metaPlugin = new MetaPlugin();
                metaPlugin.Name = plugin.GetAttributeValue<string>("name");
                metaPlugin.Rank = plugin.GetAttributeValue<int>("rank");
                metaPlugin.FilteredAttributes = plugin.GetAttributeValue<string>("filteringattributes");
                metaPlugin.Mode = plugin.GetAttributeValue<OptionSetValue>("mode").Value;
                metaPlugin.Stage = plugin.GetAttributeValue<OptionSetValue>("stage").Value;
                metaPlugin.MessageName = plugin.GetAttributeValue<EntityReference>("sdkmessageid").Name;
                metaPlugin.AssemblyName = plugin.GetAttributeValue<EntityReference>("eventhandler").Name;
                metaPlugin.PrimaryEntity = plugin.GetAttributeValue<AliasedValue>("sdkmessagefilter.primaryobjecttypecode").Value as string;
                plugins.Add(metaPlugin);
            }

            return plugins;
        }

        private List<Entity> GetCurrencies() {
            var query = new QueryExpression("transactioncurrency");
            query.ColumnSet = new ColumnSet(true);

            return proxy.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>()).ToList();
        }

        private Entity GetBaseOrganization() {
            var baseOrganizationId = (proxy.Execute(new WhoAmIRequest()) as WhoAmIResponse).OrganizationId;
            var query = new QueryExpression("organization");
            query.ColumnSet = new ColumnSet(true);

            return proxy.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>())
                .First(e => e.Id.Equals(baseOrganizationId));
        }


        internal IEnumerable<Entity> GetBusinessUnits() {
            var query = new QueryExpression("businessunit");
            query.ColumnSet = new ColumnSet(true);

            return proxy.RetrieveMultiple(query).Entities;
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
                        var logicalName =
                            type.CustomAttributes
                            .FirstOrDefault(a => a.AttributeType.Name == "EntityLogicalNameAttribute")
                            ?.ConstructorArguments
                            ?.FirstOrDefault()
                            .Value as string;

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
            var query = new QueryExpression("workflow");
            query.ColumnSet = new ColumnSet(true);
            query.Criteria = new FilterExpression();
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);

            var category = new FilterExpression(LogicalOperator.Or);
            category.AddCondition("category", ConditionOperator.Equal, 0);
            category.AddCondition("category", ConditionOperator.Equal, 3);

            query.Criteria.AddFilter(category);
            query.Criteria.AddCondition("type", ConditionOperator.Equal, 2);

            return proxy.RetrieveMultiple(query).Entities
                .Select(e => e.ToEntity<Entity>());
        }

        internal Dictionary<Guid, SecurityRole> GetSecurityRoles(Guid rootBUId) {


            var privPotc = new LinkEntity(priv, potc, "privilegeid", "privilegeid", JoinOperator.LeftOuter);
            privPotc.Columns = new ColumnSet("objecttypecode");
            privPotc.EntityAlias = privPotcAlias;

            var rpR = new LinkEntity(rp, r, "roleid", "parentrootroleid", JoinOperator.LeftOuter);
            rpR.Columns = new ColumnSet("name", "roleid", "roletemplateid");
            rpR.LinkCriteria = new FilterExpression();
            rpR.LinkCriteria.AddCondition("businessunitid", ConditionOperator.Equal, rootBUId);
            rpR.EntityAlias = rpRAlias;

            var privRp = new LinkEntity(priv, rp, "privilegeid", "privilegeid", JoinOperator.LeftOuter);
            privRp.Columns = new ColumnSet("privilegedepthmask");
            privRp.LinkEntities.Add(rpR);
            privRp.LinkEntities.Add(privPotc);
            privRp.EntityAlias = privRpAlias;

            var query = new QueryExpression(priv);
            query.ColumnSet = new ColumnSet("accessright", "canbeglobal", "canbedeep", "canbelocal", "canbebasic");
            query.LinkEntities.Add(privRp);

            var entities = new List<Entity>();
            query.PageInfo.PageNumber = 1;
            var resp = proxy.RetrieveMultiple(query);
            entities.AddRange(resp.Entities);
            while (resp.MoreRecords) {
                query.PageInfo.PageNumber = query.PageInfo.PageNumber + 1;
                query.PageInfo.PagingCookie = resp.PagingCookie;
                resp = proxy.RetrieveMultiple(query);
                entities.AddRange(resp.Entities);
            }

            var roles = new Dictionary<Guid, SecurityRole>();
            foreach (var e in entities.Where(e => e.Attributes.ContainsKey(rpRAlias + ".roleid"))) {
                var entityName = (e.Attributes[privPotcAlias + ".objecttypecode"] as AliasedValue).Value as string;
                if (entityName == "none") continue;

                var rp = toRolePrivilege(e);
                if (rp.AccessRight == AccessRights.None) continue;
                var roleId = (Guid)(e.Attributes[rpRAlias + ".roleid"] as AliasedValue).Value;
                if (!roles.ContainsKey(roleId)) {
                    roles[roleId] = new SecurityRole();
                    roles[roleId].Name = (e.Attributes[rpRAlias + ".name"] as AliasedValue).Value as string;
                    roles[roleId].RoleId = roleId;
                    if (e.Attributes.ContainsKey(rpRAlias + ".roletemplateid")) {
                        roles[roleId].RoleTemplateId = ((e.Attributes[rpRAlias + ".roletemplateid"] as AliasedValue).Value as EntityReference).Id;
                    }
                    roles[roleId].Privileges = new Dictionary<string, Dictionary<AccessRights, DG.Tools.RolePrivilege>>();
                }

                if (!roles[roleId].Privileges.ContainsKey(entityName)) {
                    roles[roleId].Privileges.Add(entityName, new Dictionary<AccessRights, DG.Tools.RolePrivilege>());
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

        private DG.Tools.RolePrivilege toRolePrivilege(Entity e) {
            var rp = new DG.Tools.RolePrivilege();
            rp.CanBeGlobal = (bool)e.Attributes["canbeglobal"];
            rp.CanBeDeep = (bool)e.Attributes["canbedeep"];
            rp.CanBeLocal = (bool)e.Attributes["canbelocal"];
            rp.CanBeBasic = (bool)e.Attributes["canbebasic"];
            rp.AccessRight = (AccessRights)e.Attributes["accessright"];
            rp.PrivilegeDepth =
                PrivilegeDepthToEnum((int)(e.Attributes[privRpAlias + ".privilegedepthmask"] as AliasedValue).Value);
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

        private EntityMetadata[] GetEntityMetadataBulk(HashSet<string> logicalNames) {
            var request = new ExecuteMultipleRequest();
            request.Requests = new OrganizationRequestCollection();
            request.Requests.AddRange(logicalNames.Select(lname => GetEntityMetadataRequest(lname)));
            request.Settings = new ExecuteMultipleSettings() {
                ContinueOnError = false,
                ReturnResponses = true
            };

            var bulkResp = (proxy.Execute(request) as ExecuteMultipleResponse).Responses;
            if (bulkResp.Any(resp => resp.Fault != null)) {
                var resp = bulkResp.First(r => r.Fault != null);
                throw new FaultException($"Error while retrieving entity metadata: {resp.Fault.Message}");
            }

            return bulkResp
                .Select(resp => (resp.Response as RetrieveEntityResponse).EntityMetadata)
                .ToArray();
        }

    }
}
