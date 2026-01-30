using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_runas
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Owner", 1033)]
    Owner = 0,

    [EnumMember]
    [OptionSetMetadata("Calling User", 1033)]
    CallingUser = 1,
}