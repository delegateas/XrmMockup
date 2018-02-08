﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;

namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 2011 instance
    /// </summary>
    public class XrmMockup2011 : XrmMockupBase {


        private static Dictionary<XrmMockupSettings, XrmMockup2011> instances = new Dictionary<XrmMockupSettings, XrmMockup2011>();


        private XrmMockup2011(XrmMockupSettings Settings) 
            : base(Settings) {
        }
        
        /// <summary>
        /// Gets an instance of XrmMockup2011
        /// </summary>
        /// <param name="Settings"></param>
        public static XrmMockup2011 GetInstance(XrmMockupSettings Settings) {
            if (instances.ContainsKey(Settings)) {
                return instances[Settings];
            }

            var instance = new XrmMockup2011(Settings);
            instances[Settings] = instance;
            return instance;
        }
    }

}
