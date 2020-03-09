using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour {

    private Camera mainCamera;

    public ulong playerCode;

    private GameObject speechBubble;
    public Transform playerImage;
    public Text playerName;
    public TextMeshProUGUI speechText;

    public Vector2 Direction;
    public Vector3 destPosition;
    public Vector3 authorizedDestPosition;
    public Vector3 nameOffset;
    public Vector3 speechBubbleOffset;

    private float moveSpeed, xSpeed, ySpeed;
    public bool inputFlag = false, chatFlag = false, controlCharFlag = false;
    public bool moveRecvFlag = false;

    private Transform Trans;
    public SpriteRenderer Renderer;
    private Animator Animater;

    public string nickName;

    chatManager Chat;
    
	// Use this for initialization
	void Awake () {

        Trans = gameObject.GetComponent<Transform>();
        setPlayerRender();   
        Animater = gameObject.GetComponentInChildren<Animator>();
        speechBubble = transform.Find("SpeechBubble").gameObject;
        speechText = speechBubble.GetComponentInChildren<TextMeshProUGUI>();
        speechBubble.SetActive(false);
        setPlayerImage();

        destPosition = Trans.position;
        authorizedDestPosition = Trans.position;

        mainCamera = Camera.main;

        transform.SetAsLastSibling();

        nameOffset = new Vector3(1.0f, -60.0f, 0.0f);
        speechBubbleOffset = new Vector3(-53.0f, 15.0f, 0.0f);

        moveSpeed = 300.0f;
        xSpeed = 60.0f;
        ySpeed = 60.0f;

        Chat = GameObject.Find("ClientManager").GetComponent<chatManager>();
    }
	
	// Update is called once per frame
	void Update () {

        getInput();
        Move();
        updatePosition();
	}

    void updatePosition()
    {
        if (playerImage == null) return;
            speechBubble.transform.position = mainCamera.WorldToScreenPoint(playerImage.transform.position) + speechBubbleOffset;
            playerName.transform.position = mainCamera.WorldToScreenPoint(playerImage.transform.position) + nameOffset;
    }

    public void Move()
    {
        if (controlCharFlag && !inputFlag) return;
        if (!moveRecvFlag) return;
        Trans.position = Vector3.MoveTowards(Trans.position, authorizedDestPosition, moveSpeed * Time.deltaTime);
        if (Trans.position == authorizedDestPosition)
        {
            inputFlag = false;
            moveRecvFlag = false;
            Animater.SetBool("Walk", false);
        }
    }

    public void getInput()
    {
        if (!controlCharFlag) return;
        if (inputFlag) return;
        if (chatFlag) return;

        Vector2 moveVector;
        moveVector.x = Input.GetAxisRaw("Horizontal");
        moveVector.y = Input.GetAxisRaw("Vertical");
        Direction = moveVector;

        destPosition.x = Trans.position.x + (xSpeed * Direction.x);
        destPosition.y = Trans.position.y + (ySpeed * Direction.y);

        if (destPosition.x < -164 || destPosition.x > 1576)
            return;
        if (destPosition.y > 1110 || destPosition.y < -630)
            return;

        if (Direction != Vector2.zero)
        {
            inputFlag = true;
            Chat.Move(destPosition);
        }        
    }

    void moveMsg()
    {

    }

    public void setChatFlag(bool _Val)
    {
        chatFlag = _Val;
    }

    public void setControlFlag()
    {
        controlCharFlag = true;
    }

    public void setDestPosition(Vector3 _Destination)
    {
        float nowXposition = Trans.position.x, destXposition = _Destination.x;
        float Direction = destXposition - nowXposition;
        if (Direction < 0f)
            Renderer.flipX = true;
        else if (Direction> 0f)
            Renderer.flipX = false;

        authorizedDestPosition = _Destination;
        Animater.SetBool("Walk", true);
        moveRecvFlag = true;
    }

    IEnumerator speechBubbleInvisible()
    {
        yield return new WaitForSeconds(2);
        speechBubble.SetActive(false);
    }

    public void setSpeechBubble(string _Text)
    {
        speechBubble.SetActive(true);
        speechText.text =nickName + " : " + _Text;
        StartCoroutine(speechBubbleInvisible());
    }

    public void setName(string _Name)
    {
        if (playerName == null)
            setPlayerName();
        nickName = _Name;
        playerName.text = nickName;
    }

    public void setColor(Color _Color)
    {
        Renderer.color = _Color;
    }

    public void setPosition(Vector2 _Position)
    {
        transform.position = _Position;
    }

    public void setPlayerCode(ulong _playerCode)
    {
        playerCode = _playerCode;
    }

    void setPlayerName()
    {
        if (playerName != null) return;
        playerName = transform.Find("Name").GetComponent<Text>();
    }

    void setPlayerImage()
    {
        if (playerImage != null) return;
        playerImage = transform.Find("Image");
    }

    void setPlayerRender()
    {
        if (Renderer != null) return;
        Renderer = gameObject.GetComponentInChildren<SpriteRenderer>();
    }

    public Transform getImage()
    {
        if (playerImage == null)
            setPlayerImage();
        return playerImage;
    }
}
