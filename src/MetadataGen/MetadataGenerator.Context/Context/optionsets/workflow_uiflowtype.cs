using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum workflow_uiflowtype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Windows recorder (V1)", 1033)]
    WindowsrecorderV1 = 0,

    [EnumMember]
    [OptionSetMetadata("Selenium IDE", 1033)]
    SeleniumIDE = 1,

    [EnumMember]
    [OptionSetMetadata("Power Automate Desktop", 1033)]
    PowerAutomateDesktop = 2,

    [EnumMember]
    [OptionSetMetadata("Test", 1033)]
    Test = 3,

    [EnumMember]
    [OptionSetMetadata("Recording", 1033)]
    Recording = 101,
}