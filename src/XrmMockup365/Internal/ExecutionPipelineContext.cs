using Microsoft.Xrm.Sdk;
using System;

namespace DG.Tools.XrmMockup.Internal
{
    /// <summary>
    /// Carries all intermediate state for a single request execution through the pipeline stages.
    /// Populated progressively: BuildContext → PreValidation → PreOperation → Operation → PostOperation.
    /// </summary>
    internal class ExecutionPipelineContext
    {
        // Immutable inputs — set once during BuildPipelineContext
        public OrganizationRequest Request { get; set; }
        public EntityReference UserRef { get; set; }
        public PluginContext ParentPluginContext { get; set; }
        public MockupServiceSettings Settings { get; set; }

        // Derived during BuildPipelineContext
        public PluginContext PluginContext { get; set; }
        public string RequestMessage { get; set; }
        public Tuple<object, string, Guid> EntityInfo { get; set; }
        public EntityReference PrimaryRef { get; set; }
        public EntityCollection EntityCollection { get; set; }
        public bool ShouldTrigger { get; set; }

        // Images — populated at specific stage boundaries
        public Entity PreImage { get; set; }       // fetched before PreValidation
        public Entity SyncPostImage { get; set; }  // fetched at start of PostOperation (sync)
        public Entity AsyncPostImage { get; set; } // fetched before async staging

        // Output — set by the main operation stage
        public OrganizationResponse Response { get; set; }
    }
}
