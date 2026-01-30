using System.Runtime.Serialization;
using DG.Tools.XrmMockup;
using Microsoft.Crm.Sdk.Messages;
using RolePrivilege = DG.Tools.XrmMockup.RolePrivilege;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Models;
using XrmMockup.MetadataGenerator.Core.Services;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Services;

/// <summary>
/// Tests for DataContractMetadataSerializer.
/// Tests verify the serializer correctly creates metadata files with expected content.
/// </summary>
public class DataContractMetadataSerializerTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly DataContractMetadataSerializer _serializer;

    public DataContractMetadataSerializerTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"MetadataSerializerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testOutputDir);

        var options = Options.Create(new GeneratorOptions { OutputDirectory = _testOutputDir });
        var logger = Substitute.For<ILogger<DataContractMetadataSerializer>>();
        _serializer = new DataContractMetadataSerializer(options, logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, recursive: true);
        }
    }

    [Fact]
    public async Task SerializeMetadataAsync_CreatesMetadataXmlFile()
    {
        var skeleton = new MetadataSkeleton
        {
            EntityMetadata = [],
            Currencies = [],
            Plugins = [],
            OptionSets = [],
            DefaultStateStatus = []
        };

        await _serializer.SerializeMetadataAsync(skeleton);

        var metadataPath = Path.Combine(_testOutputDir, "Metadata.xml");
        Assert.True(File.Exists(metadataPath));
    }

    [Fact]
    public async Task SerializeMetadataAsync_CreatesDeserializableXml()
    {
        var skeleton = new MetadataSkeleton
        {
            EntityMetadata = [],
            Currencies = [],
            Plugins = [],
            OptionSets = [],
            DefaultStateStatus = []
        };

        await _serializer.SerializeMetadataAsync(skeleton);

        var metadataPath = Path.Combine(_testOutputDir, "Metadata.xml");
        await using var stream = new FileStream(metadataPath, FileMode.Open);
        var deserializer = new DataContractSerializer(typeof(MetadataSkeleton));
        var deserialized = (MetadataSkeleton?)deserializer.ReadObject(stream);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public async Task SerializeWorkflowsAsync_CreatesWorkflowsDirectory()
    {
        var workflow = new Entity("workflow", Guid.NewGuid());
        workflow["name"] = "Test Workflow";

        await _serializer.SerializeWorkflowsAsync([workflow]);

        var workflowsDir = Path.Combine(_testOutputDir, "Workflows");
        Assert.True(Directory.Exists(workflowsDir));
    }

    [Fact]
    public async Task SerializeWorkflowsAsync_CreatesOneFilePerWorkflow()
    {
        var workflow = new Entity("workflow", Guid.NewGuid());
        workflow["name"] = "Test Workflow";

        await _serializer.SerializeWorkflowsAsync([workflow]);

        var workflowsDir = Path.Combine(_testOutputDir, "Workflows");
        var files = Directory.GetFiles(workflowsDir, "*.xml");
        Assert.Single(files);
    }

    [Fact]
    public async Task SerializeWorkflowsAsync_UsesWorkflowNameForFileName()
    {
        var workflow = new Entity("workflow", Guid.NewGuid());
        workflow["name"] = "Test Workflow";

        await _serializer.SerializeWorkflowsAsync([workflow]);

        var workflowsDir = Path.Combine(_testOutputDir, "Workflows");
        var files = Directory.GetFiles(workflowsDir, "*.xml");
        Assert.Contains("TestWorkflow.xml", files[0]);
    }

    [Fact]
    public async Task SerializeWorkflowsAsync_RemovesSpecialCharactersFromFileName()
    {
        var workflow = new Entity("workflow", Guid.NewGuid());
        workflow["name"] = "Test Workflow (Special/Chars)";

        await _serializer.SerializeWorkflowsAsync([workflow]);

        var workflowsDir = Path.Combine(_testOutputDir, "Workflows");
        var files = Directory.GetFiles(workflowsDir, "*.xml");
        Assert.Single(files);
        Assert.Contains("TestWorkflowSpecialChars.xml", files[0]);
    }

    [Fact]
    public async Task SerializeWorkflowsAsync_CreatesMultipleFilesForMultipleWorkflows()
    {
        var workflow1 = new Entity("workflow", Guid.NewGuid());
        workflow1["name"] = "First Workflow";
        var workflow2 = new Entity("workflow", Guid.NewGuid());
        workflow2["name"] = "Second Workflow";

        await _serializer.SerializeWorkflowsAsync([workflow1, workflow2]);

        var workflowsDir = Path.Combine(_testOutputDir, "Workflows");
        var files = Directory.GetFiles(workflowsDir, "*.xml");
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public async Task SerializeSecurityRolesAsync_CreatesSecurityRolesDirectory()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.SerializeSecurityRolesAsync(roles);

        var securityRolesDir = Path.Combine(_testOutputDir, "SecurityRoles");
        Assert.True(Directory.Exists(securityRolesDir));
    }

    [Fact]
    public async Task SerializeSecurityRolesAsync_CreatesOneFilePerRole()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.SerializeSecurityRolesAsync(roles);

        var securityRolesDir = Path.Combine(_testOutputDir, "SecurityRoles");
        var files = Directory.GetFiles(securityRolesDir, "*.xml");
        Assert.Single(files);
    }

    [Fact]
    public async Task SerializeSecurityRolesAsync_UsesRoleNameForFileName()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.SerializeSecurityRolesAsync(roles);

        var securityRolesDir = Path.Combine(_testOutputDir, "SecurityRoles");
        var files = Directory.GetFiles(securityRolesDir, "*.xml");
        Assert.Contains("TestRole.xml", files[0]);
    }

    [Fact]
    public async Task SerializeSecurityRolesAsync_CreatesMultipleFilesForMultipleRoles()
    {
        var role1Id = Guid.NewGuid();
        var role2Id = Guid.NewGuid();
        var roles = new Dictionary<Guid, SecurityRole>
        {
            { role1Id, new SecurityRole { Name = "First Role", RoleId = role1Id, Privileges = [] } },
            { role2Id, new SecurityRole { Name = "Second Role", RoleId = role2Id, Privileges = [] } }
        };

        await _serializer.SerializeSecurityRolesAsync(roles);

        var securityRolesDir = Path.Combine(_testOutputDir, "SecurityRoles");
        var files = Directory.GetFiles(securityRolesDir, "*.xml");
        Assert.Equal(2, files.Length);
    }

    [Fact]
    public async Task GenerateTypeDeclarationsAsync_CreatesTypeDeclarationsFile()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.GenerateTypeDeclarationsAsync(roles);

        var typeDefPath = Path.Combine(_testOutputDir, "TypeDeclarations.cs");
        Assert.True(File.Exists(typeDefPath));
    }

    [Fact]
    public async Task GenerateTypeDeclarationsAsync_ContainsNamespace()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.GenerateTypeDeclarationsAsync(roles);

        var typeDefPath = Path.Combine(_testOutputDir, "TypeDeclarations.cs");
        var content = await File.ReadAllTextAsync(typeDefPath);
        Assert.Contains("namespace DG.Tools.XrmMockup", content);
    }

    [Fact]
    public async Task GenerateTypeDeclarationsAsync_ContainsSecurityRolesStruct()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.GenerateTypeDeclarationsAsync(roles);

        var typeDefPath = Path.Combine(_testOutputDir, "TypeDeclarations.cs");
        var content = await File.ReadAllTextAsync(typeDefPath);
        Assert.Contains("public struct SecurityRoles", content);
    }

    [Fact]
    public async Task GenerateTypeDeclarationsAsync_ContainsRoleNameAndGuid()
    {
        var roleId = Guid.NewGuid();
        var role = new SecurityRole
        {
            Name = "Test Role",
            RoleId = roleId,
            Privileges = []
        };
        var roles = new Dictionary<Guid, SecurityRole> { { roleId, role } };

        await _serializer.GenerateTypeDeclarationsAsync(roles);

        var typeDefPath = Path.Combine(_testOutputDir, "TypeDeclarations.cs");
        var content = await File.ReadAllTextAsync(typeDefPath);
        Assert.Contains("TestRole", content);
        Assert.Contains(roleId.ToString(), content);
    }

    [Fact]
    public async Task GenerateTypeDeclarationsAsync_GeneratesAllRoles()
    {
        var roles = new Dictionary<Guid, SecurityRole>
        {
            { Guid.NewGuid(), new SecurityRole { Name = "First Role", RoleId = Guid.NewGuid(), Privileges = [] } },
            { Guid.NewGuid(), new SecurityRole { Name = "Second Role", RoleId = Guid.NewGuid(), Privileges = [] } },
            { Guid.NewGuid(), new SecurityRole { Name = "Third Role", RoleId = Guid.NewGuid(), Privileges = [] } }
        };

        await _serializer.GenerateTypeDeclarationsAsync(roles);

        var typeDefPath = Path.Combine(_testOutputDir, "TypeDeclarations.cs");
        var content = await File.ReadAllTextAsync(typeDefPath);
        Assert.Contains("FirstRole", content);
        Assert.Contains("SecondRole", content);
        Assert.Contains("ThirdRole", content);
    }
}
