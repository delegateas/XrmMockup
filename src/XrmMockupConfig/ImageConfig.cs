using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DG.Tools.XrmMockup.Config
{
    public class ImageConfig
    {
        public ImageConfig(string name, string entityAlias, int imageType, string attributes)
        {
            Name = name;
            EntityAlias = entityAlias;
            ImageType = imageType;
            Attributes = attributes;
        }

        public string Name { get; set; }
        public string EntityAlias { get; set; }
        public int ImageType { get; set; }
        public string Attributes { get; set; }
    }
}
