using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("XrmMockup16Test")]
namespace DG.Tools.XrmMockup {
    /// <summary>
    /// A class for mocking a crm 2016 instance
    /// </summary>
    public class XrmMockup2016 : XrmMockupBase {
        

        private static Dictionary<XrmMockupSettings, XrmMockup2016> instances = new Dictionary<XrmMockupSettings, XrmMockup2016>();


        private XrmMockup2016(XrmMockupSettings Settings, MetadataSkeleton metadata = null, List<Entity> workflows = null, List<SecurityRole> securityRoles = null) :
            base(Settings, metadata, workflows, securityRoles)
        {
        }


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

        /// <summary>
        /// Gets an instance of XrmMockup2016 using the same metadata as the provided
        /// </summary>
        /// <param name="xrmMockup">The existing instance to copy</param>
        /// <param name="settings">
        ///     If provided, will override the settings from the existing instance.<br/>
        ///     <em>NOTE: Changing <see cref="XrmMockupSettings.MetadataDirectoryPath"/> will not trigger a reload</em>
        /// </param>
        public static XrmMockup2016 GetInstance(XrmMockup2016 xrmMockup, XrmMockupSettings settings = null)
        {
            return new XrmMockup2016(settings ?? xrmMockup.Settings, xrmMockup.Metadata, xrmMockup.Workflows, xrmMockup.SecurityRoles);
        } 
     }
}
