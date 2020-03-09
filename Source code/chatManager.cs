using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System;
using TMPro;


public class Player
{
    private System.UInt64 playerCode;
    private string nickName;
    private GameObject Obj;

    private int xTile, yTile;
    private int nowCH, nowMap;

    private PlayerController Controller;

    public void setPlayerCode(System.UInt64 _playerCode) { playerCode = _playerCode; Controller.setPlayerCode(_playerCode); }
    public void setObj(GameObject _Obj) { Obj = _Obj; }
    public void setName(string _Name) { nickName = _Name; Controller.setName(_Name); }
    public void setCH(int _CH) { nowCH = _CH; }
    public void setMap(int _mapNo) { nowMap = _mapNo; }
    public void setXTile(int _xTile) { xTile = _xTile; }
    public void setYTile(int _yTile) { yTile = _yTile; }
    public void setController() { Controller = Obj.GetComponent<PlayerController>(); }

    public System.UInt64 getPlayerCode() { return playerCode; }
    public GameObject getObj() { return Obj; }
    public string getName() { return nickName; }
    public int getCH() { return nowCH; }
    public int getMap() { return nowMap; }
    public int getXTile() { return xTile; }
    public int getYTile() { return yTile; }

    // Controller
    public void setChatFlag(bool _Flag) { Controller.setChatFlag(_Flag); }
    public void setControlFlag() { Controller.setControlFlag(); }
    public void setDestPosition(Vector3 _Destination) { Controller.setDestPosition(_Destination); }
    public void setSpeechBubble(string _Text) { Controller.setSpeechBubble(_Text); }
    public void setColor(Color _Color) { Controller.setColor(_Color); }
    public void setPosition(Vector2 _Position) { Controller.setPosition(_Position); }

    public Transform getImage() { return Controller.getImage(); }
}

public class chatManager : MonoBehaviour
{

    [SerializeField]
    public InputField chatField;

    public ScrollRect chatBar;
    public GameObject chatContent;
    public Scrollbar chatScroll;
    public TextMeshProUGUI chatText;
    public Canvas renderCanvas;
    public Color otherPlayerColor;
    public Dropdown chDropdown;
    public Cinemachine.CinemachineVirtualCamera cineCamera;

    public sceneManager Scene;
    public NetworkManager Network;

    public bool chattingFlag;
    public bool channelFlag;

    public System.UInt64 playerCode;


    private Vector2 defaultPosition;
    private float tileSize = 60.0f;
    private int clientCount = 0;

    public Queue<TextMeshProUGUI> chatQueue;

    // Player
    private Player controlPlayer;
    private Dictionary<System.UInt64, Player> otherPlayer;

    // Use this for initialization
    void Start()
    {
        chattingFlag = false;
        channelFlag = false;

        chatBar = GameObject.Find("Chatting_Bar").GetComponent<ScrollRect>();
        chatText = chatBar.GetComponentInChildren<TextMeshProUGUI>();
        chatContent = chatBar.content.gameObject;
        renderCanvas = GameObject.Find("renderCanvas").GetComponent<Canvas>();
        chatQueue = new Queue<TextMeshProUGUI>();
        otherPlayer = new Dictionary<ulong, Player>();
        chDropdown = GameObject.Find("Channel").GetComponent<Dropdown>();
        chDropdown.onValueChanged.AddListener(delegate { dropDownFunction(chDropdown); });
        cineCamera = GameObject.Find("cineCamera").GetComponent<Cinemachine.CinemachineVirtualCamera>();

        defaultPosition = new Vector2(-164.0f, 1110.0f);
        otherPlayerColor = new Color(183, 165, 119, 255);

        Scene = sceneManager.Instance;
        Network = NetworkManager.Instance;
        Network.setChatMng(this);

        request_playerData();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!chattingFlag)
            {
                chatField.Select();
                controlPlayer.setChatFlag(true);
                chattingFlag = true;
            }
            else
            {
                Chatting();
                chatField.text = string.Empty;
                chatField.Select();
                controlPlayer.setChatFlag(false);
                chattingFlag = false;
            }
        }
    }

    void setFollowCamera()
    {
        cineCamera.Follow = controlPlayer.getImage();
    }

    string getName()
    {
        string newName = "A";

        return newName;
    }

    void Chatting()
    {
        if (chatField.text.Length <= 0 || chatField.text.Length > 50) return;
        string Chat = chatField.text;

        byte[] Msg = packet_Chatting(Chat);
        if (Msg == null)
            return;
        Network.sendMsg(Msg);
    }

    public void Move(Vector2 _Direction)
    {
        Debug.Log("Move req] xTile : " + getXtile(_Direction.x) + ", yTile : " + getYtile(_Direction.y));
        byte[] Msg = packet_Move(getXtile(_Direction.x), getYtile(_Direction.y));
        Network.sendMsg(Msg);
    }

    void chMove(int _CH)
    {
        Scene.fadeOut();
        Destroy(controlPlayer.getObj());
        byte[] Msg = packet_chMove(_CH);
        Network.sendMsg(Msg);
        Debug.Log("Send CH Packet");
    }

    void request_playerData()
    {
        byte[] Msg = packet_reqPlayerData();
        Network.sendMsg(Msg);
        Debug.Log("Req data");
    }

    public void recv_playerData(byte[] _Data)
    {
        int dataPos = 7;
        System.UInt64 Index = BitConverter.ToUInt64(_Data, dataPos); dataPos += sizeof(System.UInt64);
        playerCode = Index;
        Debug.Log("Playercode recv : " + playerCode);
    }

    public void recv_createPlayer(byte[] _Data)
    {
        System.UInt64 Index;
        int CH, mapNo, xTile, yTile;

        int dataPos = 7;
        Index = System.BitConverter.ToUInt64(_Data, dataPos); dataPos += sizeof(System.UInt64);
        CH = System.BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        mapNo = System.BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        xTile = System.BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        yTile = System.BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        Player newPlayer = new Player();
        Quaternion zero = new Quaternion(0, 0, 0, 0);
        newPlayer.setObj(Instantiate(Resources.Load("Prefabs/Player"), getPosition(xTile, yTile), zero) as GameObject);
        newPlayer.getObj().transform.parent = renderCanvas.transform;

        newPlayer.setController();
        newPlayer.setPlayerCode(Index);
        newPlayer.setCH(CH);
        newPlayer.setMap(mapNo);
        newPlayer.setXTile(xTile);
        newPlayer.setYTile(yTile);
        Debug.Log("Create playerCode : " + Index);
        if (playerCode == Index)
        {
            setCH(CH);
            controlPlayer = newPlayer;
            controlPlayer.setControlFlag();
            controlPlayer.setName("플레이어");
            setFollowCamera();
            Scene.fadeIn();
            Debug.Log("Create player : 플레이어");
        }
        else
        {
            string Name = "A" + clientCount;
            clientCount++;
            newPlayer.setName(Name);
            newPlayer.setColor(otherPlayerColor);
            if(!otherPlayer.ContainsKey(newPlayer.getPlayerCode()))
                otherPlayer.Add(newPlayer.getPlayerCode(), newPlayer);
        }

    }

    public void recv_deletePlayer(byte[] _Data)
    {
        int dataPos = 7;
        System.UInt64 playerCode = BitConverter.ToUInt64(_Data, dataPos);
        Debug.Log("Delete playerCode : " + playerCode);
        if (!otherPlayer.ContainsKey(playerCode))
            return;
        Player deletePlayer = otherPlayer[playerCode];
        if (deletePlayer == null) return;
        otherPlayer.Remove(playerCode);
        Destroy(deletePlayer.getObj());
        Debug.Log("Delete Success" + playerCode);
    }

    public void recv_Chatting(byte[] _Data)
    {
        int dataPos = 7;
        System.UInt64 playerCode = BitConverter.ToUInt64(_Data, dataPos);
        dataPos += sizeof(System.UInt64);
        int chatSize = BitConverter.ToInt32(_Data, dataPos);
        dataPos += sizeof(int);
        string Chat = System.Text.Encoding.Unicode.GetString(_Data, dataPos, chatSize);
        if (controlPlayer == null) return;
        if (controlPlayer.getPlayerCode() == playerCode)
            showMsg(controlPlayer, Chat);
        else
        {
            if (!otherPlayer.ContainsKey(playerCode))
                return;
            showMsg(otherPlayer[playerCode], Chat);
        }
    }

    public void recv_Move(byte[] _Data)
    {
        int dataPos = 7;
        System.UInt64 playerCode = BitConverter.ToUInt64(_Data, dataPos); dataPos += sizeof(System.UInt64);
        int xTile = BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        int yTile = BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        Debug.Log("Recv move] xTile : " + xTile + ", yTile : " + yTile + ", now xTile : " + controlPlayer.getXTile() + ", now yTile : " + controlPlayer.getYTile());

        Player movePlayer = controlPlayer;

        if (playerCode != controlPlayer.getPlayerCode())
        { 
            try
            {
                movePlayer = otherPlayer[playerCode];
            }
            catch (KeyNotFoundException)
            {
                Debug.Log(playerCode + "존재 않음");
            }
        }
        Vector2 newPosition = getPosition(xTile, yTile);
        movePlayer.setDestPosition(newPosition);
        movePlayer.setXTile(xTile);
        movePlayer.setYTile(yTile);
    }

    public void recv_chMove(byte[] _Data)
    {
        Debug.Log("Ch recv");
        int dataPos = 7;
        System.UInt64 playerCode = BitConverter.ToUInt64(_Data, dataPos); dataPos += sizeof(System.UInt64);
        int CH = BitConverter.ToInt32(_Data, dataPos); dataPos += sizeof(int);
        
        Scene.fadeIn();
        setCH(CH);
    }

    byte[] packet_Move(int _xTile, int _yTile)
    {
        int dataSize = sizeof(short) + sizeof(int) + sizeof(int);
        byte[] Msg = new byte[dataSize];

        byte[] Type = BitConverter.GetBytes((short)chatProtocol.Protocol.c2s_playerMove);
        byte[] xPos = BitConverter.GetBytes(_xTile);
        byte[] yPos = BitConverter.GetBytes(_yTile);

        int dataPos = 0;
        Array.Copy(Type, 0, Msg, dataPos, Type.Length); dataPos += Type.Length;
        Array.Copy(xPos, 0, Msg, dataPos, xPos.Length); dataPos += xPos.Length;
        Array.Copy(yPos, 0, Msg, dataPos, yPos.Length); dataPos += yPos.Length;

        return Msg;
    }

    byte[] packet_chMove(int _CH)
    {
        int dataSize = sizeof(short) + sizeof(int);
        byte[] Msg = new byte[dataSize];

        byte[] Type = BitConverter.GetBytes((short)chatProtocol.Protocol.c2s_playerCHChange);
        byte[] CH = BitConverter.GetBytes(_CH);

        int dataPos = 0;
        Array.Copy(Type, 0, Msg, dataPos, Type.Length); dataPos += Type.Length;
        Array.Copy(CH, 0, Msg, dataPos, CH.Length); dataPos += CH.Length;

        return Msg;
    }

    byte[] packet_Chatting(string _Chat)
    {
        if (_Chat.Length <= 0 || _Chat.Length > 50) return null;

        int dataSize = (_Chat.Length * 2) + sizeof(System.UInt64) + sizeof(short) + sizeof(int);
        byte[] Msg = new byte[dataSize];

        byte[] Type = BitConverter.GetBytes((short)chatProtocol.Protocol.c2s_Chatting);
        byte[] playerCode = BitConverter.GetBytes(controlPlayer.getPlayerCode());
        byte[] chatSize = BitConverter.GetBytes((_Chat.Length * 2));
        byte[] Chat = System.Text.Encoding.Unicode.GetBytes(_Chat);
        int dataPos = 0;
        Array.Copy(Type, 0, Msg, dataPos, Type.Length); dataPos += Type.Length;
        Array.Copy(playerCode, 0, Msg, dataPos, playerCode.Length); dataPos += playerCode.Length;
        Array.Copy(chatSize, 0, Msg, dataPos, chatSize.Length); dataPos += chatSize.Length;
        Array.Copy(Chat, 0, Msg, dataPos, Chat.Length); dataPos += Chat.Length;

        return Msg;
    }

    byte[] packet_reqPlayerData()
    {
        int dataSize = sizeof(short);
        byte[] Msg = new byte[dataSize];

        byte[] Type = BitConverter.GetBytes((short)chatProtocol.Protocol.c2s_playerData_Req);

        int dataPos = 0;
        Array.Copy(Type, 0, Msg, dataPos, Type.Length); dataPos += Type.Length;

        return Msg;
    }

    void showMsg(Player _User, string _Chat)
    {
        TextMeshProUGUI newText = TextMeshProUGUI.Instantiate(chatText, chatContent.transform);
        newText.text = _User.getName() + " : " + _Chat;
        if (chatQueue.Count == 10)
            Destroy(chatQueue.Dequeue().gameObject);
        chatQueue.Enqueue(newText);
        _User.setSpeechBubble(_Chat);

        if (!chatScroll)
            setScrollBar();
        chatScroll.value = 0;
    }

    void setScrollBar()
    {
        chatScroll = chatBar.transform.Find("Scrollbar").GetComponent<Scrollbar>();
    }

    void setCH(int _CH)
    {
        channelFlag = true;
        chDropdown.value = _CH;
        channelFlag = false;
    }

    Vector3 getPosition(int xTile, int yTile)
    {
        Vector3 newVec = new Vector3(getXpos(xTile), getYpos(yTile), 0);

        return newVec;
    }

    float getXpos(int xTile)
    {
        if (xTile < 0 || xTile > 30) return -1f;
        return defaultPosition.x + (xTile * tileSize);
    }

    float getYpos(int yTile)
    {
        if (yTile < 0 || yTile > 30) return -1f;
        return defaultPosition.y - (yTile * tileSize);
    }

    int getXtile(float xPos)
    {
        if (xPos < -164.0f || xPos > 1576.0f)
            return -1;
        return (int)((xPos - defaultPosition.x) / tileSize);
    }

    int getYtile(float yPos)
    {
        if (yPos > 1110.0f || yPos < -630.0f)
            return -1;
        return (int)((defaultPosition.y - yPos) / tileSize);
    }

    void dropDownFunction(Dropdown _Value)
    {
        if (channelFlag) return;
        Debug.Log("Dropdown function call");
        chMove(_Value.value);
    }

    public void setPlayerCode(ulong _playerCode)
    {
        playerCode = _playerCode;
    }

    public void Terminate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
