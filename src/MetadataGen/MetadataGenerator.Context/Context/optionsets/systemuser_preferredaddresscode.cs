using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_preferredaddresscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Mailing Address", 1033)]
    MailingAddress = 1,

    [EnumMember]
    [OptionSetMetadata("Other Address", 1033)]
    OtherAddress = 2,
}