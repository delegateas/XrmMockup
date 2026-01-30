using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum contact_preferredcontactmethodcode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Any", 1033)]
    Any = 1,

    [EnumMember]
    [OptionSetMetadata("Email", 1033)]
    Email = 2,

    [EnumMember]
    [OptionSetMetadata("Phone", 1033)]
    Phone = 3,

    [EnumMember]
    [OptionSetMetadata("Fax", 1033)]
    Fax = 4,

    [EnumMember]
    [OptionSetMetadata("Mail", 1033)]
    Mail = 5,
}