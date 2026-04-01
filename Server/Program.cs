using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Mail;
using System.Diagnostics;
using System.Xml;


namespace ConsoleApp1
{
    internal class Program
    {
        static int lastAssignedGlobalID = 100;
        static Dictionary<int, byte[]> gameState = new Dictionary<int, byte[]>();  // To store game state
        static List<IPEndPoint> connectedClients = new List<IPEndPoint>();  // To store connected clients
        static Socket newsock;

        static Dictionary<string, int> requestCounts = new Dictionary<string, int>();
        static int maxRequestsPerMinute = 100;

        static void Main(string[] args)
        {
            SetupServer();

            Thread resetThread = new Thread(ResetRequestCounts);
            resetThread.IsBackground = true;
            resetThread.Start();

            // Main loop to handle client connections, game state updates, and server commands
            while (true)
            {
                try
                {
                    HandleClientConnections();
                    SendGameStateToClients();
                    HandleServerCommands();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
        }

        // Method to initialize and bind the server socket
        static void SetupServer()
        {
            // Define port number and endpoint for the server
            int port = 9050;
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

            // Create a new socket for UDP communication and bind it to the endpoint
            newsock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            newsock.Bind(ipep);

            // Display a message indicating that the socket is open and ready to receive connections
            Console.WriteLine("Socket open");
        }

        // Method to handle incoming client connections
        static void HandleClientConnections()
        {
            // Variables for receiving data from clients
            int recv;
            byte[] data = new byte[1024];

            // Define an endpoint for the remote client
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            // Receive data from a remote client and convert it to a string
            recv = newsock.ReceiveFrom(data, ref remote);
            string received = Encoding.ASCII.GetString(data, 0, recv);

            // Convert the remote endpoint to a string representation
            string remoteString = remote.ToString();

            // Check if the client is already connected
            bool isClientConnected = connectedClients.Any(client => client.ToString() == remoteString);

            /* Implementing rate limiting */
            // Identify the client
            string clientIdentifier = GetClientIdentifier(remote);

            // Check if the client has exceeded the request limit
            if (IsRateLimited(clientIdentifier))
            {
                Console.WriteLine($"Rate limit exceeded for client: {clientIdentifier}");
                return;
            }

            // If client is not already connected, add to the list of connected clients
            if (!isClientConnected)
            {
                connectedClients.Add((IPEndPoint)remote);
                Console.WriteLine("New client connected: " + remoteString);
                Console.WriteLine("Number of connected clients: " + connectedClients.Count);
            }

            //}
            if (received.Contains("unique network ID"))
            {
                // Assign a unique ID to a game object
                //string substring = received.Substring(received.IndexOf(":") + 2);
                
                string[] substring = received.Split(':');
                //Console.WriteLine("Object UID: " + substring[1]);

                //int subint = int.Parse(substring[1]);
                int.TryParse(substring[1], out int subint);
                Console.WriteLine("Object UID: " + subint);
                string response = "Assigned UID: " + subint + "; Global ID: " + lastAssignedGlobalID;
                Console.WriteLine(response);
                lastAssignedGlobalID++;
                byte[] array = Encoding.ASCII.GetBytes(response);
                newsock.SendTo(array, array.Length, SocketFlags.None, remote);
            }
            if (received.Contains("PositionRotationPacket"))
            {
                // Handle a packet containing position and rotation data of a game object
                string[] parts = received.Split(",");

                int globalID;
                if (int.TryParse(parts[1], out globalID))
                {
                    if (gameState.ContainsKey(globalID))
                    {
                        gameState[globalID] = Encoding.ASCII.GetBytes(received);

                    }
                    else
                    {
                        gameState.Add(globalID, Encoding.ASCII.GetBytes(received));

                    }
                    //Console.WriteLine("GlobalID: " + parts[1] + "; " +
                    //    " Position: " + parts[2] + ", " + parts[3] + ", " + parts[4] + "; " +
                    //    " Rotation: " + parts[5] + ", " + parts[6] + ", " + parts[7]);
                }
            }

            if (received.Contains("ChatMessage:"))
            {
                // Extract chat message from received data
                string[] parts = received.Split(":");
                string chatMessage = parts[1];

                // Broadcast chat message to all connected clients
                foreach (var clientEndpoint in connectedClients)
                {
                    byte[] messageBytes = Encoding.ASCII.GetBytes(chatMessage);
                    newsock.SendTo(messageBytes, messageBytes.Length, SocketFlags.None, clientEndpoint);
                }
            }

            IncrementRequestCount(clientIdentifier);
        }

        // Method to handle server commands entered via the console
        static void HandleServerCommands()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                Console.WriteLine();

                switch (key.Key) 
                {
                    case ConsoleKey.C:
                        ShowConnectedClients();
                        break;
                    case ConsoleKey.S:
                        ShowGameState();
                        break;

                }
            }
        }

        // Method to display connected clients
        static void ShowConnectedClients()
        {
            Console.WriteLine("Connected Clients:");
            foreach (var clientEndPoint in connectedClients)
            {
                Console.WriteLine(clientEndPoint.ToString());
            }
        }

        // Method to display current game state
        static void ShowGameState()
        {
            Console.WriteLine("Game State:");
            foreach (var gameData in gameState)
            {
                Console.WriteLine($"GlobalID: {gameData.Key}, Data: {Encoding.ASCII.GetString(gameData.Value)}");
            }
        }

        // Method to send game state to connected clients
        static void SendGameStateToClients()
        {

            foreach (var clientEndPoint in connectedClients)
            {
                foreach (var gameData in gameState)
                {
                    byte[] gameDataBytes = gameData.Value;
                    newsock.SendTo(gameDataBytes, gameDataBytes.Length, SocketFlags.None, clientEndPoint);
                }
            }
        }

        static string GetClientIdentifier(EndPoint clientEndPoint)
        {
            if (clientEndPoint is IPEndPoint)
            {
                IPEndPoint iPEndPoint = (IPEndPoint)clientEndPoint;
                return iPEndPoint.Address.ToString();
            }
            else
            {
                return "UnknownClient";
            }
        }

        static bool IsRateLimited(string clientIdentifier)
        {
            lock (requestCounts)
            {
                // Check if the client identifier exists in the dictionary
                if (requestCounts.ContainsKey(clientIdentifier))
                {
                    // Get the request count for the client
                    int count = requestCounts[clientIdentifier];

                    // Check if the request count exceeds the limit
                    return count >= maxRequestsPerMinute;
                }
                else
                {
                    return false;
                }
            }
        }

        static void IncrementRequestCount(string clientIdentifier)
        {
            lock (requestCounts)
            {
                // Increment the request count for the client
                if (requestCounts.ContainsKey(clientIdentifier))
                {
                    requestCounts[clientIdentifier]++;
                }
                else
                {
                    requestCounts[clientIdentifier] = 1;
                }
            }
        }

        static void ResetRequestCounts()
        {
            // Periodically reset request counts to prevent accumulation over time
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMinutes(1)); // Reset counts every minute

                lock (requestCounts)
                {
                    requestCounts.Clear(); // Clear all request counts
                }
            }
        }
    }
}

