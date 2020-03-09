using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class startSceneManager : MonoBehaviour {

    [SerializeField]
    private InputField IPField;
    [SerializeField]
    private InputField PORTField;
    [SerializeField]
    private NetworkManager Network;
    [SerializeField]
    private sceneManager Scene;
    [SerializeField]
    private GameObject Popup;

    // Use this for initialization
    void Start()
    {
        Network = NetworkManager.Instance;
        Scene = sceneManager.Instance;
        IPField = GameObject.Find("IPField").GetComponent<InputField>();
        PORTField = GameObject.Find("PortField").GetComponent<InputField>();
        Popup = GameObject.Find("POPUP");
        Popup.SetActive(false);
        Network.setStart(this);
    }
	
	public void connectFunction()
    {
        if (IPField.text.Length == 0 || PORTField.text.Length == 0 
            || IPField.text.Length > 15 || PORTField.text.Length > 6)
        {
            turnPanel(true);
            return;
        }
        string IP = IPField.text;
        int Port = Int32.Parse(PORTField.text);

        if (Network.callConnectFunction(IP, Port))
        {
            Scene.fadeOut();
            StartCoroutine(Network.checkConnect());
        }
        else
            turnPanel(true);
    }

    public void terminateFunction()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void turnPanel(bool _flag)
    {
        Popup.SetActive(_flag);
    }

    public void Login()
    {
        int dataSize = sizeof(short) + sizeof(short);
        int dataPos = 0;
        byte[] packetType = BitConverter.GetBytes((short)(chatProtocol.Protocol.c2s_Login_Req));
        byte[] clientType = BitConverter.GetBytes((short)(chatProtocol.chatClientType.unityClient));
        byte[] Msg = new byte[dataSize];
        Array.Copy(packetType, 0, Msg, 0, packetType.Length); dataPos += packetType.Length;
        Array.Copy(clientType, 0, Msg, 2, clientType.Length); dataPos += clientType.Length;
        Network.sendMsg(Msg);
    }

    public void recv_Login(byte[] _msg)
    {
        char Result = BitConverter.ToChar(_msg, 7);
        if (Result == 0)
            Scene.trunLoginFail(true);
        else
            Scene.changeScene("Chat");
    }

}
