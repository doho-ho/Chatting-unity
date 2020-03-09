using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct dataObject
{
    public byte[] data;
    public System.DateTime time;
};

public class StateObject
{
    public Socket workSocket = null;
    public const int bufferSize = 3072;
    public byte[] Buffer = new byte[bufferSize];
}

public class networkLib : MonoBehaviour {

    public static Queue<dataObject> packetQueue = new Queue<dataObject>();

    private static Socket client;
    private static StateObject state = new StateObject();
    private static IPAddress ipAddress;
    private static IPEndPoint remoteEP;

    [SerializeField]
    protected static bool connectFlag = false;
    [SerializeField]
    protected static bool setFlag = false;
    private static bool onFlag = false;

    public int packetProcessPerSec = 100;

    private static byte Code = 0x77;
    private static byte KeyCode = 0xb6;

    protected static string IP;
    protected static int Port;
    protected static System.UInt64 sessionKey;


    // ManualResetEvent instances signal completion.  
    private static ManualResetEvent connectDone = new ManualResetEvent(false);
    private static ManualResetEvent sendDone = new ManualResetEvent(false);
    private static ManualResetEvent receiveDone = new ManualResetEvent(false);

    public void OnDestroy()
    {
        client.Disconnect(true);
        client.Close();
    }

    public static void setSocket()
    {
        Debug.Log("IP : " + IP + ", Port : " + Port);
        if (Port == 0 || IP == null)
            throw new Exception("Port or IP are zero");
        if (setFlag == true) return;
        setFlag = true;
        ipAddress = IPAddress.Parse(IP);
        remoteEP = new IPEndPoint(ipAddress, Port);
    }

    protected static bool StartClient()
    {
        if (!setFlag || connectFlag)
            return false;
        if (onFlag) return false;
        onFlag = true;
        Debug.Log("Set : " + setFlag + ", Connect : " + connectFlag);
        client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        // Connect to a remote device.  
        try
        {
            // Connect to the remote endpoint.  
            client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            onFlag = false;
            return false;
        }
    }

    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Debug.Log("Connect true");
            connectFlag = true;
            onFlag = false;
            // Retrieve the socket from the state object.  
            client = (Socket)ar.AsyncState;

            // Complete the connection.  
            client.EndConnect(ar);
            // 작업처리
            Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());

            // Signal that the connection has been made.  
            connectDone.Set();

            // Receive the response from the remote device.  
            Recv();
        }
        catch (Exception e)
        {
            connectFlag = false;
            Debug.Log("Connect false");
            Debug.Log(e.ToString());
            Console.WriteLine(e.ToString());
        }
    }

    private static void Recv()
    {
        try
        {
            state.workSocket = client;
            // Begin receiving the data from the remote device.  
            client.BeginReceive(state.Buffer, 0, StateObject.bufferSize, SocketFlags.None,
                new AsyncCallback(RecvCallback), state);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private static void RecvCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;

            // Read data from the remote device.  
            int bytesRead = client.EndReceive(ar);

            if (bytesRead > 0)
            {
                separatePacket(state.Buffer, bytesRead);

                // Get the rest of the data.  
                client.BeginReceive(state.Buffer, 0, StateObject.bufferSize, SocketFlags.None,
                    new AsyncCallback(RecvCallback), state);
            }
            else
            {
                // Signal that all bytes have been received.  
                receiveDone.Set();
            }
        }
        catch (Exception e)
        {
            connectFlag = false;
            Debug.Log("Recv call back " + e.ToString());
            Console.WriteLine(e.ToString());
        }
    }

    private void Send(byte[] data)
    {
        // Convert the string data to byte data using ASCII encoding.  
        //byte[] byteData = Encoding.UTF8.GetBytes(data);

        // Begin sending the data to the remote device.  
        client.BeginSend(data, 0, data.Length, 0,
            new AsyncCallback(SendCallback), client);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.  
            Socket client = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.  
            int bytesSent = client.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to server.", bytesSent);

            // Signal that all bytes have been sent.  
            sendDone.Set();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public bool sendMsg(byte[] _msg)
    {
        if (connectFlag == false) return false;
        byte[] data = Encode(_msg);
        Send(data);
        return true;
    }


    private static byte[] Encode(byte[] msg)
    {

        short len = (short)msg.Length;
        byte[] data = new byte[5 + len];

        byte randCode = (byte)(UnityEngine.Random.Range(0, 32767) % 256);
        byte checkSum = 0;
        int count;
        for (count = 0; count < msg.Length; count++)
        {
            checkSum += msg[count];
            data[(count + 5)] = (byte)(msg[count] ^ randCode);
        }

        checkSum = (byte)((checkSum % 256) ^ randCode);

        data[0] = Code;
        data[1] = (byte)len;
        data[2] = (byte)(len >> 8);
        data[3] = (byte)(randCode ^ KeyCode);
        data[4] = checkSum;

        return data;
    }

    private static void separatePacket(byte[] _data, int _len)
    {
        int dataPos;
        for (dataPos = 0; dataPos != _len;)
        {
            int Len = BitConverter.ToInt16(_data, (dataPos + 1)) + 5;
            byte[] data = new byte[Len];
            Array.Copy(_data, dataPos, data, 0, Len);
            if (Decode(data))
                enqueue(data);
            dataPos += Len;
        }
    }

    private static bool Decode(byte[] _data)
    {
        byte checkSum = 0;
        int len = BitConverter.ToInt16(_data, 1) + 5;
        _data[3] ^= KeyCode;
        _data[4] ^= _data[3];
        int count;
        for (count = 5; count < len; count++)
        {
            _data[count] ^= _data[3];
            checkSum += _data[count];
        }

        checkSum = (byte)(checkSum % 256);


        if (_data[0] != Code)
            return false;
        if (_data[4] != checkSum)
            return false;

        return true;
    }

    public static void enqueue(byte[] _data)
    {
        dataObject node = new dataObject();
        node.data = _data;
        node.time = System.DateTime.Now;

        packetQueue.Enqueue(node);
    }

    public dataObject dequeue()
    {
        return packetQueue.Dequeue();
    }

    public void Net_ConnectoGame(string _ip, short _port)
    {
        Disconnect();
        IP = _ip;
        Port = _port;
        StartClient();
    }

    public void Disconnect()
    {
        // Release the socket.  
        client.Shutdown(SocketShutdown.Both);
        client.Close();
        connectFlag = false;
    }

    public void setSessionKey(System.UInt64 _sessionKey)
    {
        sessionKey = _sessionKey;
    }

    public void GAME_LOGIN()
    {
        //byte[] Type = BitConverter.GetBytes((short)Protocol.game_loginUser_req);
        byte[] sessionKey1 = BitConverter.GetBytes(sessionKey);
        byte[] version = BitConverter.GetBytes((int)0.1);
        byte[] msg = new byte[14];
        //   Array.Copy(Type, 0, msg, 0, Type.Length);
        Array.Copy(sessionKey1, 0, msg, 2, sessionKey1.Length);
        Array.Copy(version, 0, msg, 10, version.Length);
        sendMsg(msg);
    }

}
