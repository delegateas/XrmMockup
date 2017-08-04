using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 2015 instance
    /// </summary>
    public class XrmMockup365 : XrmMockupBase {

        private static Dictionary<XrmMockupSettings, XrmMockup365> instances = new Dictionary<XrmMockupSettings, XrmMockup365>();


        private XrmMockup365(XrmMockupSettings Settings, MetadataSkeleton Metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) : 
            base(Settings, Metadata, Workflows, SecurityRoles) {
        }
        
        /// <summary>
        /// Gets an instance of XrmMockup2015
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup365 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }

            var prefix = "../../Metadata";
            var instance = new XrmMockup365(Settings, Utility.GetMetadata(prefix), Utility.GetWorkflows(prefix), Utility.GetSecurityRoles(prefix));
            instances[Settings] = instance;
            return instance;
        }
    }
}
