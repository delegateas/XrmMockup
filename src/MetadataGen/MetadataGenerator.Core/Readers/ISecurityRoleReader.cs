using DG.Tools.XrmMockup;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads security role definitions from Dataverse.
/// </summary>
public interface ISecurityRoleReader
{
    /// <summary>
    /// Gets security roles with their privileges.
    /// Duplicate role names are automatically mitigated by appending a counter suffix.
    /// </summary>
    Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        CancellationToken ct = default);
}
