﻿using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DG.Tools.XrmMockup
{
    /// <summary>
    /// Settings for XrmMockup instance
    /// </summary>
    public class XrmMockupSettings {
        /// <summary>
        /// List of base-types which all your plugins extend.
        /// This is used to locate the assemblies required.
        /// </summary>
        public IEnumerable<Type> BasePluginTypes { get; set; }
        /// <summary>
        /// List of at least one instance of a CodeActivity in each of your projects that contain CodeActivities. 
        /// This is used to locate the assemblies required to find all CodeActivity.
        /// </summary>
        public IEnumerable<Type> CodeActivityInstanceTypes { get; set; }
        /// <summary>
        /// Enable early-bound proxy types.
        /// </summary>
        public bool? EnableProxyTypes { get; set; }
        /// <summary>
        /// Sets whether all workflow definitions should be included on startup. Default is true.
        /// </summary>
        public bool? IncludeAllWorkflows { get; set; }
        /// <summary>
        /// List of request that will not throw exceptions.
        /// </summary>
        public IEnumerable<string> ExceptionFreeRequests { get; set; }
        /// <summary>
        /// Environment settings for connection to an online environment for live debugging.
        /// </summary>
        public Env? OnlineEnvironment { get; set; }
        /// <summary>
        /// Overwrites the path to the directory containing metadata files. Default is '../../Metadata/'.
        /// </summary>
        public string MetadataDirectoryPath { get; set; }
    }

    public struct Env {
        public string uri;
        public AuthenticationProviderType providerType;
        public string username;
        public string password;
        public string domain;
    }
}
