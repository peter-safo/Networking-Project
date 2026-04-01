using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class NetworkGameObject : MonoBehaviour
{
    public bool isLocallyOwned = true;
    public int uniqueNetworkID;
    public int localID;
    static int lastAssignedLocalID = 0;


    void Awake()
    {
        if (isLocallyOwned)
        {
            localID = lastAssignedLocalID++;
            lastAssignedLocalID = localID;
        }
    }

    public byte[] ToPacket()
    {
        // Example: "uniqueNetworkID,positionX,positionY,positionZ,rotationX,rotationY,rotationZ"
        string data = $"PositionRotationPacket,{uniqueNetworkID},{transform.position.x},{transform.position.y},{transform.position.z},{transform.rotation.eulerAngles.x},{transform.rotation.eulerAngles.y},{transform.rotation.eulerAngles.z}";
        Debug.Log(data);
        return Encoding.ASCII.GetBytes(data);
    }

    public void FromPacket(byte[] packet)
    {
        string packetData = Encoding.ASCII.GetString(packet);
        //string[] info = packetData.Split(new char[] { ',' });
        string[] info = packetData.Split(',');

        uniqueNetworkID = int.Parse(info[1]);
        float posX = float.Parse(info[2]);
        float posY = float.Parse(info[3]);
        float posZ = float.Parse(info[4]);
        float rotX = float.Parse(info[5]);
        float rotY = float.Parse(info[6]);
        float rotZ = float.Parse(info[7]);

        transform.position = new Vector3(posX, posY, posZ);
        transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);

    }
}
