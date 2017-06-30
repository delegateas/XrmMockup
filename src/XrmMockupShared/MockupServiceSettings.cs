using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DG.Tools {
    /// <summary>
    /// A class that encapsulates settings for MockupService
    /// </summary>
    public class MockupServiceSettings {
        public enum Role { SDK, UI }
        /// <summary>
        /// Indicates whether plugins and workflows should be triggered
        /// </summary>
        public bool TriggerProcesses { get; private set; }
        /// <summary>
        /// Indicates whether read only attributes should be settable
        /// </summary>
        public bool SetUnsettableFields { get; private set; }
        /// <summary>
        /// Indicates whether the service should be a SDK or UI service
        /// </summary>
        public Role ServiceRole { get; private set; }
        /// <summary>
        /// Creates a default service setting
        /// </summary>

        public MockupServiceSettings() : this(true, false, Role.SDK) { }

        /// <summary>
        /// Creates a custom service setting
        /// </summary>
        /// <param name="triggerProcesses"></param>
        /// <param name="setUnsettableFields"></param>
        /// <param name="serviceRole"></param>
        public MockupServiceSettings(bool triggerProcesses, bool setUnsettableFields, Role serviceRole) {
            this.TriggerProcesses = triggerProcesses;
            this.SetUnsettableFields = setUnsettableFields;
            this.ServiceRole = serviceRole;
        }
    }
}
