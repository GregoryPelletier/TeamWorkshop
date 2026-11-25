using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// The pause menu. Press Esc (or P) in any level to open it.
//   Resume        goes back to the game.
//   Unstuck       moves the player back to the last safe spot they stood on.
//   Restart Level starts the level again.
//   Quit Game     closes the game (stops Play mode in the editor).
// It puts itself into every scene that has a player, so nothing has to be added to the levels.
public class PauseMenu : MonoBehaviour
{
    [Tooltip("How often (seconds) the player's position is saved as a safe spot.")]
    public float saveEvery = 0.3f;
    [Tooltip("Unstuck skips safe spots closer than this to where the player is now.")]
    public float unstuckDistance = 1.5f;

    static PauseMenu menuInstance;

    Health playerHealth;
    Rigidbody2D playerBody;
    Collider2D playerCollider;
    bool isPaused;
    float nextSaveTime;
    Vector3 levelStart;
    readonly List<Vector3> safeSpots = new List<Vector3>();
    readonly List<Behaviour> pausedScripts = new List<Behaviour>();
    int resumeFrame;
    string shownNote = "";
    float noteUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MakeMenu()
    {
        if (menuInstance != null)
        {
            return;
        }
        GameObject menuObject = new GameObject("Pause Menu");
        DontDestroyOnLoad(menuObject);
        menuInstance = menuObject.AddComponent<PauseMenu>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += WhenSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= WhenSceneLoaded;
    }

    void WhenSceneLoaded(Scene loadedScene, LoadSceneMode loadMode)
    {
        SetPaused(false);
        playerHealth = null;
        safeSpots.Clear();
    }

    bool FindPlayer()
    {
        if (playerHealth != null)
        {
            return true;
        }
        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth != null && oneHealth.team == Team.Player)
            {
                playerHealth = oneHealth;
            }
        }
        if (playerHealth == null)
        {
            return false;
        }
        playerBody = playerHealth.GetComponent<Rigidbody2D>();
        playerCollider = playerHealth.GetComponent<Collider2D>();
        levelStart = playerHealth.transform.position;
        safeSpots.Clear();
        safeSpots.Add(levelStart);
        return true;
    }

    void Update()
    {
        if (!FindPlayer())
        {
            return;
        }
        Keyboard keyboardInput = Keyboard.current;
        bool menuPressed = keyboardInput != null && (keyboardInput.escapeKey.wasPressedThisFrame || keyboardInput.pKey.wasPressedThisFrame);
        if (menuPressed)
        {
            SetPaused(!isPaused);
        }
        // the player's scripts come back one frame after the menu closes,
        // so the click on a menu button does not also swing the staff
        if (!isPaused && pausedScripts.Count > 0 && Time.frameCount > resumeFrame)
        {
            foreach (Behaviour playerScript in pausedScripts)
            {
                if (playerScript != null)
                {
                    playerScript.enabled = true;
                }
            }
            pausedScripts.Clear();
        }
        if (!isPaused && Time.time >= nextSaveTime)
        {
            nextSaveTime = Time.time + saveEvery;
            SaveSafeSpot();
        }
    }

    // A spot is safe when the player is standing still on solid ground and is not hurt or defeated.
    void SaveSafeSpot()
    {
        if (playerHealth.IsDefeated || playerBody == null || playerCollider == null)
        {
            return;
        }
        if (Mathf.Abs(playerBody.linearVelocity.y) > 0.01f || Time.time - playerHealth.LastHurtTime < 1f)
        {
            return;
        }
        Bounds playerBox = playerCollider.bounds;
        Vector2 feetPoint = new Vector2(playerBox.center.x, playerBox.min.y + 0.02f);
        RaycastHit2D groundHit = Physics2D.Raycast(feetPoint, Vector2.down, 0.1f, LayerMask.GetMask("Ground"));
        if (groundHit.collider == null || groundHit.collider.isTrigger)
        {
            return;
        }
        Vector3 standSpot = playerHealth.transform.position;
        Vector3 lastSpot = safeSpots[safeSpots.Count - 1];
        if (Vector3.Distance(standSpot, lastSpot) < 0.3f)
        {
            return;
        }
        safeSpots.Add(standSpot);
        if (safeSpots.Count > 60)
        {
            safeSpots.RemoveAt(1);   // always keep the level start
        }
    }

    void SetPaused(bool wantPaused)
    {
        if (wantPaused == isPaused)
        {
            return;
        }
        isPaused = wantPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        if (isPaused)
        {
            // stop the player's own scripts from reading the keys and the mouse while the menu is open
            pausedScripts.Clear();
            if (playerHealth != null)
            {
                foreach (Behaviour playerScript in playerHealth.GetComponents<Behaviour>())
                {
                    bool readsInput = playerScript is PlayerMovement || playerScript is MeleeAttack ||
                                      playerScript is SpellInput || playerScript is FireballCaster;
                    if (readsInput && playerScript.enabled)
                    {
                        playerScript.enabled = false;
                        pausedScripts.Add(playerScript);
                    }
                }
            }
            return;
        }
        resumeFrame = Time.frameCount;
    }

    void Unstuck()
    {
        Vector3 currentSpot = playerHealth.transform.position;
        Vector3 bestSpot = levelStart;
        for (int spotIndex = safeSpots.Count - 1; spotIndex >= 0; spotIndex--)
        {
            if (Vector3.Distance(safeSpots[spotIndex], currentSpot) >= unstuckDistance)
            {
                bestSpot = safeSpots[spotIndex];
                safeSpots.RemoveRange(spotIndex + 1, safeSpots.Count - spotIndex - 1);
                break;
            }
        }
        playerHealth.transform.position = bestSpot;
        if (playerBody != null)
        {
            playerBody.position = bestSpot;
            playerBody.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();
        SetPaused(false);
        shownNote = "Moved back to a safe spot.";
        noteUntil = Time.time + 2f;
    }

    void RestartLevel()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void QuitGame()
    {
        SetPaused(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnGUI()
    {
        if (playerHealth == null)
        {
            return;
        }
        int smallSize = ScreenText.SizeFor(0.022f);
        if (!isPaused)
        {
            Rect hintArea = new Rect(0f, Screen.height * 0.01f, Screen.width - Screen.width * 0.01f, Screen.height * 0.05f);
            ScreenText.Draw(hintArea, "Esc: Menu", smallSize, TextAnchor.UpperRight, new Color(1f, 1f, 1f, 0.7f), false);
            if (Time.time < noteUntil)
            {
                Rect noteArea = new Rect(0f, Screen.height * 0.2f, Screen.width, Screen.height * 0.06f);
                ScreenText.Draw(noteArea, shownNote, ScreenText.SizeFor(0.03f), TextAnchor.MiddleCenter, Color.white, true);
            }
            return;
        }

        // dark see-through cover over the game
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = oldColor;

        int titleSize = ScreenText.SizeFor(0.07f);
        Rect titleArea = new Rect(0f, Screen.height * 0.16f, Screen.width, Screen.height * 0.1f);
        ScreenText.Draw(titleArea, "Paused", titleSize, TextAnchor.MiddleCenter, Color.white, false);

        float buttonWidth = Screen.width * 0.26f;
        float buttonHeight = Screen.height * 0.075f;
        float buttonLeft = (Screen.width - buttonWidth) * 0.5f;
        float buttonTop = Screen.height * 0.32f;
        float buttonGap = buttonHeight * 1.3f;
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = ScreenText.SizeFor(0.032f);
        buttonStyle.fontStyle = FontStyle.Bold;

        if (GUI.Button(new Rect(buttonLeft, buttonTop, buttonWidth, buttonHeight), "Resume", buttonStyle))
        {
            SetPaused(false);
        }
        if (GUI.Button(new Rect(buttonLeft, buttonTop + buttonGap, buttonWidth, buttonHeight), "Unstuck", buttonStyle))
        {
            Unstuck();
        }
        if (GUI.Button(new Rect(buttonLeft, buttonTop + buttonGap * 2f, buttonWidth, buttonHeight), "Restart Level", buttonStyle))
        {
            RestartLevel();
        }
        if (GUI.Button(new Rect(buttonLeft, buttonTop + buttonGap * 3f, buttonWidth, buttonHeight), "Quit Game", buttonStyle))
        {
            QuitGame();
        }
        Rect helpArea = new Rect(0f, buttonTop + buttonGap * 4.1f, Screen.width, Screen.height * 0.05f);
        ScreenText.Draw(helpArea, "Unstuck moves you back to the last place you stood safely.", smallSize, TextAnchor.MiddleCenter, Color.white, false);
    }
}
