using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmMockupConfig
{

    // ImageTuple           : Name, EntityAlias, ImageType, Attributes
    //using StepConfig = System.Tuple<string, int, string, string>;
    //using ExtendedStepConfig = System.Tuple<int, int, string, int, string, string>;
//    using ImageConfig = System.Tuple<string, string, int, string>;

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
