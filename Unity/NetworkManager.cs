using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SocialPlatforms;
//using UnityEngine.tvOS;
using UnityEditor;
using System.Linq;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    static UdpState state;
    static UdpClient client;
    static IPEndPoint ep;
    //public bool isLocallyOwned = true;
    public GameObject networkPlayerPrefab;
    public string receiveString = "";

    public List<NetworkGameObject> networkObjects = new List<NetworkGameObject>();
    public List<NetworkGameObject> worldState;

    public static NetworkManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        networkObjects = new List<NetworkGameObject>();
        worldState = new List<NetworkGameObject>();

        worldState.AddRange(FindObjectsOfType<NetworkGameObject>());
        StartCoroutine(SendNetworkUpdates());
        StartCoroutine(UpdateWorldState());

        string ipAddress = "127.0.0.1";
        IPAddress address = IPAddress.Parse(ipAddress);
        ep = new IPEndPoint(address, 9050);

        client = new UdpClient();
        client.Connect(ep);

        Debug.Log("Client complete");

        string myMessage = "I'm a Unity Client - Hi! :)";
        byte[] array = Encoding.ASCII.GetBytes(myMessage);
        client.Send(array, array.Length);

        client.BeginReceive(ReceiveAsyncCallback, state);
        networkObjects.AddRange(FindObjectsOfType<NetworkGameObject>());

        RequestUIDs();
    }

    // Method to send chat message
    public void SendChatMessage(string message)
    {
        if (client != null && ep != null)
        {
            // Convert message to bytes and send asynchronously
            byte[] data = Encoding.ASCII.GetBytes(message);
            client.BeginSend(data, data.Length, ep, SendCallback, client);
        }
    }

    void SendCallback(IAsyncResult result)
    {
        client.EndSend(result);
    }


    //Method to receive chat message
    void ReceiveChatMessage(IAsyncResult result)
    {
        // Receive message from the server
        IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] receivedData = client.EndReceive(result, ref remoteEndpoint);
        string message = Encoding.ASCII.GetString(receivedData);

        // Handle received message
        // For example, pass it to the lobby chat UI
        LobbyChat lobbyChat = FindObjectOfType<LobbyChat>();
        if (lobbyChat != null)
        {
            lobbyChat.ReceiveMessage("Server", message);
        }

        // Continue receiving messages asynchronously
        client.BeginReceive(ReceiveChatMessage, null);
    }

    void ReceiveAsyncCallback(IAsyncResult result)
    {
        byte[] receiveBytes = client.EndReceive(result, ref ep);
        receiveString = Encoding.ASCII.GetString(receiveBytes);
        Debug.Log("Received " + receiveString + " from " + ep.ToString());

        if (receiveString.Contains("UID"))
        {
            string[] parts = receiveString.Split(new char[] { ':', ';' });
            Debug.Log("Parts amount: " + parts.Length);
            foreach (string part in parts)
            {
                Debug.Log("Part: " + part);
            }

            int.TryParse(parts[1], out int localID);
            int.TryParse(parts[3], out int globalID);

            foreach (NetworkGameObject networkGameObject in networkObjects)
            {
                if (localID == networkGameObject.localID)
                {
                    networkGameObject.uniqueNetworkID = globalID;
                    Debug.Log("Assigned uniqueNetworkID: " + globalID + " to localID: " + localID);
                }

            }
        }

        LobbyChat lobbyChat = FindObjectOfType<LobbyChat>();
        if (lobbyChat != null)
        {
            lobbyChat.ReceiveMessage("Server", receiveString);
        }

        client.BeginReceive(ReceiveAsyncCallback, state);
    }


    IEnumerator UpdateWorldState()
    {
        string tempString;

        while (true)
        {
            tempString = receiveString;

            if (tempString.Contains("PositionRotationPacket"))
            {
                bool isFound = false;
                string[] parts = tempString.Split(',');

                Debug.Log(parts.Length);

                string data = parts[1];
                byte[] dataBytes = Encoding.ASCII.GetBytes(tempString);
                int.TryParse(parts[1], out int UID);
                Debug.Log(parts[1]);

                for (int i = 0; i < worldState.Count; i++)
                {
                    if (worldState[i].uniqueNetworkID == UID)
                    {
                        isFound = true;

                        if (!worldState[i].isLocallyOwned)
                        {
                            worldState[i].FromPacket(dataBytes);
                        }
                    }
                }

                if (!isFound)
                {
                    GameObject newPlayerObject = Instantiate(networkPlayerPrefab);
                    NetworkGameObject newPlayer = newPlayerObject.GetComponent<NetworkGameObject>();
                    newPlayer.uniqueNetworkID = UID;
                    worldState.Add(newPlayer);
                    newPlayer.FromPacket(dataBytes); 
                }

                else
                {
                    Debug.LogError("Invalid PositionRotationPacket format");
                }
            }

            yield return null;
            yield return new WaitUntil(() => receiveString != tempString);
        }
    }

    public struct UdpState
    {
        public UdpClient u;
        public IPEndPoint e;
    }

    void RequestUIDs()
    {
        //networkObjects.AddRange(FindObjectsOfType<NetworkGameObject>());

        for (int i = 0; i < networkObjects.Count; i++)
        {
            if (networkObjects[i].uniqueNetworkID == 0 && networkObjects[i].isLocallyOwned == true)
            {
                
                string UIDMessage = "unique network ID: " + networkObjects[i].localID;
                byte[] byteMessage = Encoding.ASCII.GetBytes(UIDMessage);
                client.Send(byteMessage, byteMessage.Length);
                Debug.Log(UIDMessage);
            }
        }
    }

    IEnumerator SendNetworkUpdates()
    {
        while (true)
        {
            List<NetworkGameObject> localNetworkObjects = new List<NetworkGameObject>(networkObjects);

            foreach (NetworkGameObject networkGameObject in localNetworkObjects)
            {
                if (networkGameObject.isLocallyOwned && networkGameObject.uniqueNetworkID != 0)
                {
                    byte[] packet = networkGameObject.ToPacket();
                    client.Send(packet, packet.Length);
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}