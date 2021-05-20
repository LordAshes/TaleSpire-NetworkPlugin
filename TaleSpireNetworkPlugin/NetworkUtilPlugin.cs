using System;
using BepInEx;
using System.Collections.Generic;
using System.Net.Sockets;
using Newtonsoft.Json;
using UnityEngine;
using System.Collections.Concurrent;

namespace NetworkPlugin
{
    [BepInPlugin(Guid, "Network Plug-In", Version)]
    public class NetworkUtilPlugin : BaseUnityPlugin
    {
        // Constants
        public const string Guid = "org.LordAshes.HolloFox.plugins.Network";
        private const string Version = "1.1.0.0";

        // Dictionary for callbacks
        private static readonly Dictionary<string, Action<Socket, NetworkMessage>> _serverCallbacks 
            = new Dictionary<string, Action<Socket, NetworkMessage>>();
        private static readonly Dictionary<string, Action<Socket, NetworkMessage>> _clientCallbacks
            = new Dictionary<string, Action<Socket, NetworkMessage>>();

        // Queue of all messages to be sent or broadcast
        private static readonly ConcurrentQueue<NetworkMessage> _serverMessages = new ConcurrentQueue<NetworkMessage>();
        private static readonly ConcurrentQueue<NetworkMessage> _clientMessages = new ConcurrentQueue<NetworkMessage>();

        // Content directory
        private static string dir = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Chat handler
        private static Network.Server server = new Network.Server();
        private static Network.Client client = new Network.Client();

        // Need to generate an AuthorId 
        private static Guid AuthorId = System.Guid.NewGuid();

        private List<string> last = new List<string>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("Network Plugin Active.");
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            // Ensure that there is a camera controller instance
            if (CameraController.HasInstance)
            {
                // Ensure that there is a board session manager instance
                if (BoardSessionManager.HasInstance)
                {
                    // Ensure that there is a board
                    if (BoardSessionManager.HasBoardAndIsInNominalState)
                    {
                        // Ensure that the board is not loading
                        if (!BoardSessionManager.IsLoading)
                        {
                            // Check for user input to determine if the Sync Mod Server status should be toggled
                            server.StartOn(KeyCode.Y, ClientRequests);

                            // Check to see if the Sync Mod Server has sent a notification and connect to the Sync Mod Server if one is sent
                            client.CheckForServerNotification(ServerRequests);

                            // Broadcasting all server messages
                            if (server.isRunning())
                            {
                                while (_serverMessages.TryDequeue(out var message))
                                {
                                    Debug.Log("Sending Server Message!");
                                    var serialized = JsonConvert.SerializeObject(message);
                                    server.Broadcast(serialized);
                                }
                            }

                            // Sending all messages from client to server
                            if (client.isConnected()) {
                                while (_clientMessages.TryDequeue(out var message)) {
                                    UnityEngine.Debug.Log("Sending Client Message!");
                                    var serialized = JsonConvert.SerializeObject(message);
                                    client.Send(serialized);
                                }
                            }
                            
                            // Dump log
                            if (Input.GetKeyDown(KeyCode.X))
                            {
                                foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)))
                                {
                                    UnityEngine.Debug.Log(obj.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to handle messages sent from the server to the client
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="request">String request</param>
        private void ServerRequests(Socket client, string request)
        {
            var messageRequest = JsonConvert.DeserializeObject<NetworkMessage>(request);
            var callback = _serverCallbacks[messageRequest.PackageId];
            callback(client, messageRequest);
        }

        /// <summary>
        /// Method to handle messages sent from the client to the server
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="request">String request</param>
        private void ClientRequests(Socket client, string request)
        {
            var messageRequest = JsonConvert.DeserializeObject<NetworkMessage>(request);
            var callback = _clientCallbacks[messageRequest.PackageId];
            callback(client, messageRequest);
        }

        /// <summary>
        /// Adds a call back for specific mod for Method to handle messages sent from the client to the server
        /// </summary>
        /// <param name="key">Guid of the mod that is using the callback.</param>
        /// <param name="callback">the method that is called back upon message being received.</param>
        /// <returns>success if the callback is stored</returns>
        public static bool AddClientCallback(string key, Action<Socket, NetworkMessage> callback)
        {
            if (_clientCallbacks.ContainsKey(key)) return false;
            _clientCallbacks.Add(key,callback);
            return true;
        }

        /// <summary>
        /// Adds a call back for specific mod for Method to handle messages sent from the server to the client
        /// </summary>
        /// <param name="key">Guid of the mod that is using the callback.</param>
        /// <param name="callback">the method that is called back upon message being received.</param>
        /// <returns>success if the callback is stored</returns>
        public static bool AddServerCallback(string key, Action<Socket, NetworkMessage> callback)
        {
            if (_serverCallbacks.ContainsKey(key)) return false;
            _serverCallbacks.Add(key, callback);
            return true;
        }

        /// <summary>
        /// Queues a message to be sent to the server from the client
        /// </summary>
        /// <param name="message">message to be sent</param>
        public static void ClientSendMessage(NetworkMessage message)
        {
            _clientMessages.Enqueue(message);
        }

        /// <summary>
        /// Queues a message to be broadcast from the server to the clients
        /// </summary>
        /// <param name="message">message to broadcast</param>
        public static void ServerSendMessage(NetworkMessage message)
        {
            _serverMessages.Enqueue(message);
        }

        /// <summary>
        /// Allows a mod to determine if they're hosting or client
        /// </summary>
        /// <returns>true or false value if they're hosting.</returns>
        public static bool IsHost()
        {
            return server.isRunning();
        }

        /// <summary>
        /// Allows a mod to determine if they're hosting or client
        /// </summary>
        /// <returns>true or false value if they're hosting.</returns>
        public static bool IsClient()
        {
            return client.isConnected();
        }

        /// <summary>
        /// Allows a mod to obtain a consistent GUID to determine player id.
        /// </summary>
        /// <returns>Guid for the player.</returns>
        public static Guid GetAuthorId()
        {
            return AuthorId;
        }
    }
}
