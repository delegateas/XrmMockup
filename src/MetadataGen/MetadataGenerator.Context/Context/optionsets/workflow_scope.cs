using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_scope
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("User", 1033)]
    User = 1,

    [EnumMember]
    [OptionSetMetadata("Business Unit", 1033)]
    BusinessUnit = 2,

    [EnumMember]
    [OptionSetMetadata("Parent: Child Business Units", 1033)]
    ParentChildBusinessUnits = 3,

    [EnumMember]
    [OptionSetMetadata("Organization", 1033)]
    Organization = 4,
}