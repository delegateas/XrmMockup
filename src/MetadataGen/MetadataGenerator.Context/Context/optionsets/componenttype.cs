using System.Runtime.Serialization;

namespace XrmMockup.MetadataGenerator.Tool.Context;

[System.CodeDom.Compiler.GeneratedCode("DataverseProxyGenerator", "4.0.0.22")]
[DataContract]
#pragma warning disable CS8981
public enum componenttype
#pragma warning restore CS8981
{
    [EnumMember]
    [OptionSetMetadata("Entity", 1033)]
    Entity = 1,

    [EnumMember]
    [OptionSetMetadata("Attribute", 1033)]
    Attribute = 2,

    [EnumMember]
    [OptionSetMetadata("Relationship", 1033)]
    Relationship = 3,

    [EnumMember]
    [OptionSetMetadata("Attribute Picklist Value", 1033)]
    AttributePicklistValue = 4,

    [EnumMember]
    [OptionSetMetadata("Attribute Lookup Value", 1033)]
    AttributeLookupValue = 5,

    [EnumMember]
    [OptionSetMetadata("View Attribute", 1033)]
    ViewAttribute = 6,

    [EnumMember]
    [OptionSetMetadata("Localized Label", 1033)]
    LocalizedLabel = 7,

    [EnumMember]
    [OptionSetMetadata("Relationship Extra Condition", 1033)]
    RelationshipExtraCondition = 8,

    [EnumMember]
    [OptionSetMetadata("Option Set", 1033)]
    OptionSet = 9,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship", 1033)]
    EntityRelationship = 10,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship Role", 1033)]
    EntityRelationshipRole = 11,

    [EnumMember]
    [OptionSetMetadata("Entity Relationship Relationships", 1033)]
    EntityRelationshipRelationships = 12,

    [EnumMember]
    [OptionSetMetadata("Managed Property", 1033)]
    ManagedProperty = 13,

    [EnumMember]
    [OptionSetMetadata("Entity Key", 1033)]
    EntityKey = 14,

    [EnumMember]
    [OptionSetMetadata("Privilege", 1033)]
    Privilege = 16,

    [EnumMember]
    [OptionSetMetadata("PrivilegeObjectTypeCode", 1033)]
    PrivilegeObjectTypeCode = 17,

    [EnumMember]
    [OptionSetMetadata("Index", 1033)]
    Index = 18,

    [EnumMember]
    [OptionSetMetadata("Role", 1033)]
    Role = 20,

    [EnumMember]
    [OptionSetMetadata("Role Privilege", 1033)]
    RolePrivilege = 21,

    [EnumMember]
    [OptionSetMetadata("Display String", 1033)]
    DisplayString = 22,

    [EnumMember]
    [OptionSetMetadata("Display String Map", 1033)]
    DisplayStringMap = 23,

    [EnumMember]
    [OptionSetMetadata("Form", 1033)]
    Form = 24,

    [EnumMember]
    [OptionSetMetadata("Organization", 1033)]
    Organization = 25,

    [EnumMember]
    [OptionSetMetadata("Saved Query", 1033)]
    SavedQuery = 26,

    [EnumMember]
    [OptionSetMetadata("Workflow", 1033)]
    Workflow = 29,

    [EnumMember]
    [OptionSetMetadata("Report", 1033)]
    Report = 31,

    [EnumMember]
    [OptionSetMetadata("Report Entity", 1033)]
    ReportEntity = 32,

    [EnumMember]
    [OptionSetMetadata("Report Category", 1033)]
    ReportCategory = 33,

    [EnumMember]
    [OptionSetMetadata("Report Visibility", 1033)]
    ReportVisibility = 34,

    [EnumMember]
    [OptionSetMetadata("Attachment", 1033)]
    Attachment = 35,

    [EnumMember]
    [OptionSetMetadata("Email Template", 1033)]
    EmailTemplate = 36,

    [EnumMember]
    [OptionSetMetadata("Contract Template", 1033)]
    ContractTemplate = 37,

    [EnumMember]
    [OptionSetMetadata("KB Article Template", 1033)]
    KBArticleTemplate = 38,

    [EnumMember]
    [OptionSetMetadata("Mail Merge Template", 1033)]
    MailMergeTemplate = 39,

    [EnumMember]
    [OptionSetMetadata("Duplicate Rule", 1033)]
    DuplicateRule = 44,

    [EnumMember]
    [OptionSetMetadata("Duplicate Rule Condition", 1033)]
    DuplicateRuleCondition = 45,

    [EnumMember]
    [OptionSetMetadata("Entity Map", 1033)]
    EntityMap = 46,

    [EnumMember]
    [OptionSetMetadata("Attribute Map", 1033)]
    AttributeMap = 47,

    [EnumMember]
    [OptionSetMetadata("Ribbon Command", 1033)]
    RibbonCommand = 48,

    [EnumMember]
    [OptionSetMetadata("Ribbon Context Group", 1033)]
    RibbonContextGroup = 49,

    [EnumMember]
    [OptionSetMetadata("Ribbon Customization", 1033)]
    RibbonCustomization = 50,

    [EnumMember]
    [OptionSetMetadata("Ribbon Rule", 1033)]
    RibbonRule = 52,

    [EnumMember]
    [OptionSetMetadata("Ribbon Tab To Command Map", 1033)]
    RibbonTabToCommandMap = 53,

    [EnumMember]
    [OptionSetMetadata("Ribbon Diff", 1033)]
    RibbonDiff = 55,

    [EnumMember]
    [OptionSetMetadata("Saved Query Visualization", 1033)]
    SavedQueryVisualization = 59,

    [EnumMember]
    [OptionSetMetadata("System Form", 1033)]
    SystemForm = 60,

    [EnumMember]
    [OptionSetMetadata("Web Resource", 1033)]
    WebResource = 61,

    [EnumMember]
    [OptionSetMetadata("Site Map", 1033)]
    SiteMap = 62,

    [EnumMember]
    [OptionSetMetadata("Connection Role", 1033)]
    ConnectionRole = 63,

    [EnumMember]
    [OptionSetMetadata("Complex Control", 1033)]
    ComplexControl = 64,

    [EnumMember]
    [OptionSetMetadata("Hierarchy Rule", 1033)]
    HierarchyRule = 65,

    [EnumMember]
    [OptionSetMetadata("Custom Control", 1033)]
    CustomControl = 66,

    [EnumMember]
    [OptionSetMetadata("Custom Control Default Config", 1033)]
    CustomControlDefaultConfig = 68,

    [EnumMember]
    [OptionSetMetadata("Field Security Profile", 1033)]
    FieldSecurityProfile = 70,

    [EnumMember]
    [OptionSetMetadata("Field Permission", 1033)]
    FieldPermission = 71,

    [EnumMember]
    [OptionSetMetadata("Plugin Type", 1033)]
    PluginType = 90,

    [EnumMember]
    [OptionSetMetadata("Plugin Assembly", 1033)]
    PluginAssembly = 91,

    [EnumMember]
    [OptionSetMetadata("SDK Message Processing Step", 1033)]
    SDKMessageProcessingStep = 92,

    [EnumMember]
    [OptionSetMetadata("SDK Message Processing Step Image", 1033)]
    SDKMessageProcessingStepImage = 93,

    [EnumMember]
    [OptionSetMetadata("Service Endpoint", 1033)]
    ServiceEndpoint = 95,

    [EnumMember]
    [OptionSetMetadata("Routing Rule", 1033)]
    RoutingRule = 150,

    [EnumMember]
    [OptionSetMetadata("Routing Rule Item", 1033)]
    RoutingRuleItem = 151,

    [EnumMember]
    [OptionSetMetadata("SLA", 1033)]
    SLA = 152,

    [EnumMember]
    [OptionSetMetadata("SLA Item", 1033)]
    SLAItem = 153,

    [EnumMember]
    [OptionSetMetadata("Convert Rule", 1033)]
    ConvertRule = 154,

    [EnumMember]
    [OptionSetMetadata("Convert Rule Item", 1033)]
    ConvertRuleItem = 155,

    [EnumMember]
    [OptionSetMetadata("Mobile Offline Profile", 1033)]
    MobileOfflineProfile = 161,

    [EnumMember]
    [OptionSetMetadata("Mobile Offline Profile Item", 1033)]
    MobileOfflineProfileItem = 162,

    [EnumMember]
    [OptionSetMetadata("Similarity Rule", 1033)]
    SimilarityRule = 165,

    [EnumMember]
    [OptionSetMetadata("Data Source Mapping", 1033)]
    DataSourceMapping = 166,

    [EnumMember]
    [OptionSetMetadata("SDKMessage", 1033)]
    SDKMessage = 201,

    [EnumMember]
    [OptionSetMetadata("SDKMessageFilter", 1033)]
    SDKMessageFilter = 202,

    [EnumMember]
    [OptionSetMetadata("SdkMessagePair", 1033)]
    SdkMessagePair = 203,

    [EnumMember]
    [OptionSetMetadata("SdkMessageRequest", 1033)]
    SdkMessageRequest = 204,

    [EnumMember]
    [OptionSetMetadata("SdkMessageRequestField", 1033)]
    SdkMessageRequestField = 205,

    [EnumMember]
    [OptionSetMetadata("SdkMessageResponse", 1033)]
    SdkMessageResponse = 206,

    [EnumMember]
    [OptionSetMetadata("SdkMessageResponseField", 1033)]
    SdkMessageResponseField = 207,

    [EnumMember]
    [OptionSetMetadata("Import Map", 1033)]
    ImportMap = 208,

    [EnumMember]
    [OptionSetMetadata("WebWizard", 1033)]
    WebWizard = 210,

    [EnumMember]
    [OptionSetMetadata("Canvas App", 1033)]
    CanvasApp = 300,

    [EnumMember]
    [OptionSetMetadata("Connector", 1033)]
    Connector = 371,

    [EnumMember]
    [OptionSetMetadata("Connector", 1033)]
    Connector_1 = 372,

    [EnumMember]
    [OptionSetMetadata("Environment Variable Definition", 1033)]
    EnvironmentVariableDefinition = 380,

    [EnumMember]
    [OptionSetMetadata("Environment Variable Value", 1033)]
    EnvironmentVariableValue = 381,

    [EnumMember]
    [OptionSetMetadata("AI Project Type", 1033)]
    AIProjectType = 400,

    [EnumMember]
    [OptionSetMetadata("AI Project", 1033)]
    AIProject = 401,

    [EnumMember]
    [OptionSetMetadata("AI Configuration", 1033)]
    AIConfiguration = 402,

    [EnumMember]
    [OptionSetMetadata("Entity Analytics Configuration", 1033)]
    EntityAnalyticsConfiguration = 430,

    [EnumMember]
    [OptionSetMetadata("Attribute Image Configuration", 1033)]
    AttributeImageConfiguration = 431,

    [EnumMember]
    [OptionSetMetadata("Entity Image Configuration", 1033)]
    EntityImageConfiguration = 432,
}