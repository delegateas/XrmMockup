using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_type
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Definition", 1033)]
    Definition = 1,

    [EnumMember]
    [OptionSetMetadata("Activation", 1033)]
    Activation = 2,

    [EnumMember]
    [OptionSetMetadata("Template", 1033)]
    Template = 3,
}