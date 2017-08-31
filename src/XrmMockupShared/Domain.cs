using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {


    public enum ExecutionMode {
        Synchronous = 0,
        Asynchronous = 1,
    }

    public enum ExecutionStage {
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40,
    }

    public enum Deployment {
        ServerOnly = 0,
        MicrosoftDynamicsCRMClientforOutlookOnly = 1,
        Both = 2,
    }

    // EventOperation based on CRM 2016
    public enum EventOperation {
        AddItem,
        AddListMembers,
        AddMember,
        AddMembers,
        AddPrincipalToQueue,
        AddPrivileges,
        AddProductToKit,
        AddRecurrence,
        AddToQueue,
        AddUserToRecordTeam,
        ApplyRecordCreationAndUpdateRule,
        Assign,
        AssignUserRoles,
        Associate,
        BackgroundSend,
        Book,
        CalculatePrice,
        Cancel,
        CheckIncoming,
        CheckPromote,
        Clone,
        CloneProduct,
        Close,
        CopyDynamicListToStatic,
        CopySystemForm,
        Create,
        CreateException,
        CreateInstance,
        CreateKnowledgeArticleTranslation,
        CreateKnowledgeArticleVersion,
        Delete,
        DeleteOpenInstances,
        DeliverIncoming,
        DeliverPromote,
        DetachFromQueue,
        Disassociate,
        Execute,
        ExecuteById,
        Export,
        ExportAll,
        ExportCompressed,
        ExportCompressedAll,
        GenerateSocialProfile,
        GetDefaultPriceLevel,
        GrantAccess,
        Handle,
        Import,
        ImportAll,
        ImportCompressedAll,
        ImportCompressedWithProgress,
        ImportWithProgress,
        LockInvoicePricing,
        LockSalesOrderPricing,
        Lose,
        Merge,
        ModifyAccess,
        PickFromQueue,
        Publish,
        PublishAll,
        PublishTheme,
        QualifyLead,
        Recalculate,
        ReleaseToQueue,
        RemoveFromQueue,
        RemoveItem,
        RemoveMember,
        RemoveMembers,
        RemovePrivilege,
        RemoveProductFromKit,
        RemoveRelated,
        RemoveUserFromRecordTeam,
        RemoveUserRoles,
        ReplacePrivileges,
        Reschedule,
        Retrieve,
        RetrieveExchangeRate,
        RetrieveFilteredForms,
        RetrieveMultiple,
        RetrievePersonalWall,
        RetrievePrincipalAccess,
        RetrieveRecordWall,
        RetrieveSharedPrincipalsAndAccess,
        RetrieveUnpublished,
        RetrieveUnpublishedMultiple,
        RetrieveUserQueues,
        RevokeAccess,
        Route,
        RouteTo,
        Send,
        SendFromTemplate,
        SetLocLabels,
        SetRelated,
        SetState,
        SetStateDynamicEntity,
        TriggerServiceEndpointCheck,
        UnlockInvoicePricing,
        UnlockSalesOrderPricing,
        Update,
        ValidateRecurrenceRule,
        Win
    }

    public enum ImageType {
        PreImage = 0,
        PostImage = 1,
        Both = 2,
    }

    internal enum FullNameConventionCode {
        LastFirst = 0,
        FirstLast = 1,
        LastFirstMiddleInitial = 2,
        FirstMiddleInitialLast = 3,
        LastFirstMiddle = 4,
        FirstMiddleLast = 5,
        LastSpaceFirst = 6,
        LastNoSpaceFirst = 7
    }
}
