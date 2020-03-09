using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NetworkManager : networkLib {

    protected static NetworkManager s_Instance;

    public chatManager Chat;
    public startSceneManager Start;
    public sceneManager Scene;

    private Queue<byte[]> waitQ = new Queue<byte[]>();

    public static NetworkManager Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;

            s_Instance = FindObjectOfType<NetworkManager>();

            if (s_Instance != null)
                return s_Instance;

            NetworkManager NetMng = Resources.Load<NetworkManager>("NetworkManager");
            s_Instance = Instantiate(NetMng);

            return s_Instance;
        }
    }

    public enum nowStatus
    {
        Start = 0, Lobby, Game,
    }


    void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        Scene = sceneManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        int Count = 0;
        int maxPacketProcessCount = Instance.packetProcessPerSec;
        for (Count = 0; Count < maxPacketProcessCount; Count++)
        {
            float nowTime = Time.time;
            if (packetQueue.Count == 0)
                break;
            dataObject data = dequeue();
            completeRecv(data.data);
        }
        Count = 0;
        while(waitQ.Count != 0)
        {
            if (Count < maxPacketProcessCount) break;
            byte[] Data = waitQ.Dequeue();
            completeRecv(Data);
        }
    }

    void loginFail()
    {
        StopCoroutine(checkConnect());
        Scene.proc_loginFail();
    }

    private void completeRecv(byte[] _Data)
    {
        short Type = BitConverter.ToInt16(_Data, 5);
        switch (Type)
        {
            case (short)chatProtocol.Protocol.s2c_Login_Res:
                {
                    if (!Start)
                        waitQ.Enqueue(_Data);
                    else
                        Start.recv_Login(_Data);
                    break;
                }
            case (short)chatProtocol.Protocol.s2c_playerData_Res:
                Chat.recv_playerData(_Data);
                break;
            case (short)chatProtocol.Protocol.s2c_createPlayer:
                Chat.recv_createPlayer(_Data);
                break;
            case (short)chatProtocol.Protocol.s2c_deletePlayer:
                Chat.recv_deletePlayer(_Data);
                break;
            case (short)chatProtocol.Protocol.s2c_playerMove:
                Chat.recv_Move(_Data);
                break;
            case (short)chatProtocol.Protocol.s2c_playerCHChange:
                Chat.recv_chMove(_Data);
                break;
            case (short)chatProtocol.Protocol.s2c_Chatting:
                Chat.recv_Chatting(_Data);
                break;
            default:
                break;
        }
    }

    public bool callConnectFunction(string _ip, int _port)
    {
        IP = _ip;
        Port = _port;
        setSocket();
        return StartClient();
    }

    public IEnumerator checkConnect()
    {
        yield return new WaitUntil(() => connectFlag);
        Debug.Log("Connect");
        Start.Login();
    }

    public void setChatMng(chatManager _Chat)
    {
        Chat = _Chat;
    }

    public void setStart(startSceneManager _Start)
    {
        Start = _Start;
    }
}
