using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum account_preferredappointmenttimecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Morning", 1033)]
    Morning = 1,

    [EnumMember]
    [OptionSetMetadata("Afternoon", 1033)]
    Afternoon = 2,

    [EnumMember]
    [OptionSetMetadata("Evening", 1033)]
    Evening = 3,
}