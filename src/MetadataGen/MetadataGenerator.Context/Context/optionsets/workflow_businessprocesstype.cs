using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_businessprocesstype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Business Flow", 1033)]
    BusinessFlow = 0,

    [EnumMember]
    [OptionSetMetadata("Task Flow", 1033)]
    TaskFlow = 1,
}