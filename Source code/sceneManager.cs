using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class sceneManager : MonoBehaviour
{

    [SerializeField]
    private Image Fade;
    public GameObject loginFail;
    protected static sceneManager s_Instance;

    private float fadeSpeed, fadeTime;
    private float startAlpha, endAlpha;
    public bool isPlaying;

    public static sceneManager Instance
    {
        get
        {
            if (s_Instance != null)
                return s_Instance;

            s_Instance = FindObjectOfType<sceneManager>();

            if (s_Instance != null)
                return s_Instance;

            sceneManager SMng = Resources.Load<sceneManager>("Prefabs/SceneManager");
            s_Instance = Instantiate(SMng);

            return s_Instance;
        }
    }

    void Awake()
    {
        if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Fade = GameObject.Find("Fade").GetComponent<Image>();
        loginFail = GameObject.Find("loginFail");
        loginFail.SetActive(false);
        Fade.enabled = false;
        isPlaying = false;
        fadeSpeed = 1.0f;
        startAlpha = 0.0f;
        endAlpha = 1.0f;
        fadeTime = 0.0f;
    }

    public void trunLoginFail(bool _Flag)
    {
        Debug.Log("Login fail flag : " + _Flag);
        loginFail.SetActive(_Flag);
    }

    public void proc_loginFail()
    {
        fadeIn();
        trunLoginFail(true);
    }

    public void fadeOut()
    {
        if (isPlaying)
            return;
        Fade.enabled = true;
        StartCoroutine("coFadeOut");
    }

    public void fadeIn()
    {
        StopCoroutine("coFadeOut");
        StartCoroutine("coFadeIn");
    }

    public void changeScene(string _sceneName)
    {
        SceneManager.LoadScene(_sceneName);
    }

    IEnumerator coFadeOut()
    {
        // Playing animation
        isPlaying = true;

        Color alphaColor = Fade.color;

        fadeTime = 0.0f;
        alphaColor.a = Mathf.Lerp(startAlpha, endAlpha, fadeTime);

        while (alphaColor.a < 1.0f)
        {
            fadeTime += Time.deltaTime / fadeSpeed;

            alphaColor.a = Mathf.Lerp(startAlpha, endAlpha, fadeTime);

            if (alphaColor.a >= 1.0f)
                alphaColor.a = 1.0f;

            Fade.color = alphaColor;
            yield return null;
        }

        Fade.color = alphaColor;
        isPlaying = false;
    }

    IEnumerator coFadeIn()
    {
        isPlaying = true;

        Color alphaColor = Fade.color;

        fadeTime = 0.0f;
        alphaColor.a = Mathf.Lerp(startAlpha, endAlpha, fadeTime);

        while (alphaColor.a > 0.0f)
        {
            fadeTime -= Time.deltaTime / fadeSpeed;

            alphaColor.a = Mathf.Lerp(startAlpha, endAlpha, fadeTime);

            if (alphaColor.a <= 0.0f)
                alphaColor.a = 0.0f;

            Fade.color = alphaColor;
            Debug.Log(alphaColor);
            yield return null;
        }

        Fade.color = alphaColor;
        Fade.enabled = false;
        isPlaying = false;
    }
}
