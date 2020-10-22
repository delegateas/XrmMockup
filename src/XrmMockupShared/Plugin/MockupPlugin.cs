
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;

namespace DG.Tools.XrmMockup {

    // StepConfig           : className, PluginExecutionStage, PluginEventOperation, LogicalName
    // ExtendedStepConfig   : PluginDeployment, PluginExecutionMode, Name, ExecutionOrder, FilteredAttributes, UserContext
    // ImageTuple           : Name, EntityAlias, PluginImageType, Attributes
    //using StepConfig = System.Tuple<string, int, string, string>;
   // using ExtendedStepConfig = System.Tuple<int, int, string, int, string, string>;
    //using ImageTuple = System.Tuple<string, string, int, string>;
    using System.Reflection;
    using DG.Tools.XrmMockup.Config;

    /// <summary>
    /// Base class for all Plugins.
    /// </summary>    
    public class MockupPlugin : IPlugin {
        protected class LocalPluginContext {
            internal IServiceProvider ServiceProvider {
                get;

                private set;
            }

            internal IOrganizationService OrganizationService {
                get;

                private set;
            }

            // Delegate A/S added:
            internal IOrganizationService OrganizationAdminService {
                get;

                private set;
            }

            internal IPluginExecutionContext PluginExecutionContext {
                get;

                private set;
            }

            internal ITracingService TracingService {
                get;

                private set;
            }

            private LocalPluginContext() {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider) {
                if (serviceProvider == null) {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);

                // Delegate A/S added: Use the factory to generate the Organization Admin Service.
                this.OrganizationAdminService = factory.CreateOrganizationService(null);
            }

            internal void Trace(string message) {
                if (string.IsNullOrWhiteSpace(message) || this.TracingService == null) {
                    return;
                }

                if (this.PluginExecutionContext == null) {
                    this.TracingService.Trace(message);
                } else {
                    this.TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        this.PluginExecutionContext.CorrelationId,
                        this.PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        private Collection<Tuple<int, string, string, Action<LocalPluginContext>>> registeredEvents;

        /// <summary>
        /// Gets the List of events that the plug-in should fire for. Each List
        /// Item is a <see cref="System.Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
        /// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
        /// </summary>
        protected Collection<Tuple<int, string, string, Action<LocalPluginContext>>> RegisteredEvents {
            get {
                if (this.registeredEvents == null) {
                    this.registeredEvents = new Collection<Tuple<int, string, string, Action<LocalPluginContext>>>();
                }

                return this.registeredEvents;
            }
        }

        /// <summary>
        /// Gets or sets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName {
            get;

            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockupPlugin"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref="Type"/> of the derived class.</param>
        internal MockupPlugin(Type childClassName) {
            this.ChildClassName = childClassName.ToString();
        }


        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public void Execute(IServiceProvider serviceProvider) {
            if (serviceProvider == null) {
                throw new ArgumentNullException("serviceProvider");
            }

            // Construct the Local plug-in context.
            LocalPluginContext localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.ChildClassName));

            try {
                // Iterate over all of the expected registered events to ensure that the plugin
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> entityAction =
                    (from a in this.RegisteredEvents
                     where (
                     a.Item1 == localcontext.PluginExecutionContext.Stage &&
                     a.Item2.ToLower() == localcontext.PluginExecutionContext.MessageName.ToLower() &&
                     (string.IsNullOrWhiteSpace(a.Item3) ? true : a.Item3 == localcontext.PluginExecutionContext.PrimaryEntityName)
                     )
                     select a.Item4).FirstOrDefault();

                if (entityAction != null) {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}",
                        this.ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    try {
                        entityAction.Invoke(localcontext);
                    } catch (TargetInvocationException e) {
                        throw e.InnerException;
                    }

                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
                }
            } catch (FaultException<OrganizationServiceFault> e) {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            } finally {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.ChildClassName));
            }
        }


        // Delegate A/S added:
        /// <summary>
        /// The methods exposes the RegisteredEvents as a collection of tuples
        /// containing:
        /// - The full assembly name of the class containing the RegisteredEvents
        /// - The Pipeline Stage
        /// - The Event Operation
        /// - Logical Entity Name (or empty for all)
        /// This will allow to instantiate each plug-in and iterate through the 
        /// PluginProcessingSteps in order to sync the code repository with 
        /// MS CRM without have to use any extra layer to perform this operation
        /// </summary>
        /// <returns></returns>
        /// 

        public IEnumerable<Tuple<string, int, string, string>> PluginProcessingSteps() {
            var className = this.ChildClassName;
            foreach (var events in this.RegisteredEvents) {
                yield return new Tuple<string, int, string, string>
                    (className, events.Item1, events.Item2, events.Item3);
            }
        }

        #region Additional helper methods

        protected static T GetImage<T>(LocalPluginContext context, PluginImageType PluginImageType, string name) where T : Entity {
            EntityImageCollection collection = null;
            if (PluginImageType == PluginImageType.PreImage) {
                collection = context.PluginExecutionContext.PreEntityImages;
            } else if (PluginImageType == PluginImageType.PostImage) {
                collection = context.PluginExecutionContext.PostEntityImages;
            }

            Entity entity;
            if (collection != null && collection.TryGetValue(name, out entity)) {
                return entity.ToEntity<T>();
            } else {
                return null;
            }
        }

        protected static T GetImage<T>(LocalPluginContext context, PluginImageType PluginImageType) where T : Entity {
            return GetImage<T>(context, PluginImageType, PluginImageType.ToString());
        }

        protected static T GetPreImage<T>(LocalPluginContext context, string name = "PreImage") where T : Entity {
            return GetImage<T>(context, PluginImageType.PreImage, name);
        }

        protected static T GetPostImage<T>(LocalPluginContext context, string name = "PostImage") where T : Entity {
            return GetImage<T>(context, PluginImageType.PostImage, name);
        }

        #endregion


        #region PluginStepConfig retrieval
        /// <summary>
        /// Made by Delegate A/S
        /// Get the plugin step configurations.
        /// </summary>
        /// <returns>List of steps</returns>
        public IEnumerable<PluginStepConfig> PluginProcessingStepConfigs() {
            var className = this.ChildClassName;
            foreach (var config in this.PluginStepConfigs) {
                yield return
                    new PluginStepConfig(
                        new StepConfig(className, config._PluginExecutionStage, config._PluginEventOperation, config._LogicalName),
                        new ExtendedStepConfig(config._PluginDeployment, config._PluginExecutionMode, config._Name, config._ExecutionOrder, config._FilteredAttributes, config._UserContext),
                        config.GetImages());
            }
        }


        protected PluginStepConfig<Entity> RegisterPluginStep(
            string logicalName, PluginEventOperation PluginEventOperation, PluginExecutionStage PluginExecutionStage, Action<LocalPluginContext> action){
            PluginStepConfig<Entity> stepConfig = new PluginStepConfig<Entity>(logicalName, PluginEventOperation, PluginExecutionStage);
            this.PluginStepConfigs.Add((IPluginStepConfig)stepConfig);

            this.RegisteredEvents.Add(
                new Tuple<int, string, string, Action<LocalPluginContext>>(
                    stepConfig._PluginExecutionStage,
                    stepConfig._PluginEventOperation.ToLower(),
                    stepConfig._LogicalName,
                    new Action<LocalPluginContext>(action)));

            return stepConfig;
        }


        private Collection<IPluginStepConfig> pluginConfigs;
        private Collection<IPluginStepConfig> PluginStepConfigs {
            get {
                if (this.pluginConfigs == null) {
                    this.pluginConfigs = new Collection<IPluginStepConfig>();
                }
                return this.pluginConfigs;
            }
        }
        #endregion

    }

    #region PluginStepConfig made by Delegate A/S
    interface IPluginStepConfig {
        string _LogicalName { get; }
        string _PluginEventOperation { get; }
        int _PluginExecutionStage { get; }

        string _Name { get; }
        int _PluginDeployment { get; }
        int _PluginExecutionMode { get; }
        int _ExecutionOrder { get; }
        string _FilteredAttributes { get; }
        Guid _UserContext { get; }
        IEnumerable<ImageConfig> GetImages();
    }

    /// <summary>
    /// Made by Delegate A/S
    /// Class to encapsulate the various configurations that can be made 
    /// to a plugin step.
    /// </summary>
    public class PluginStepConfig<T> : IPluginStepConfig where T : Entity {
        public string _LogicalName { get; private set; }
        public string _PluginEventOperation { get; private set; }
        public int _PluginExecutionStage { get; private set; }

        public string _Name { get; private set; }
        public int _PluginDeployment { get; private set; }
        public int _PluginExecutionMode { get; private set; }
        public int _ExecutionOrder { get; private set; }
        public Guid _UserContext { get; private set; }

        public Collection<PluginStepImage> _Images = new Collection<PluginStepImage>();
        public Collection<string> _FilteredAttributesCollection = new Collection<string>();

        public string _FilteredAttributes {
            get {
                if (this._FilteredAttributesCollection.Count == 0) return null;
                return string.Join(",", this._FilteredAttributesCollection).ToLower();
            }
        }


        public PluginStepConfig(string logicalName, PluginEventOperation PluginEventOperation, PluginExecutionStage PluginExecutionStage) {
            this._LogicalName = logicalName;
            this._PluginEventOperation = PluginEventOperation.ToString();
            this._PluginExecutionStage = (int)PluginExecutionStage;
            this._PluginDeployment = (int)PluginDeployment.ServerOnly;
            this._PluginExecutionMode = (int)PluginExecutionMode.Synchronous;
            this._ExecutionOrder = 1;
            this._UserContext = Guid.Empty;
        }

        private PluginStepConfig<T> AddFilteredAttribute(Expression<Func<T, object>> lambda) {
            this._FilteredAttributesCollection.Add(GetMemberName(lambda));
            return this;
        }

        public PluginStepConfig<T> AddFilteredAttributes(params Expression<Func<T, object>>[] lambdas) {
            foreach (var lambda in lambdas) this.AddFilteredAttribute(lambda);
            return this;
        }

        public PluginStepConfig<T> SetPluginDeployment(PluginDeployment PluginDeployment) {
            this._PluginDeployment = (int)PluginDeployment;
            return this;
        }

        public PluginStepConfig<T> SetExecutionMode(PluginExecutionMode PluginExecutionMode) {
            this._PluginExecutionMode = (int)PluginExecutionMode;
            return this;
        }

        public PluginStepConfig<T> SetName(string name) {
            this._Name = name;
            return this;
        }

        public PluginStepConfig<T> SetExecutionOrder(int executionOrder) {
            this._ExecutionOrder = executionOrder;
            return this;
        }

        public PluginStepConfig<T> SetUserContext(Guid userContext) {
            this._UserContext = userContext;
            return this;
        }

        public PluginStepConfig<T> AddImage(PluginImageType PluginImageType) {
            return this.AddImage(PluginImageType, null);
        }

        public PluginStepConfig<T> AddImage(PluginImageType PluginImageType, params Expression<Func<T, object>>[] attributes) {
            return this.AddImage(PluginImageType.ToString(), PluginImageType.ToString(), PluginImageType, attributes);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, PluginImageType PluginImageType) {
            return this.AddImage(name, entityAlias, PluginImageType, null);
        }

        public PluginStepConfig<T> AddImage(string name, string entityAlias, PluginImageType PluginImageType, params Expression<Func<T, object>>[] attributes) {
            this._Images.Add(new PluginStepImage(name, entityAlias, PluginImageType, attributes));
            return this;
        }

        public IEnumerable<ImageConfig> GetImages() {
            foreach (var image in this._Images) {
                yield return new ImageConfig(image.Name, image.EntityAlias, image.PluginImageType, image.Attributes);
            }
        }

        /// <summary>
        /// Container for information about images attached to steps
        /// </summary>
        public class PluginStepImage {
            public string Name { get; private set; }
            public string EntityAlias { get; private set; }
            public int PluginImageType { get; private set; }
            public string Attributes { get; private set; }

            public PluginStepImage(string name, string entityAlias, PluginImageType PluginImageType, Expression<Func<T, object>>[] attributes) {
                this.Name = name;
                this.EntityAlias = entityAlias;
                this.PluginImageType = (int)PluginImageType;

                if (attributes != null && attributes.Length > 0) {
                    this.Attributes = string.Join(",", attributes.Select(x => PluginStepConfig<T>.GetMemberName(x))).ToLower();
                } else {
                    this.Attributes = null;
                }
            }
        }


        private static string GetMemberName(Expression<Func<T, object>> lambda) {
            MemberExpression body = lambda.Body as MemberExpression;

            if (body == null) {
                UnaryExpression ubody = (UnaryExpression)lambda.Body;
                body = ubody.Operand as MemberExpression;
            }

            return body.Member.Name;
        }
    }

    class AnyEntity : Entity {
        public AnyEntity() : base("") { }
    }

    /**
     * Enums to help setup plugin steps
     */

    public enum PluginExecutionMode {
        Synchronous = 0,
        Asynchronous = 1,
    }

    public enum PluginExecutionStage {
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40,
    }

    public enum PluginDeployment {
        ServerOnly = 0,
        MicrosoftDynamicsCRMClientforOutlookOnly = 1,
        Both = 2,
    }

    // PluginEventOperation based on CRM 2016
    public enum PluginEventOperation {
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

    public enum PluginImageType {
        PreImage = 0,
        PostImage = 1,
        Both = 2,
    }
    #endregion
}