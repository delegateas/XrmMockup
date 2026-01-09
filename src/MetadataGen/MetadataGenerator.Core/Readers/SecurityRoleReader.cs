using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads security role definitions from Dataverse.
/// </summary>
internal sealed class SecurityRoleReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<SecurityRoleReader> logger) : ISecurityRoleReader
{
    private const string Privilege = "privilege";
    private const string RolePrivileges = "roleprivileges";
    private const string Role = "role";
    private const string PrivilegeOtc = "privilegeobjecttypecodes";

    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        string[] solutions,
        string[] additionalSecurityRoles,
        CancellationToken ct = default)
    {
        logger.LogInformation("Getting security roles");

        return await Task.Run(() =>
        {
            // Get role IDs to filter by (if solutions specified)
            var roleIdsToInclude = GetRoleIdsToInclude(solutions, additionalSecurityRoles, rootBusinessUnitId);

            // Query all required data
            var privileges = QueryPaging(new QueryExpression(Privilege)
            {
                ColumnSet = new ColumnSet("privilegeid", "accessright", "canbeglobal", "canbedeep", "canbelocal", "canbebasic")
            });

            var privilegeOTCs = QueryPaging(new QueryExpression(PrivilegeOtc)
            {
                ColumnSet = new ColumnSet("privilegeid", "objecttypecode")
            });

            var rolePrivileges = QueryPaging(new QueryExpression(RolePrivileges)
            {
                ColumnSet = new ColumnSet("privilegeid", "roleid", "privilegedepthmask")
            });

            var roleList = QueryPaging(GetRoleQuery(roleIdsToInclude));

            // Join: roleprivileges inner join roles
            // Use GetAttributeValue for defensive access in case parentrootroleid is missing
            var rolePrivilegeJoinRole =
                from roleprivilege in rolePrivileges
                join role in roleList on (Guid)roleprivilege["roleid"] equals GetParentRootRoleId(role)
                where ((EntityReference)role["businessunitid"]).Id.Equals(rootBusinessUnitId) &&
                      (int)roleprivilege["privilegedepthmask"] != 0
                select new { roleprivilege, role };

            // Join: privileges left outer join privilegeOTCs
            var privilegesJoinPrivilegeOTCs =
                from privilege in privileges
                join privilegeOTC in privilegeOTCs on (Guid)privilege["privilegeid"] equals ((EntityReference)privilegeOTC["privilegeid"]).Id into res
                from privilegeOTC in res.DefaultIfEmpty()
                select new { privilege, privilegeOTC };

            // Full join: pp left outer join rpr
            var entities =
                from pp in privilegesJoinPrivilegeOTCs
                join rpr in rolePrivilegeJoinRole on (Guid)pp.privilege["privilegeid"] equals (Guid)rpr.roleprivilege["privilegeid"] into res
                from rpr in res.DefaultIfEmpty()
                select new { pp, rpr };

            // Build security roles
            var roles = new Dictionary<Guid, SecurityRole>();
            var roleNameCounters = new Dictionary<string, int>();

            var validSecurityRoles = entities.Where(e =>
                e.pp?.privilege != null &&
                (e.pp?.privilegeOTC?.Contains("objecttypecode")).GetValueOrDefault() &&
                (e.rpr?.roleprivilege?.Contains("roleid")).GetValueOrDefault() &&
                (e.rpr?.role?.Contains("name")).GetValueOrDefault());

            foreach (var e in validSecurityRoles)
            {
                var entityName = (string)e.pp.privilegeOTC!["objecttypecode"];

                if (entityName == "none") continue;

                var rp = ToRolePrivilege(e.pp.privilege!, e.rpr!.roleprivilege);
                if (rp.AccessRight == AccessRights.None) continue;

                var roleId = (Guid)e.rpr.roleprivilege["roleid"];
                if (!roles.TryGetValue(roleId, out var roleValue))
                {
                    var name = (string)e.rpr.role["name"];

                    // Always mitigate duplicate names
                    if (roleNameCounters.TryGetValue(name, out var count))
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("Duplicate security role name detected: \"{Name}\". If this is not on purpose, contact Microsoft Support.", name);
                        }

                        name += $"_{++count}";
                        roleNameCounters[name] = count;
                    }
                    else
                    {
                        roleNameCounters[name] = 1;
                    }

                    roleValue = new SecurityRole
                    {
                        Name = name,
                        RoleId = roleId,
                        Privileges = []
                    };
                    roles[roleId] = roleValue;

                    if (e.rpr.role.Attributes.ContainsKey("roletemplateid"))
                    {
                        roles[roleId].RoleTemplateId = ((EntityReference)e.rpr.role["roletemplateid"]).Id;
                    }
                }

                if (!roleValue.Privileges.TryGetValue(entityName, out var value))
                {
                    value = [];
                    roleValue.Privileges.Add(entityName, value);
                }

                value.TryAdd(rp.AccessRight, rp);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} security roles", roles.Count);
            }
            return roles;
        }, ct);
    }

    private List<Entity> QueryPaging(QueryExpression query)
    {
        query.PageInfo.PageNumber = 1;
        var entities = new List<Entity>();
        var resp = _service.RetrieveMultiple(query);
        entities.AddRange(resp.Entities);

        while (resp.MoreRecords)
        {
            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = resp.PagingCookie;
            resp = _service.RetrieveMultiple(query);
            entities.AddRange(resp.Entities);
        }

        return entities;
    }

    private static DG.Tools.XrmMockup.RolePrivilege ToRolePrivilege(Entity privilege, Entity rolePrivilege)
    {
        return new DG.Tools.XrmMockup.RolePrivilege
        {
            CanBeGlobal = privilege.GetAttributeValue<bool>("canbeglobal"),
            CanBeDeep = privilege.GetAttributeValue<bool>("canbedeep"),
            CanBeLocal = privilege.GetAttributeValue<bool>("canbelocal"),
            CanBeBasic = privilege.GetAttributeValue<bool>("canbebasic"),
            AccessRight = privilege.GetAttributeValue<AccessRights>("accessright"),
            PrivilegeDepth = PrivilegeDepthToEnum((int)rolePrivilege["privilegedepthmask"])
        };
    }

    private static PrivilegeDepth PrivilegeDepthToEnum(int privilegeDepth)
    {
        return privilegeDepth switch
        {
            1 => PrivilegeDepth.Basic,
            2 => PrivilegeDepth.Local,
            4 => PrivilegeDepth.Deep,
            8 => PrivilegeDepth.Global,
            _ => throw new NotImplementedException($"Unknown privilege depth mask: {privilegeDepth}")
        };
    }

    /// <summary>
    /// Gets the parent root role ID from a role entity.
    /// Falls back to the role's own ID if parentrootroleid is not present.
    /// </summary>
    private static Guid GetParentRootRoleId(Entity role)
    {
        var parentRootRole = role.GetAttributeValue<EntityReference>("parentrootroleid");
        if (parentRootRole != null)
            return parentRootRole.Id;

        // Fallback: use the role's own ID (for root roles or when parentrootroleid is not populated)
        return role.Id;
    }

    /// <summary>
    /// Gets the set of role IDs to include based on solutions and additional role names.
    /// Returns null if no filtering is needed (no solutions specified).
    /// </summary>
    private HashSet<Guid>? GetRoleIdsToInclude(string[] solutions, string[] additionalSecurityRoles, Guid rootBusinessUnitId)
    {
        if (solutions.Length == 0)
            return null;

        var roleIds = new HashSet<Guid>();

        // Get role IDs from solutions
        var solutionRoleIds = GetRoleIdsFromSolutions(solutions);
        foreach (var id in solutionRoleIds)
        {
            roleIds.Add(id);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Found {Count} roles in specified solutions", solutionRoleIds.Count);
        }

        // Get role IDs by name for additional roles
        if (additionalSecurityRoles.Length > 0)
        {
            var additionalRoleIds = GetRoleIdsByName(additionalSecurityRoles, rootBusinessUnitId);
            foreach (var id in additionalRoleIds)
            {
                roleIds.Add(id);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Found {Count} additional roles by name", additionalRoleIds.Count);
            }
        }

        return roleIds;
    }

    /// <summary>
    /// Gets role IDs that are part of the specified solutions.
    /// </summary>
    private HashSet<Guid> GetRoleIdsFromSolutions(string[] solutions)
    {
        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("roleid"),
            Distinct = true
        };

        var solutionComponentQuery = new LinkEntity(
            Role, "solutioncomponent",
            "roleid", "objectid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        solutionComponentQuery.LinkCriteria.AddCondition("componenttype", ConditionOperator.Equal, 20);
        query.LinkEntities.Add(solutionComponentQuery);

        var solutionQuery = new LinkEntity(
            "solutioncomponent", "solution",
            "solutionid", "solutionid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions.Cast<object>().ToArray());
        solutionComponentQuery.LinkEntities.Add(solutionQuery);

        var results = _service.RetrieveMultiple(query);
        return results.Entities.Select(e => e.Id).ToHashSet();
    }

    /// <summary>
    /// Gets role IDs by their names in the root business unit.
    /// </summary>
    private HashSet<Guid> GetRoleIdsByName(string[] roleNames, Guid rootBusinessUnitId)
    {
        if (roleNames.Length == 0)
            return [];

        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("roleid")
        };
        query.Criteria.AddCondition("name", ConditionOperator.In, roleNames.Cast<object>().ToArray());
        query.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, rootBusinessUnitId);

        var results = _service.RetrieveMultiple(query);
        return results.Entities.Select(e => e.Id).ToHashSet();
    }

    /// <summary>
    /// Builds the role query, optionally filtering by role IDs.
    /// </summary>
    private static QueryExpression GetRoleQuery(HashSet<Guid>? roleIdsToInclude)
    {
        var query = new QueryExpression(Role)
        {
            ColumnSet = new ColumnSet("parentrootroleid", "name", "roleid", "roletemplateid", "businessunitid")
        };

        if (roleIdsToInclude is not null && roleIdsToInclude.Count > 0)
        {
            query.Criteria.AddCondition("roleid", ConditionOperator.In, roleIdsToInclude.Cast<object>().ToArray());
        }

        return query;
    }
}
