using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum account_statuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Active", 1033)]
    Active = 1,

    [EnumMember]
    [OptionSetMetadata("Inactive", 1033)]
    Inactive = 2,
}