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
    /// When solutions are specified, only roles from those solutions (and any additional named roles) are returned.
    /// When no solutions are specified, all roles are returned.
    /// </summary>
    Task<Dictionary<Guid, SecurityRole>> GetSecurityRolesAsync(
        Guid rootBusinessUnitId,
        string[] solutions,
        string[] additionalSecurityRoles,
        CancellationToken ct = default);
}
