using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Moves the game on to another scene, fading to black and back so the switch is smooth.
// Walk Into: put it on a trigger at the end of a level. When the player walks in, the next scene loads
//   (only after Boss To Defeat is defeated, if one is set).
// Press Any Key: put it on any object in a start scene. It shows a title, and any key or click starts the game.
// Every scene it loads must be in File > Build Profiles > Scene List.
public class LevelChanger : MonoBehaviour
{
    public enum StartWhen { WalkInto, PressAnyKey }

    [Tooltip("Walk Into: the player walks into this object's trigger. Press Any Key: any key or click.")]
    public StartWhen startWhen = StartWhen.WalkInto;
    [Tooltip("The scene to load, exactly as it is named in the Scene List.")]
    public string nextSceneName = "";
    [Tooltip("Seconds to fade to black, and again to fade back in.")]
    public float fadeSeconds = 0.75f;

    [Header("Walk Into only")]
    [Tooltip("Must be defeated before the way opens. Leave empty to always be open.")]
    public Health bossToDefeat;
    [Tooltip("Shown when the player walks in before the boss is defeated.")]
    public string lockedMessage = "Defeat the boss to move on.";

    [Header("Press Any Key only")]
    public string titleText = "Dragon's Toll";
    public string promptText = "Press any key to start";

    bool isChanging;
    float nextMessageTime;

    void Update()
    {
        if (startWhen != StartWhen.PressAnyKey || isChanging)
        {
            return;
        }
        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (keyPressed || mouseClicked)
        {
            ChangeScene();
        }
    }

    void OnTriggerEnter2D(Collider2D otherCollider)
    {
        TryWalkInto(otherCollider);
    }

    void OnTriggerStay2D(Collider2D otherCollider)
    {
        TryWalkInto(otherCollider);
    }

    void TryWalkInto(Collider2D otherCollider)
    {
        if (startWhen != StartWhen.WalkInto || isChanging)
        {
            return;
        }
        Health walkerHealth = otherCollider.GetComponentInParent<Health>();
        if (walkerHealth == null || walkerHealth.team != Team.Player || walkerHealth.IsDefeated)
        {
            return;
        }
        if (bossToDefeat != null && !bossToDefeat.IsDefeated)
        {
            if (Time.time >= nextMessageTime)
            {
                GameMessage.Show(lockedMessage, 2.5f);
                nextMessageTime = Time.time + 3f;
            }
            return;
        }
        ChangeScene();
    }

    // Can also be called by a button or another script.
    public void ChangeScene()
    {
        if (isChanging)
        {
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError("LevelChanger: the scene '" + nextSceneName + "' is not in File > Build Profiles > Scene List.");
            return;
        }
        isChanging = true;
        ScreenFader.FadeAndLoad(nextSceneName, fadeSeconds);
    }

    void OnGUI()
    {
        if (startWhen != StartWhen.PressAnyKey || isChanging)
        {
            return;
        }
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.12f);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.4f);
        GUIStyle promptStyle = new GUIStyle(titleStyle);
        promptStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.045f);
        promptStyle.fontStyle = FontStyle.Normal;
        promptStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f + 0.4f * Mathf.Sin(Time.time * 3f));

        GUI.Label(new Rect(0f, Screen.height * 0.25f, Screen.width, Screen.height * 0.2f), titleText, titleStyle);
        GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, Screen.height * 0.1f), promptText, promptStyle);
    }
}

// The black fade between scenes. LevelChanger makes it; it is never placed by hand.
// It stays alive while the next scene loads, then fades back in and removes itself.
public class ScreenFader : MonoBehaviour
{
    static ScreenFader activeFader;
    float blackAmount;

    public static void FadeAndLoad(string sceneName, float fadeSeconds)
    {
        if (activeFader != null)
        {
            return;
        }
        GameObject faderObject = new GameObject("Screen Fader");
        DontDestroyOnLoad(faderObject);
        activeFader = faderObject.AddComponent<ScreenFader>();
        activeFader.StartCoroutine(activeFader.FadeRoutine(sceneName, fadeSeconds));
    }

    IEnumerator FadeRoutine(string sceneName, float fadeSeconds)
    {
        float fadeTimer = 0f;
        while (fadeTimer < fadeSeconds)
        {
            fadeTimer += Time.unscaledDeltaTime;
            blackAmount = Mathf.Clamp01(fadeTimer / fadeSeconds);
            yield return null;
        }
        blackAmount = 1f;

        AsyncOperation sceneLoading = SceneManager.LoadSceneAsync(sceneName);
        while (!sceneLoading.isDone)
        {
            yield return null;
        }
        GameMessage.Text = "";   // no leftover message from the last level

        fadeTimer = 0f;
        while (fadeTimer < fadeSeconds)
        {
            fadeTimer += Time.unscaledDeltaTime;
            blackAmount = 1f - Mathf.Clamp01(fadeTimer / fadeSeconds);
            yield return null;
        }
        activeFader = null;
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (blackAmount <= 0f)
        {
            return;
        }
        GUI.depth = -1000;
        GUI.color = new Color(0f, 0f, 0f, blackAmount);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
