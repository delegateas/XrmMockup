using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum contact_accountrolecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Decision Maker", 1033)]
    DecisionMaker = 1,

    [EnumMember]
    [OptionSetMetadata("Employee", 1033)]
    Employee = 2,

    [EnumMember]
    [OptionSetMetadata("Influencer", 1033)]
    Influencer = 3,
}