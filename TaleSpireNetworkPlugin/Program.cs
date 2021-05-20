using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

namespace NetworkPlugin
{
    [BepInPlugin("org.d20armyknife.plugins.Network", "Network Plug-In", "1.0.21.0")]
    public class CustomMiniPlugin : BaseUnityPlugin
    {
        // Content directory
        private static string dir = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.LastIndexOf("/")) + "/TaleSpire_CustomData/";

        // Chat handelr
        private Network.Server server = new Network.Server();
        private Network.Client client = new Network.Client();

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

                            if (Input.GetKeyDown(KeyCode.U))
                            {
                                UnityEngine.Debug.Log("Sending Server Message!");
                                if (server.isRunning())
                                {
                                    server.Broadcast("Bow To Your Master!");
                                }
                            }

                            if (Input.GetKeyDown(KeyCode.I))
                            {
                                UnityEngine.Debug.Log("Sending Client Message!");
                                if (client.isConnected())
                                {
                                    client.Send("We Bow In Your Presence, Master!");
                                }
                            }

                            if (Input.GetKeyDown(KeyCode.X))
                            {
                                foreach(UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)))
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
            //
            // To Do: Do something with the request
            //
            SystemMessage.DisplayInfoText("Server Requested:\r\n"+request);
        }

        /// <summary>
        /// Method to handle messages sent from the client to the server
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="request">String request</param>
        private void ClientRequests(Socket client, string request)
        {
            //
            // To Do: Do something with the request
            //
            SystemMessage.DisplayInfoText("Client\r\n"+client.RemoteEndPoint.ToString()+"\r\nRequested:\r\n" + request);
        }
    }
}
