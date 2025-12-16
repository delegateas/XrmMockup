using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum activityparty_instancetypecode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Not Recurring", 1033)]
    NotRecurring = 0,

    [EnumMember]
    [OptionSetMetadata("Recurring Master", 1033)]
    RecurringMaster = 1,

    [EnumMember]
    [OptionSetMetadata("Recurring Instance", 1033)]
    RecurringInstance = 2,

    [EnumMember]
    [OptionSetMetadata("Recurring Exception", 1033)]
    RecurringException = 3,

    [EnumMember]
    [OptionSetMetadata("Recurring Future Exception", 1033)]
    RecurringFutureException = 4,
}