using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_throttlingbehaviortype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("None", 1033)]
    None = 0,

    [EnumMember]
    [OptionSetMetadata("TenantPool", 1033)]
    TenantPool = 1,

    [EnumMember]
    [OptionSetMetadata("CopilotStudio", 1033)]
    CopilotStudio = 2,
}