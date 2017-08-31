using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Plugin {

    public class PluginImageRegistration {
        public string Name;
        public ImageType ImageType;
        public string EntityAlias;
        public string[] Attributes;

        public PluginImageRegistration(string name, ImageType imageType) {
            this.Name = name;
            this.ImageType = imageType;
        }
    }
}