using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum contact_familystatuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Single", 1033)]
    Single = 1,

    [EnumMember]
    [OptionSetMetadata("Married", 1033)]
    Married = 2,

    [EnumMember]
    [OptionSetMetadata("Divorced", 1033)]
    Divorced = 3,

    [EnumMember]
    [OptionSetMetadata("Widowed", 1033)]
    Widowed = 4,
}