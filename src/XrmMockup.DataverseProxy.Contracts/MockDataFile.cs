#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
#nullable enable
#endif
using System.Collections.Generic;

namespace XrmMockup.DataverseProxy.Contracts
{
    /// <summary>
    /// JSON schema for mock data file used in testing.
    /// </summary>
    internal class MockDataFile
    {
        /// <summary>
        /// List of serialized entities to use as mock data.
        /// Each entity is serialized using DataContractSerializer as a byte array.
        /// </summary>
        public List<byte[]> Entities { get; set; } = new List<byte[]>();
    }
}
