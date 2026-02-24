using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum account_ownershipcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Public", 1033)]
    @Public = 1,

    [EnumMember]
    [OptionSetMetadata("Private", 1033)]
    @Private = 2,

    [EnumMember]
    [OptionSetMetadata("Subsidiary", 1033)]
    Subsidiary = 3,

    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    Other = 4,
}