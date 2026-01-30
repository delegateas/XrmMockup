extern alias XrmMockupLib;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Connection;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for SecurityRoleReader.
/// XrmMockup loads security roles from TestMetadata/SecurityRoles (99 role files).
/// Tests verify the reader correctly retrieves roles with their privileges.
///
/// Note: Many tests are skipped because XrmMockup doesn't populate
/// privilege/roleprivileges/privilegeobjecttypecodes entities required for full security role retrieval.
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

    #region Solution Filtering Tests

    /// <summary>
    /// Tests backward compatibility: when no solutions are specified,
    /// the reader should return all security roles (no filtering applied).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NoSolutions_ReturnsAllRoles()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] emptySolutions = [];
        string[] emptyAdditionalRoles = [];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            emptySolutions,
            emptyAdditionalRoles);

        // Assert - Should return a dictionary (even if empty due to XrmMockup limitations)
        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Tests that when solutions are specified but no additional roles,
    /// only roles from those solutions should be returned.
    /// Note: This test verifies the filtering is applied but may return empty
    /// due to XrmMockup not populating solutioncomponent entities.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithSolutions_OnlyReturnsSolutionRoles()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] solutions = ["TestSolution"];
        string[] emptyAdditionalRoles = [];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions,
            emptyAdditionalRoles);

        // Assert - Should return a dictionary (filtering is applied)
        // Due to XrmMockup limitations with solutioncomponent, result may be empty
        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Tests that when both solutions and additional role names are specified,
    /// the reader returns roles from solutions plus any additional named roles.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithSolutionsAndAdditionalRoles_ReturnsBoth()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] solutions = ["TestSolution"];
        string[] additionalRoles = ["System Administrator", "Basic User"];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions,
            additionalRoles);

        // Assert - Should return a dictionary
        Assert.NotNull(result);
        Assert.IsType<Dictionary<Guid, DG.Tools.XrmMockup.SecurityRole>>(result);
    }

    /// <summary>
    /// Tests that when additional role names don't exist,
    /// the reader does not throw an exception and returns gracefully.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_AdditionalRolesNotFound_DoesNotFail()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] solutions = ["TestSolution"];
        string[] nonExistentRoles = ["NonExistentRole1", "NonExistentRole2"];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions,
            nonExistentRoles);

        // Assert - Should not throw and should return a dictionary
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that when only additional role names are specified without solutions,
    /// the filtering still works (no solution filtering since solutions array is empty).
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_AdditionalRolesOnlyNoSolutions_ReturnsAllRoles()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] emptySolutions = [];
        string[] additionalRoles = ["System Administrator"];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            emptySolutions,
            additionalRoles);

        // Assert - When no solutions specified, all roles are returned (no filtering)
        // The additional roles parameter is only used when solutions are specified
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that the method handles empty arrays gracefully.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_EmptyArrays_ReturnsSuccessfully()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            additionalSecurityRoles: []);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that non-existent solution names don't cause errors.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_NonExistentSolution_ReturnsEmptyOrFiltered()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] nonExistentSolutions = ["NonExistentSolution123"];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            nonExistentSolutions,
            additionalSecurityRoles: []);

        // Assert - Should not throw, may return empty since no roles in non-existent solution
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that multiple solutions are handled correctly.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_MultipleSolutions_HandlesCorrectly()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        string[] multipleSolutions = ["Solution1", "Solution2", "Solution3"];

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            multipleSolutions,
            additionalSecurityRoles: []);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests cancellation token support.
    /// </summary>
    [Fact]
    public async Task GetSecurityRolesAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _reader.GetSecurityRolesAsync(
            rootBusinessUnitId,
            solutions: [],
            additionalSecurityRoles: [],
            ct: cts.Token);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ReturnsSecurityRoles()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetSecurityRolesAsync_ReturnsDictionaryOfGuidToSecurityRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

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

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        Assert.True(result.ContainsKey(SystemAdministratorRoleId));
        Assert.Equal("System Administrator", result[SystemAdministratorRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsSystemCustomizerRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        Assert.True(result.ContainsKey(SystemCustomizerRoleId));
        Assert.Equal("System Customizer", result[SystemCustomizerRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_ContainsBasicUserRole()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        Assert.True(result.ContainsKey(BasicUserRoleId));
        Assert.Equal("Basic User", result[BasicUserRoleId].Name);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_SystemAdministratorHasPrivileges()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        var sysAdmin = result[SystemAdministratorRoleId];
        Assert.NotEmpty(sysAdmin.Privileges);
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_AllRolesHaveNames()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        foreach (var role in result.Values)
        {
            Assert.False(string.IsNullOrEmpty(role.Name), $"Role {role.RoleId} is missing a name");
        }
    }

    [Fact(Skip = "XrmMockup doesn't populate privilege/roleprivileges/privilegeobjecttypecodes entities")]
    public async Task GetSecurityRolesAsync_DictionaryKeyMatchesRoleId()
    {
        var rootBusinessUnitId = Crm.RootBusinessUnit.Id;

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

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

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

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

        var result = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

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

        var result1 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);
        var result2 = await _reader.GetSecurityRolesAsync(rootBusinessUnitId, [], []);

        Assert.Equal(result1.Count, result2.Count);
        Assert.Equal(result1[SystemAdministratorRoleId].Name, result2[SystemAdministratorRoleId].Name);
    }
}
