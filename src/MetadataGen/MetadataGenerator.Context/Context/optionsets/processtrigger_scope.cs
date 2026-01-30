using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum processtrigger_scope
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Form", 1033)]
    Form = 1,

    [EnumMember]
    [OptionSetMetadata("Entity", 1033)]
    Entity = 2,
}