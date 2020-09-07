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

[assembly: InternalsVisibleTo("XrmMockup15Test")]
namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 2015 instance
    /// </summary>
    public class XrmMockup2015 : XrmMockupBase {

        private static Dictionary<XrmMockupSettings, XrmMockup2015> instances = new Dictionary<XrmMockupSettings, XrmMockup2015>();


        private XrmMockup2015(XrmMockupSettings Settings) : 
            base(Settings) {
        }
        
        /// <summary>
        /// Gets an instance of XrmMockup2015
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup2015 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }

            var instance = new XrmMockup2015(Settings);
            instances[Settings] = instance;
            return instance;
        }
    }
}
