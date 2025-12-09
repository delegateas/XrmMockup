using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum account_preferredappointmentdaycode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Sunday", 1033)]
    Sunday = 0,

    [EnumMember]
    [OptionSetMetadata("Monday", 1033)]
    Monday = 1,

    [EnumMember]
    [OptionSetMetadata("Tuesday", 1033)]
    Tuesday = 2,

    [EnumMember]
    [OptionSetMetadata("Wednesday", 1033)]
    Wednesday = 3,

    [EnumMember]
    [OptionSetMetadata("Thursday", 1033)]
    Thursday = 4,

    [EnumMember]
    [OptionSetMetadata("Friday", 1033)]
    Friday = 5,

    [EnumMember]
    [OptionSetMetadata("Saturday", 1033)]
    Saturday = 6,
}