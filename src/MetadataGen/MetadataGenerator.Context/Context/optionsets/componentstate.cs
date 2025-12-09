using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum componentstate
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Published", 1033)]
    Published = 0,

    [EnumMember]
    [OptionSetMetadata("Unpublished", 1033)]
    Unpublished = 1,

    [EnumMember]
    [OptionSetMetadata("Deleted", 1033)]
    Deleted = 2,

    [EnumMember]
    [OptionSetMetadata("Deleted Unpublished", 1033)]
    DeletedUnpublished = 3,
}