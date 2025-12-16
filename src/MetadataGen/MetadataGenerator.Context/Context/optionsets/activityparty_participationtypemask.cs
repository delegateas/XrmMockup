using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum activityparty_participationtypemask
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Sender", 1033)]
    Sender = 1,

    [EnumMember]
    [OptionSetMetadata("To Recipient", 1033)]
    ToRecipient = 2,

    [EnumMember]
    [OptionSetMetadata("CC Recipient", 1033)]
    CCRecipient = 3,

    [EnumMember]
    [OptionSetMetadata("BCC Recipient", 1033)]
    BCCRecipient = 4,

    [EnumMember]
    [OptionSetMetadata("Required attendee", 1033)]
    Requiredattendee = 5,

    [EnumMember]
    [OptionSetMetadata("Optional attendee", 1033)]
    Optionalattendee = 6,

    [EnumMember]
    [OptionSetMetadata("Organizer", 1033)]
    Organizer = 7,

    [EnumMember]
    [OptionSetMetadata("Regarding", 1033)]
    Regarding = 8,

    [EnumMember]
    [OptionSetMetadata("Owner", 1033)]
    Owner = 9,

    [EnumMember]
    [OptionSetMetadata("Resource", 1033)]
    Resource = 10,

    [EnumMember]
    [OptionSetMetadata("Customer", 1033)]
    Customer = 11,

    [EnumMember]
    [OptionSetMetadata("Chat Participant", 1033)]
    ChatParticipant = 12,

    [EnumMember]
    [OptionSetMetadata("Related", 1033)]
    Related = 13,
}