using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum contact_address1_shippingmethodcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Airborne", 1033)]
    Airborne = 1,

    [EnumMember]
    [OptionSetMetadata("DHL", 1033)]
    DHL = 2,

    [EnumMember]
    [OptionSetMetadata("FedEx", 1033)]
    FedEx = 3,

    [EnumMember]
    [OptionSetMetadata("UPS", 1033)]
    UPS = 4,

    [EnumMember]
    [OptionSetMetadata("Postal Mail", 1033)]
    PostalMail = 5,

    [EnumMember]
    [OptionSetMetadata("Full Load", 1033)]
    FullLoad = 6,

    [EnumMember]
    [OptionSetMetadata("Will Call", 1033)]
    WillCall = 7,
}