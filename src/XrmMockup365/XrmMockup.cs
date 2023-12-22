using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("XrmMockup365Test")]
namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 365 instance
    /// </summary>
    public class XrmMockup365 : XrmMockupBase {

        private static readonly Dictionary<XrmMockupSettings, XrmMockup365> instances = new Dictionary<XrmMockupSettings, XrmMockup365>();

        private XrmMockup365(XrmMockupSettings Settings, MetadataSkeleton metadata = null, List<Entity> workflows = null, List<SecurityRole> securityRoles = null) :
            base(Settings, metadata, workflows, securityRoles)
        {
        }
        
        /// <summary>
        /// Gets an instance of XrmMockup365
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup365 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }

            var instance = new XrmMockup365(Settings);
            instances[Settings] = instance;
            return instance;
        }

        /// <summary>
        /// Gets an instance of XrmMockup365 using the same metadata as the provided
        /// </summary>
        /// <param name="xrmMockup"></param>
        public static XrmMockup365 GetInstance(XrmMockup365 xrmMockup)
        {
            return new XrmMockup365(xrmMockup.Settings, xrmMockup.Metadata, xrmMockup.Workflows, xrmMockup.SecurityRoles);
        }
    }
}
