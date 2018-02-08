using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DG.Tools;
using Microsoft.Xrm.Sdk.Metadata;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;


namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 2016 instance
    /// </summary>
    public class XrmMockup2016 : XrmMockupBase {
        

        private static Dictionary<XrmMockupSettings, XrmMockup2016> instances = new Dictionary<XrmMockupSettings, XrmMockup2016>();


        private XrmMockup2016(XrmMockupSettings Settings) : 
            base(Settings) {}

        
        /// <summary>
        /// Gets an instance of XrmMockup2016
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup2016 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }
            var instance = new XrmMockup2016(Settings);
            instances[Settings] = instance;
            return instance;
        }
    }
}
