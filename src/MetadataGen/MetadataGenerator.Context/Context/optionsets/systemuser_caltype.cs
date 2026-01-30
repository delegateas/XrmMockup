using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_caltype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Professional", 1033)]
    Professional = 0,

    [EnumMember]
    [OptionSetMetadata("Administrative", 1033)]
    Administrative = 1,

    [EnumMember]
    [OptionSetMetadata("Basic", 1033)]
    Basic = 2,

    [EnumMember]
    [OptionSetMetadata("Device Professional", 1033)]
    DeviceProfessional = 3,

    [EnumMember]
    [OptionSetMetadata("Device Basic", 1033)]
    DeviceBasic = 4,

    [EnumMember]
    [OptionSetMetadata("Essential", 1033)]
    Essential = 5,

    [EnumMember]
    [OptionSetMetadata("Device Essential", 1033)]
    DeviceEssential = 6,

    [EnumMember]
    [OptionSetMetadata("Enterprise", 1033)]
    Enterprise = 7,

    [EnumMember]
    [OptionSetMetadata("Device Enterprise", 1033)]
    DeviceEnterprise = 8,

    [EnumMember]
    [OptionSetMetadata("Sales", 1033)]
    Sales = 9,

    [EnumMember]
    [OptionSetMetadata("Service", 1033)]
    Service = 10,

    [EnumMember]
    [OptionSetMetadata("Field Service", 1033)]
    FieldService = 11,

    [EnumMember]
    [OptionSetMetadata("Project Service", 1033)]
    ProjectService = 12,
}