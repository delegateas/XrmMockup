using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_statuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Draft", 1033)]
    Draft = 1,

    [EnumMember]
    [OptionSetMetadata("Activated", 1033)]
    Activated = 2,

    [EnumMember]
    [OptionSetMetadata("CompanyDLPViolation", 1033)]
    CompanyDLPViolation = 3,
}