using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_category
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Workflow", 1033)]
    Workflow = 0,

    [EnumMember]
    [OptionSetMetadata("Dialog", 1033)]
    Dialog = 1,

    [EnumMember]
    [OptionSetMetadata("Business Rule", 1033)]
    BusinessRule = 2,

    [EnumMember]
    [OptionSetMetadata("Action", 1033)]
    Action = 3,

    [EnumMember]
    [OptionSetMetadata("Business Process Flow", 1033)]
    BusinessProcessFlow = 4,

    [EnumMember]
    [OptionSetMetadata("Modern Flow", 1033)]
    ModernFlow = 5,

    [EnumMember]
    [OptionSetMetadata("Desktop Flow", 1033)]
    DesktopFlow = 6,

    [EnumMember]
    [OptionSetMetadata("AI Flow", 1033)]
    AIFlow = 7,
}