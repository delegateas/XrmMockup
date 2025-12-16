using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_accessmode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Read-Write", 1033)]
    ReadWrite = 0,

    [EnumMember]
    [OptionSetMetadata("Administrative", 1033)]
    Administrative = 1,

    [EnumMember]
    [OptionSetMetadata("Read", 1033)]
    Read = 2,

    [EnumMember]
    [OptionSetMetadata("Support User", 1033)]
    SupportUser = 3,

    [EnumMember]
    [OptionSetMetadata("Non-interactive", 1033)]
    Noninteractive = 4,

    [EnumMember]
    [OptionSetMetadata("Delegated Admin", 1033)]
    DelegatedAdmin = 5,
}