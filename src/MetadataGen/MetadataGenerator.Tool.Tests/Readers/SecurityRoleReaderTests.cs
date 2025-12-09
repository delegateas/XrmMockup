extern alias XrmMockupLib;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for SecurityRoleReader.
/// XrmMockup loads security roles from TestMetadata/SecurityRoles (99 role files).
/// Tests verify the reader correctly retrieves roles with their privileges.
/// </summary>
public class SecurityRoleReaderTests : ReaderTestBase
{
    private readonly SecurityRoleReader _reader;
    private readonly ILogger<SecurityRoleReader> _logger;

    /// <summary>
    /// Known security role GUIDs from TestMetadata/TypeDeclarations.cs.
    /// </summary>
    private static readonly Guid SystemAdministratorRoleId = new("edfa07f1-a1ba-f011-bbd3-7c1e52365f30");
    private static readonly Guid SystemCustomizerRoleId = new("78ff07f1-a1ba-f011-bbd3-7c1e52365f30");
    private static readonly Guid BasicUserRoleId = new("76ff07f1-a1ba-f011-bbd3-7c1e52365f30");

    public SecurityRoleReaderTests()
    {
        _logger = CreateLogger<SecurityRoleReader>();
        _reader = new SecurityRoleReader(ServiceProvider, _logger);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ReturnsSecurityRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSecurityRolesAsync_ReturnsDictionaryOfGuidToSecurityRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        var resultType = result.GetType();
        Assert.True(resultType.IsGenericType);
        Assert.Equal("Dictionary`2", resultType.Name);
        var typeArgs = resultType.GetGenericArguments();
        Assert.Equal(typeof(Guid), typeArgs[0]);
        Assert.Contains("SecurityRole", typeArgs[1].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsSystemAdministratorRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        Assert.True(result.ContainsKey(SystemAdministratorRoleId));
        Assert.Equal("System Administrator", result[SystemAdministratorRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsSystemCustomizerRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        Assert.True(result.ContainsKey(SystemCustomizerRoleId));
        Assert.Equal("System Customizer", result[SystemCustomizerRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsBasicUserRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        Assert.True(result.ContainsKey(BasicUserRoleId));
        Assert.Equal("Basic User", result[BasicUserRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_SystemAdministratorHasPrivileges()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        var sysAdmin = result[SystemAdministratorRoleId];
        Assert.NotEmpty(sysAdmin.Privileges);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_AllRolesHaveNames()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        foreach (var role in result.Values)
        {
            Assert.False(string.IsNullOrEmpty(role.Name), $"Role {role.RoleId} is missing a name");
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_DictionaryKeyMatchesRoleId()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        foreach (var kvp in result)
        {
            Assert.Equal(kvp.Key, kvp.Value.RoleId);
            Assert.NotEqual(Guid.Empty, kvp.Value.RoleId);
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_RolesWithPrivilegesHaveValidDepth()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        var rolesWithPrivileges = result.Values.Where(r => r.Privileges.Count > 0).ToList();
        Assert.NotEmpty(rolesWithPrivileges);

        foreach (var role in rolesWithPrivileges)
        {
            foreach (var entityPrivileges in role.Privileges)
            {
                foreach (var privilege in entityPrivileges.Value)
                {
                    Assert.True(
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Basic ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Local ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Deep ||
                        privilege.Value.PrivilegeDepth == PrivilegeDepth.Global,
                        $"Role '{role.Name}' has invalid privilege depth: {privilege.Value.PrivilegeDepth}");
                }
            }
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_PrivilegesHaveValidAccessRights()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        var roleWithPrivileges = result.Values.First(r => r.Privileges.Count > 0);
        foreach (var entityPrivileges in roleWithPrivileges.Privileges)
        {
            foreach (var privilege in entityPrivileges.Value)
            {
                Assert.Equal(privilege.Key, privilege.Value.AccessRight);
                Assert.NotEqual(AccessRights.None, privilege.Value.AccessRight);
            }
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ConsistentResultsOnMultipleCalls()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result1 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);
        var result2 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId);

        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1[SystemAdministratorRoleId].Name, result2[SystemAdministratorRoleId].Name);
    }
}
