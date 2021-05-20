using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Network
{
    public class Server
    {
        /// <summary>
        /// Dictionary to hold client connections
        /// </summary>
        private Dictionary<EndPoint, Socket> connections = new Dictionary<EndPoint, Socket>();

        /// <summary>
        /// Timer for periodic checking of connections (to remove old connections)
        /// </summary>
        private System.Timers.Timer pulse = null;

        /// <summary>
        /// Holds the running status of the server
        /// </summary>
        private bool running = false;

        /// <summary>
        /// Method for starting the Sync Mod Server
        /// </summary>
        /// <param name="callback">Callback function that receives any messages sent to the Sync Mod Server</param>
        /// <param name="port">Port on which the Sync Mod Server listens</param>
        public void Start(Action<Socket, string> callback, int port = 11000)
        {
            if (running) { return; }
            running = true;

            // Run server as a task so that it does not block execution of remaining code
            Task.Run(() =>
            {
                // Start the periodic connection checking function
                pulse = new System.Timers.Timer(30000);
                pulse.Elapsed += SessionCheck;
                pulse.Start();

                // Connect on any IP
                IPAddress ipAddress = IPAddress.Any;
                IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

                try
                {
                    UnityEngine.Debug.Log("Starting Sync Mod Server...");
                    Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    listener.Bind(localEndPoint);
                    listener.Listen(1);

                    // Look waiting for connections to allow multiple simultaneous connections
                    while (true)
                    {
                        // Pass the client on to a handlers as a task so that handling the client does not block code execution
                        UnityEngine.Debug.Log("Waiting For Connections...");
                        Socket client = listener.Accept();
                        connections.Add(client.RemoteEndPoint, client);
                        Task.Run(() => { new SessionHandler(client, callback, ref connections); });

                        UnityEngine.Debug.Log("Connections Accepted From "+client.RemoteEndPoint.ToString()+"...");
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.Log(e.ToString());
                }
            });
        }

        /// <summary>
        /// Method for stopping the Sync Mod Server
        /// </summary>
        public void Stop()
        {
            if (!running) { return; }
            running = false;
            UnityEngine.Debug.Log("Stopping Sync Mod Server...");
            pulse.Stop();
            // Disconnect all clients
            UnityEngine.Debug.Log("Disconnecting Clients...");
            foreach (Socket client in connections.Values)
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception) {; }
            }
        }

        /// <summary>
        /// Method to broadcast a message to all connected clients.
        /// Messages need to end in a \r in order to be considered complete but the method will automatically do that unless autoEndLine is set to false.
        /// </summary>
        /// <param name="message">String representation of the message</param>
        /// <param name="autoEndLine">Automatically adds a trailing \r to messages if not present</param>
        public void Broadcast(string message, bool autoEndLine = true)
        {
            UnityEngine.Debug.Log("Sync Mod Server Broadcasts '"+message+"'");
            if (!message.EndsWith("\r") && autoEndLine == true) { message = message + "\r"; }
            foreach (Socket client in connections.Values)
            {
                client.Send(Encoding.ASCII.GetBytes(message));
            }
        }

        /// <summary>
        /// Method to return the server running state
        /// </summary>
        /// <returns></returns>
        public bool isRunning()
        {
            return running;
        }

        /// <summary>
        /// Method to send out a Sync Mod Server notification and start the Sync Mod Server when user presses the indicated key
        /// or stops the Sync Mod Server if the server is running and the indicated key is pressed
        /// </summary>
        /// <param name="binding">KeyCode for the key that triggers this functionality</param>
        /// <param name="callback">Callback for client received messages</param>
        public void StartOn(KeyCode binding,Action<Socket,string> callback)
        {
            if (Input.GetKeyDown(binding))
            {
                if (!this.isRunning())
                {
                    SystemMessage.DisplayInfoText("Starting Sync Mod Server");
                    this.Start(callback);

                    // Deetrmine external IP address
                    string url = "http://checkip.dyndns.org";
                    System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                    System.Net.WebResponse resp = req.GetResponse();
                    System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                    string response = sr.ReadToEnd().Trim();
                    string[] a = response.Split(':');
                    string a2 = a[1].Substring(1);
                    string[] a3 = a2.Split('<');
                    string a4 = a3[0];

                    // Send Sync Mod Server notification to all clients via chat
                    UnityEngine.Debug.Log("Sync Mod Server At " + a4);
                    CreaturePresenter.AllCreatureAssets.ToArray()[0].Creature.Speak("> " + a4);
                }
                else
                {
                    SystemMessage.DisplayInfoText("Stopping Sync Mod Server");
                    this.Stop();
                }
            }
        }

        /// <summary>
        /// Method to periodically check for old connection. The method does a poll of all connections and disconnects any that don't reply.
        /// This is necessary because in some odd cases a client can disconnect without triggering the disconnect client code.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SessionCheck(object sender, System.Timers.ElapsedEventArgs e)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (!connections.ElementAt(i).Value.Poll(1000, SelectMode.SelectRead))
                {
                    UnityEngine.Debug.Log("Client Did Not Respond. Removing Client...");
                    try
                    {
                        connections.Remove(connections.ElementAt(i).Key);
                        i--;
                    }
                    catch (Exception) {; }
                }
            }
        }
    }

    public class Client
    {
        /// <summary>
        /// Holds the client Socket
        /// </summary>
        private Socket client = null;

        /// <summary>
        /// Holds active notifications to prevent re-processing
        /// </summary>
        private List<string> last = new List<string>();

        /// <summary>
        /// Method to connect a client to the Sync Mod Server
        /// </summary>
        /// <param name="ip">IP address to connect to</param>
        /// <param name="callback">Callback function that receives any messages sent from the server</param>
        public void Connect(string ip, Action<Socket, string> callback)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);
            IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, 11000);
            client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(remoteEndPoint);
            Task.Run(() =>
            {
                Dictionary<EndPoint, Socket> dump = new Dictionary<EndPoint, Socket>();
                new SessionHandler(client, callback, ref dump);
            });
        }

        /// <summary>
        /// Method to send messages to the Sync Mod Server
        /// Messages need to end in a \r in order to be considered complete but the method will automatically do that unless autoEndLine is set to false.
        /// </summary>
        /// <param name="message">String representation of the message</param>
        /// <param name="autoEndLine"></param>
        public void Send(string message, bool autoEndLine = true)
        {
            if (!message.EndsWith("\r") && autoEndLine == true) { message = message + "\r"; }
            client.Send(Encoding.ASCII.GetBytes(message));
        }

        /// <summary>
        /// Method to get connected state
        /// </summary>
        /// <returns></returns>
        public bool isConnected()
        {
            try
            {
                return (client != null);
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects the client from the Sync Mod Server
        /// </summary>
        public void Disconnect()
        {
            client.Close();
            client.Dispose();
        }

        /// <summary>
        /// Checks for Sync Mod Server notifications and connects client if one is received
        /// </summary>
        /// <param name="client">Network client object</param>
        /// <param name="ServerRequests">Callback for processing server messages</param>
        public void CheckForServerNotification(Action<Socket, string> ServerRequests)
        {
            List<string> current = new List<string>();

            // Look for Sync Mod Sever notifications and connect client to it if one is found
            TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
            for (int i = 0; i < texts.Length; i++)
            {
                // Look at create speech text boxes for Sync Mod Sever notification
                if ((texts[i].name == "Text") && (texts[i].font.name == "NAL Hand SDF") && (texts[i].text.Trim().Contains("> ")))
                {
                    // Prevent re-processing the notification
                    current.Add(texts[i].text);
                    // If notification has not been processed
                    if (!last.Contains(texts[i].text))
                    {
                        // GM Server sent notification. Connect client connection to GM server
                        if (this.isConnected()) { this.Disconnect(); }
                        UnityEngine.Debug.Log("Client Connecting To Sync Mod Server At " + texts[i].text.Substring(2));
                        this.Connect(texts[i].text.Substring(2), ServerRequests);
                    }
                }
            }
            last = current;
        }
    }

    public class SessionHandler
    {
        /// <summary>
        /// Method for handling TCP/IP communication. Used for both Server and Client
        /// </summary>
        /// <param name="client">Client socket</param>
        /// <param name="callback">Callback function which gets the messages received</param>
        /// <param name="connections">Dictionary of connections (server handlers) or empty dictionary (for client handlers)</param>
        public SessionHandler(Socket client, Action<Socket, string> callback, ref Dictionary<EndPoint, Socket> connections)
        {
            string data = null;
            byte[] bytes = null;

            UnityEngine.Debug.Log("Session Handler Started");

            while (true)
            {
                try
                {
                    bytes = new byte[1024];
                    int bytesRec = client.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("\r") > -1)
                    {
                        string request = data.Substring(0, data.IndexOf("\r"));
                        data = data.Substring(data.IndexOf("\r") + 1).Replace("\n", "");
                        if (request != "")
                        {
                            callback(client, request);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }

            UnityEngine.Debug.Log("Client Disconnected...");
            try { if (connections.ContainsKey(client.RemoteEndPoint)) { connections.Remove(client.RemoteEndPoint); } } catch (Exception) {; }
            try { client.Shutdown(SocketShutdown.Both); } catch (Exception) {; }
            try { client.Close(); } catch (Exception) {; }
        }
    }
}
