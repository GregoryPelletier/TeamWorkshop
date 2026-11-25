using UnityEngine;

// A gate in front of a boss arena. It stays shut until every creature on the near (left) side
// of it is defeated, then it sinks into the ground and lets the player through.
// While it is shut and the player is close, it shows how many creatures are still left.
public class ArenaGate : MonoBehaviour
{
    [Tooltip("Creatures whose names contain any of these are not counted (the boss waiting inside).")]
    public string[] ignoreNames = { "Mini Boss", "BOSS" };
    [Tooltip("How close the player has to be to see the message.")]
    public float messageRange = 4f;
    [Tooltip("Seconds the gate takes to sink into the ground.")]
    public float openTime = 1f;

    int enemiesLeft = 1;
    bool isOpening;
    float openTimer;
    float nextCheckTime;
    Vector3 closedPosition;
    float gateHeight = 2f;
    SpriteRenderer gateRenderer;
    Collider2D gateCollider;
    Health playerHealth;

    void Start()
    {
        closedPosition = transform.position;
        gateRenderer = GetComponent<SpriteRenderer>();
        gateCollider = GetComponent<Collider2D>();
        if (gateRenderer != null)
        {
            gateHeight = gateRenderer.bounds.size.y;
        }
    }

    void Update()
    {
        if (isOpening)
        {
            openTimer += Time.deltaTime;
            float openShare = Mathf.Clamp01(openTimer / openTime);
            transform.position = closedPosition + Vector3.down * gateHeight * openShare;
            if (gateRenderer != null)
            {
                Color gateColor = gateRenderer.color;
                gateColor.a = 1f - openShare;
                gateRenderer.color = gateColor;
            }
            if (openShare >= 1f)
            {
                gameObject.SetActive(false);
            }
            return;
        }
        if (Time.time < nextCheckTime)
        {
            return;
        }
        nextCheckTime = Time.time + 0.25f;
        enemiesLeft = CountEnemies();
        if (enemiesLeft == 0)
        {
            isOpening = true;
            if (gateCollider != null)
            {
                gateCollider.enabled = false;
            }
        }
    }

    int CountEnemies()
    {
        int foundCount = 0;
        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth == null || oneHealth.team != Team.Enemy || oneHealth.IsDefeated)
            {
                continue;
            }
            if (oneHealth.transform.position.x >= closedPosition.x || IsIgnored(oneHealth.name))
            {
                continue;
            }
            foundCount++;
        }
        return foundCount;
    }

    bool IsIgnored(string enemyName)
    {
        foreach (string skipName in ignoreNames)
        {
            if (enemyName.Contains(skipName))
            {
                return true;
            }
        }
        return false;
    }

    Health FindPlayer()
    {
        if (playerHealth != null)
        {
            return playerHealth;
        }
        foreach (Health oneHealth in Health.All)
        {
            if (oneHealth != null && oneHealth.team == Team.Player)
            {
                playerHealth = oneHealth;
            }
        }
        return playerHealth;
    }

    void OnGUI()
    {
        if (isOpening)
        {
            return;
        }
        Health targetPlayer = FindPlayer();
        if (targetPlayer == null)
        {
            return;
        }
        float playerGap = Mathf.Abs(targetPlayer.transform.position.x - closedPosition.x);
        if (playerGap > messageRange)
        {
            return;
        }
        string gateMessage = "The gate is shut. Defeat every creature to open it: " + enemiesLeft + " left";
        int fontSize = ScreenText.SizeFor(0.035f);
        Rect textArea = new Rect(0f, Screen.height * 0.14f, Screen.width, Screen.height * 0.08f);
        ScreenText.Draw(textArea, gateMessage, fontSize, TextAnchor.MiddleCenter, Color.white, true);
    }
}
