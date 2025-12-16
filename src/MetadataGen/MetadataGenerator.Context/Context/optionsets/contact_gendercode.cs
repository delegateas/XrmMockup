using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum contact_gendercode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Male", 1033)]
    Male = 1,

    [EnumMember]
    [OptionSetMetadata("Female", 1033)]
    Female = 2,
}