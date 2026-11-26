using UnityEngine;
using UnityEngine.SceneManagement;

// Put this on a boss. When the boss is defeated it says one of its Last Words above its head,
// typed out letter by letter: a hint that it will be back for the final fight with the Dragon.
// It picks a different line each time you win (it never says the same line twice in a row,
// even after you close the game), and every boss has its own list.
[RequireComponent(typeof(Health))]
public class BossLastWords : MonoBehaviour
{
    [Tooltip("One of these is picked each time the boss is defeated.")]
    [TextArea(2, 4)]
    public string[] lastWords = { "Ha ha ha... am I gone?? Not for long..." };
    [Tooltip("Colour of the words, to match the boss.")]
    public Color textColor = new Color(1f, 0.85f, 0.4f);
    [Tooltip("Seconds the words stay on screen after they are fully typed.")]
    public float showSeconds = 5f;
    [Tooltip("How many letters appear each second.")]
    public float lettersPerSecond = 28f;
    [Tooltip("How far above the boss's feet the words appear.")]
    public float heightAbove = 2.2f;

    Health bossHealth;

    void OnEnable()
    {
        bossHealth = GetComponent<Health>();
        bossHealth.WasDefeated += WhenDefeated;
    }

    void OnDisable()
    {
        bossHealth.WasDefeated -= WhenDefeated;
    }

    void WhenDefeated()
    {
        if (lastWords == null || lastWords.Length == 0)
        {
            return;
        }
        string chosenLine = PickLine();
        Vector3 speechSpot = transform.position + Vector3.up * heightAbove;
        BossSpeech.Say(chosenLine, speechSpot, textColor, lettersPerSecond, showSeconds);
    }

    // A random line, but never the one this boss said last time.
    string PickLine()
    {
        string saveKey = "LastWords_" + SceneManager.GetActiveScene().name + "_" + gameObject.name;
        int lastIndex = PlayerPrefs.GetInt(saveKey, -1);
        int pickIndex = Random.Range(0, lastWords.Length);
        if (lastWords.Length > 1 && pickIndex == lastIndex)
        {
            pickIndex = (pickIndex + Random.Range(1, lastWords.Length)) % lastWords.Length;
        }
        PlayerPrefs.SetInt(saveKey, pickIndex);
        PlayerPrefs.Save();
        return lastWords[pickIndex];
    }
}

// The words on screen. BossLastWords makes one; it is never placed by hand.
// It is its own object so the words stay even while the boss fades away.
public class BossSpeech : MonoBehaviour
{
    string fullLine;
    Vector3 worldSpot;
    Color lineColor;
    float typeSpeed;
    float holdSeconds;
    float startTime;

    public static void Say(string spokenLine, Vector3 speechSpot, Color textColor, float lettersPerSecond, float showSeconds)
    {
        GameObject speechObject = new GameObject("Boss Speech");
        BossSpeech newSpeech = speechObject.AddComponent<BossSpeech>();
        newSpeech.fullLine = SplitLongLine(spokenLine);
        newSpeech.worldSpot = speechSpot;
        newSpeech.lineColor = textColor;
        newSpeech.typeSpeed = Mathf.Max(1f, lettersPerSecond);
        newSpeech.holdSeconds = showSeconds;
        newSpeech.startTime = Time.time;
    }

    // Long lines are split into two rows at the space nearest the middle, so they fit on screen.
    static string SplitLongLine(string spokenLine)
    {
        if (spokenLine.Length <= 42 || spokenLine.Contains("\n"))
        {
            return spokenLine;
        }
        int middleSpot = spokenLine.Length / 2;
        int bestSpace = -1;
        for (int spotIndex = 0; spotIndex < spokenLine.Length; spotIndex++)
        {
            bool isSpace = spokenLine[spotIndex] == ' ';
            if (isSpace && (bestSpace < 0 || Mathf.Abs(spotIndex - middleSpot) < Mathf.Abs(bestSpace - middleSpot)))
            {
                bestSpace = spotIndex;
            }
        }
        if (bestSpace < 0)
        {
            return spokenLine;
        }
        return spokenLine.Substring(0, bestSpace) + "\n" + spokenLine.Substring(bestSpace + 1);
    }

    void Update()
    {
        float typeTime = fullLine.Length / typeSpeed;
        if (Time.time > startTime + typeTime + holdSeconds + 1f)
        {
            Destroy(gameObject);
        }
    }

    void OnGUI()
    {
        Camera viewCamera = Camera.main;
        if (viewCamera == null || string.IsNullOrEmpty(fullLine))
        {
            return;
        }
        float timePassed = Time.time - startTime;
        int shownLetters = Mathf.Clamp(Mathf.FloorToInt(timePassed * typeSpeed), 0, fullLine.Length);
        float typeTime = fullLine.Length / typeSpeed;
        float fadeAmount = 1f - Mathf.Clamp01(timePassed - typeTime - holdSeconds);
        if (fadeAmount <= 0f)
        {
            return;
        }

        // above the boss's head, kept inside the screen
        Vector3 screenPoint = viewCamera.WorldToScreenPoint(worldSpot);
        float boxWidth = Screen.width * 0.6f;
        float boxHeight = Screen.height * 0.16f;
        float boxLeft = Mathf.Clamp(screenPoint.x - boxWidth * 0.5f, Screen.width * 0.02f, Screen.width * 0.98f - boxWidth);
        float boxTop = Mathf.Clamp(Screen.height - screenPoint.y - boxHeight, Screen.height * 0.08f, Screen.height * 0.6f);
        if (screenPoint.z < 0f)
        {
            boxLeft = (Screen.width - boxWidth) * 0.5f;
            boxTop = Screen.height * 0.12f;
        }

        Color fadedColor = lineColor;
        fadedColor.a = fadeAmount;
        int fontSize = ScreenText.SizeFor(0.036f);
        Rect textArea = new Rect(boxLeft, boxTop, boxWidth, boxHeight);
        ScreenText.Draw(textArea, fullLine.Substring(0, shownLetters), fontSize, TextAnchor.MiddleCenter, fadedColor, true);
    }
}
