using System.Collections.Generic;
using System.Text.Json;

namespace DG.Tools.XrmMockup.Serialization
{
    public class EntityCollectionDTO
    {
        public JsonElement Entities { get; set; }
        public bool MoreRecords { get; set; }
        public string PagingCookie { get; set; }
        public string MinActiveRowVersion { get; set; }
        public int TotalRecordCount { get; set; }
        public bool TotalRecordCountLimitExceeded { get; set; }
        public string EntityName { get; set; }
    }
}
