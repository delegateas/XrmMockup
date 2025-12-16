using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum team_type
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Owner", 1033)]
    Owner = 0,

    [EnumMember]
    [OptionSetMetadata("Access", 1033)]
    Access = 1,

    [EnumMember]
    [OptionSetMetadata("Security Group", 1033)]
    SecurityGroup = 2,

    [EnumMember]
    [OptionSetMetadata("Office Group", 1033)]
    OfficeGroup = 3,
}