using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_stage
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Pre-operation", 1033)]
    Preoperation = 20,

    [EnumMember]
    [OptionSetMetadata("Post-operation", 1033)]
    Postoperation = 40,
}