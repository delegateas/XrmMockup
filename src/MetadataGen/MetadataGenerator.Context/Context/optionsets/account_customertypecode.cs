using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum account_customertypecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Competitor", 1033)]
    Competitor = 1,

    [EnumMember]
    [OptionSetMetadata("Consultant", 1033)]
    Consultant = 2,

    [EnumMember]
    [OptionSetMetadata("Customer", 1033)]
    Customer = 3,

    [EnumMember]
    [OptionSetMetadata("Investor", 1033)]
    Investor = 4,

    [EnumMember]
    [OptionSetMetadata("Partner", 1033)]
    Partner = 5,

    [EnumMember]
    [OptionSetMetadata("Influencer", 1033)]
    Influencer = 6,

    [EnumMember]
    [OptionSetMetadata("Press", 1033)]
    Press = 7,

    [EnumMember]
    [OptionSetMetadata("Prospect", 1033)]
    Prospect = 8,

    [EnumMember]
    [OptionSetMetadata("Reseller", 1033)]
    Reseller = 9,

    [EnumMember]
    [OptionSetMetadata("Supplier", 1033)]
    Supplier = 10,

    [EnumMember]
    [OptionSetMetadata("Vendor", 1033)]
    Vendor = 11,

    [EnumMember]
    [OptionSetMetadata("Other", 1033)]
    Other = 12,
}