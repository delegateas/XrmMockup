namespace DG.Some.Namespace {
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.Linq.Expressions;
    using Microsoft.Xrm.Sdk;
    using DG.Tools.XrmMockup;

    /// <summary>
    /// Base class for all Plugins.
    /// </summary>    
    public abstract class PluginNonDaxif : IPlugin {
        protected class LocalPluginContext {
            internal IServiceProvider ServiceProvider {
                get;

                private set;
            }

            internal IOrganizationService OrganizationService {
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
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref="" cred="Type"/> of the derived class.</param>
        internal PluginNonDaxif(Type childClassName) {
            this.ChildClassName = childClassName.ToString();
        }

        public abstract void Execute(IServiceProvider serviceProvider);
    }
}