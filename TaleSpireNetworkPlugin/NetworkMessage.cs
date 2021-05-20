using System;

namespace NetworkPlugin
{
    public class NetworkMessage 
    {
        /// <summary>
        /// Constant Guid string provided to identify mod
        /// </summary>
        public string PackageId;

        /// <summary>
        /// Unique identifier of an author of a message
        /// This allows a server to send their own or re-broadcast a message
        /// </summary>
        public Guid TempAuthorId;

        /// <summary>
        /// Version of the mod
        /// </summary>
        public string Version;

        /// <summary>
        /// Content of the message being sent
        /// </summary>
        public string SerializedMessage;
    }
}