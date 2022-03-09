using System.Collections.Generic;

namespace DG.Tools.XrmMockup.Serialization
{
    public class DbDTO
    {
        public Dictionary<string, TableDTO> Tables { get; set; }
    }
}
