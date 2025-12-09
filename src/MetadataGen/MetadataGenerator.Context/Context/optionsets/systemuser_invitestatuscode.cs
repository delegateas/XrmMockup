using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum systemuser_invitestatuscode
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Invitation Not Sent", 1033)]
    InvitationNotSent = 0,

    [EnumMember]
    [OptionSetMetadata("Invited", 1033)]
    Invited = 1,

    [EnumMember]
    [OptionSetMetadata("Invitation Near Expired", 1033)]
    InvitationNearExpired = 2,

    [EnumMember]
    [OptionSetMetadata("Invitation Expired", 1033)]
    InvitationExpired = 3,

    [EnumMember]
    [OptionSetMetadata("Invitation Accepted", 1033)]
    InvitationAccepted = 4,

    [EnumMember]
    [OptionSetMetadata("Invitation Rejected", 1033)]
    InvitationRejected = 5,

    [EnumMember]
    [OptionSetMetadata("Invitation Revoked", 1033)]
    InvitationRevoked = 6,
}