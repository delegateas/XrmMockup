#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
#nullable enable
#endif
using System;

namespace XrmMockup.DataverseProxy.Contracts
{
    /// <summary>
    /// Types of requests that can be sent to the proxy.
    /// </summary>
    public enum ProxyRequestType : byte
    {
        Ping = 0,
        Retrieve = 1,
        RetrieveMultiple = 2,
        Shutdown = 3
    }

    /// <summary>
    /// Base request envelope for proxy communication.
    /// </summary>
    public class ProxyRequest
    {
        public ProxyRequestType RequestType { get; set; }

        /// <summary>
        /// JSON-serialized payload for the specific request type.
        /// </summary>
#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
        public string? Payload { get; set; }
#else
        public string Payload { get; set; }
#endif

        /// <summary>
        /// Authentication token. Must match the token passed to the proxy at startup.
        /// </summary>
#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
        public string? AuthToken { get; set; }
#else
        public string AuthToken { get; set; }
#endif
    }

    /// <summary>
    /// Request to retrieve a single entity by ID.
    /// </summary>
    public class ProxyRetrieveRequest
    {
        public string EntityName { get; set; } = string.Empty;
        public Guid Id { get; set; }

        /// <summary>
        /// Column names to retrieve. Null means all columns.
        /// </summary>
#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
        public string[]? Columns { get; set; }
#else
        public string[] Columns { get; set; }
#endif
    }

    /// <summary>
    /// Request to retrieve multiple entities using a QueryExpression.
    /// </summary>
    public class ProxyRetrieveMultipleRequest
    {
        /// <summary>
        /// QueryExpression serialized using DataContractSerializer.
        /// </summary>
        public byte[] SerializedQuery { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Response from the proxy.
    /// </summary>
    public class ProxyResponse
    {
        public bool Success { get; set; }
#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
        public string? ErrorMessage { get; set; }
#else
        public string ErrorMessage { get; set; }
#endif

        /// <summary>
        /// Serialized Entity or EntityCollection using DataContractSerializer.
        /// </summary>
#if DATAVERSE_SERVICE_CLIENT || NETCOREAPP
        public byte[]? SerializedData { get; set; }
#else
        public byte[] SerializedData { get; set; }
#endif
    }
}
