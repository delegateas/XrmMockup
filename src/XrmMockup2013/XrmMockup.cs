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
    /// A class for mocking a crm 2013 instance
    /// </summary>
    public class XrmMockup2013 : XrmMockupBase {

        private static Dictionary<XrmMockupSettings, XrmMockup2013> instances = new Dictionary<XrmMockupSettings, XrmMockup2013>();


        private XrmMockup2013(XrmMockupSettings Settings, MetadataSkeleton Metadata, List<Entity> Workflows, List<SecurityRole> SecurityRoles) : 
            base(Settings, Metadata, Workflows, SecurityRoles) {
        }
        
        /// <summary>
        /// Gets an instance of XrmMockup2013
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup2013 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }

            var prefix = "../../Metadata";
            var instance = new XrmMockup2013(Settings, Utility.GetMetadata(prefix), Utility.GetWorkflows(prefix), Utility.GetSecurityRoles(prefix));
            instances[Settings] = instance;
            return instance;
        }
    }
}
